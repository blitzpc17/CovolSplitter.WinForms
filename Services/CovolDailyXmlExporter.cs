using System.Xml.Linq;
using CovolSplitter.WinForms.Models;
using Dapper;
using Npgsql;

namespace CovolSplitter.WinForms.Services;

public sealed class CovolDailyXmlExporter
{
    private readonly string _connectionString;

    private static readonly XNamespace Covol = "https://www.sat.gob.mx/ControlesVolumetricos";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Exp = "Complemento_Expendio";

    public CovolDailyXmlExporter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<string> ExportDailyXmlAsync(
        int anio,
        int mes,
        DateOnly fechaOperacion,
        string[] productKeys,
        string[] tiposMovimiento,
        string outputFolder,
        CancellationToken ct = default)
    {
        var fechaDb = fechaOperacion.ToDateTime(TimeOnly.MinValue);

        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync(ct);

        var header = await cn.QueryFirstOrDefaultAsync(new CommandDefinition(@"
            SELECT
                a.nombre_archivo,
                a.version_xml,
                a.rfc_contribuyente,
                a.rfc_representante_legal,
                a.rfc_proveedor,
                a.clave_instalacion,
                a.descripcion_instalacion,
                a.numero_pozos,
                a.numero_tanques,
                a.numero_ductos_entrada_salida,
                a.numero_ductos_transporte_distribucion,
                a.numero_dispensarios
            FROM covol.archivos a
            WHERE a.anio = @anio
              AND a.mes = @mes
            ORDER BY a.tipo_archivo, a.id
            LIMIT 1;",
            new { anio, mes },
            cancellationToken: ct
        ));

        if (header is null)
            throw new InvalidOperationException("No se encontró encabezado base para generar XML diario.");

        var tipoReporte = "EXO";
        var nomArchivo = (string?)header.nombre_archivo;
        if (!string.IsNullOrWhiteSpace(nomArchivo))
        {
            var parts = nomArchivo.Split('_');
            if (parts.Length >= 7) tipoReporte = parts[6];
        }

        var guid = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var rfcC = (string?)header.rfc_contribuyente ?? "XAXX010101000";
        var rfcP = (string?)header.rfc_proveedor ?? "XAXX010101000";
        var cve = (string?)header.clave_instalacion ?? "EDS";

        var fileName = $"D_{guid}_{rfcC}_{rfcP}_{fechaOperacion:yyyy-MM-dd}_{cve}_{tipoReporte}_XML.xml";
        var outputPath = Path.Combine(outputFolder, fileName);

        var productos = (await cn.QueryAsync(new CommandDefinition(@"
            SELECT
                p.id AS producto_id,
                p.clave_producto,
                p.clave_subproducto,
                p.marca_comercial,
                p.octanaje,
                p.combustible_no_fosil,
                p.xml_producto_base
            FROM covol.transacciones t
            JOIN covol.productos p ON p.id = t.producto_id
            WHERE t.anio = @anio
              AND t.mes = @mes
              AND t.fecha_operacion = @fechaOperacion
              AND (
                    @filtrarProductos = FALSE
                    OR (
                        p.clave_producto || '|' ||
                        COALESCE(p.clave_subproducto, '') || '|' ||
                        COALESCE(p.marca_comercial, '')
                    ) = ANY(@productKeys)
              )
            GROUP BY
                p.id,
                p.clave_producto,
                p.clave_subproducto,
                p.marca_comercial,
                p.octanaje,
                p.combustible_no_fosil,
                p.xml_producto_base
            ORDER BY
                CASE 
                    WHEN p.marca_comercial ILIKE '%MAGNA%' THEN 1
                    WHEN p.marca_comercial ILIKE '%PREMIUM%' THEN 2
                    WHEN p.marca_comercial ILIKE '%DIESEL%' THEN 3
                    ELSE 4
                END, p.marca_comercial;",
            new
            {
                anio,
                mes,
                fechaOperacion = fechaDb,
                productKeys,
                filtrarProductos = productKeys.Length > 0
            },
            cancellationToken: ct
        ))).ToList();

        var root = new XElement(Covol + "ControlesVolumetricos",
            new XAttribute(XNamespace.Xmlns + "Covol", Covol),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi),
            new XAttribute(XNamespace.Xmlns + "exp", Exp),
            new XElement(Covol + "Version", header.version_xml ?? "1.0"),
            new XElement(Covol + "RfcContribuyente", header.rfc_contribuyente ?? ""),
            new XElement(Covol + "RfcRepresentanteLegal", header.rfc_representante_legal ?? ""),
            new XElement(Covol + "RfcProveedor", header.rfc_proveedor ?? ""),
            new XElement(Covol + "Caracter",
                new XElement(Covol + "TipoCaracter", "permisionario"),
                new XElement(Covol + "ModalidadPermiso", "PER2"),
                new XElement(Covol + "NumPermiso", "PL/3664/EXP/ES/2015")
            ),
            new XElement(Covol + "ClaveInstalacion", header.clave_instalacion ?? ""),
            new XElement(Covol + "DescripcionInstalacion", header.descripcion_instalacion ?? ""),
            new XElement(Covol + "NumeroPozos", header.numero_pozos ?? 0),
            new XElement(Covol + "NumeroTanques", header.numero_tanques ?? 3),
            new XElement(Covol + "NumeroDuctosEntradaSalida", header.numero_ductos_entrada_salida ?? 0),
            new XElement(Covol + "NumeroDuctosTransporteDistribucion", header.numero_ductos_transporte_distribucion ?? 0),
            new XElement(Covol + "NumeroDispensarios", header.numero_dispensarios ?? 4),
            new XElement(Covol + "FechaYHoraCorte", $"{fechaOperacion:yyyy-MM-dd}T23:59:59-06:00")
        );

        foreach (var producto in productos)
        {
            var marca = Convert.ToString(producto.marca_comercial) ?? "";
            var productoLike = ResolverProductoLike(marca);

            var inventario = await cn.QueryFirstOrDefaultAsync(new CommandDefinition(@"
                SELECT
                    volumen_existencias_anterior,
                    volumen_existencias
                FROM covol.inventarios_diarios
                WHERE anio = @anio
                  AND mes = @mes
                  AND dia = @dia
                  AND @marca ILIKE '%' || producto_like || '%'
                ORDER BY 
                    CASE 
                        WHEN producto_like ILIKE '%MAGNA%' THEN 1
                        WHEN producto_like ILIKE '%PREMIUM%' THEN 2
                        WHEN producto_like ILIKE '%DIESEL%' THEN 3
                        ELSE 4
                    END, producto_like
                LIMIT 1;",
                new
                {
                    anio,
                    mes,
                    dia = fechaOperacion.Day,
                    marca
                },
                cancellationToken: ct
            ));

            var resumen = await cn.QueryFirstOrDefaultAsync(new CommandDefinition(@"
                SELECT
                    COALESCE(SUM(t.volumen) FILTER (WHERE t.tipo_movimiento = 'RECEPCION'), 0) AS volumen_recepcion,
                    COALESCE(SUM(t.volumen) FILTER (WHERE t.tipo_movimiento = 'ENTREGA'), 0) AS volumen_entrega,
                    COUNT(*) FILTER (WHERE t.tipo_movimiento = 'RECEPCION') AS total_recepciones,
                    COUNT(*) FILTER (WHERE t.tipo_movimiento = 'ENTREGA') AS total_entregas
                FROM covol.transacciones t
                JOIN covol.productos p ON p.id = t.producto_id
                WHERE t.anio = @anio
                  AND t.mes = @mes
                  AND t.fecha_operacion::date = @fechaOperacion::date
                  AND p.marca_comercial ILIKE '%' || @productoLike || '%';",
                new
                {
                    anio,
                    mes,
                    fechaOperacion = fechaDb,
                    productoLike
                },
                cancellationToken: ct
            ));

            var txs = (await cn.QueryAsync<CovolTransaction>(new CommandDefinition(@"
                SELECT
                    t.tipo_movimiento AS TipoMovimiento,
                    t.fecha_transaccion AS FechaTransaccion,
                    t.rfc_cliente_proveedor AS RfcClienteProveedor,
                    t.nombre_cliente_proveedor AS NombreClienteProveedor,
                    t.permiso_proveedor AS PermisoProveedor,
                    t.cfdi AS Cfdi,
                    t.cfdi_texto AS CfdiTexto,
                    t.tipo_cfdi AS TipoCfdi,
                    t.precio_compra AS PrecioCompra,
                    t.precio_venta_publico AS PrecioVentaPublico,
                    t.precio_venta AS PrecioVenta,
                    t.volumen AS Volumen,
                    t.um AS Um
                FROM covol.transacciones t
                JOIN covol.productos p ON p.id = t.producto_id
                WHERE t.anio = @anio
                  AND t.mes = @mes
                  AND t.fecha_operacion::date = @fechaOperacion::date
                  AND p.marca_comercial ILIKE '%' || @productoLike || '%';",
                new
                {
                    anio,
                    mes,
                    fechaOperacion = fechaDb,
                    productoLike
                },
                cancellationToken: ct
            ))).ToList();

            string? xmlBase = producto.xml_producto_base;
            XElement productoElement;

            string logPath = Path.Combine(outputFolder, "log_generacion_diarios.txt");

            if (!string.IsNullOrWhiteSpace(xmlBase))
            {
                productoElement = XElement.Parse(xmlBase);
                
                var recepcionesTx = txs.Where(x => x.TipoMovimiento == "RECEPCION").ToList();
                var entregasTx = txs.Where(x => x.TipoMovimiento == "ENTREGA").ToList();

                var tanques = productoElement.Elements(Covol + "TANQUE").ToList();
                var mangueras = productoElement.Descendants(Covol + "MANGUERA").ToList();

                File.AppendAllText(logPath, $"[{fechaOperacion:yyyy-MM-dd}] Producto: {productoLike} | Base XML Encontrado | Entregas en BD: {entregasTx.Count} | Mangueras en XML: {mangueras.Count}\n");

                if (tanques.Count > 0)
                {
                    var tanqueElement = tanques.First();
                    tanqueElement.Add(
                        CrearExistencias(fechaOperacion, inventario, resumen),
                        CrearRecepcionesConCFDI(resumen, recepcionesTx, inventario),
                        CrearEntregasVacias()
                    );
                }

                if (mangueras.Count > 0 && entregasTx.Count > 0)
                {
                    ProrratearEntregas(mangueras, entregasTx);
                }
                else if (entregasTx.Count > 0 && mangueras.Count == 0)
                {
                    File.AppendAllText(logPath, $"   => ¡ALERTA! Hay {entregasTx.Count} entregas de {productoLike} pero el archivo base no tiene MANGUERAS. Las entregas no se guardarán.\n");
                }
            }
            else
            {
                var entregasTx = txs.Where(x => x.TipoMovimiento == "ENTREGA").ToList();
                File.AppendAllText(logPath, $"[{fechaOperacion:yyyy-MM-dd}] Producto: {productoLike} | NO hay XML Base | Entregas en BD: {entregasTx.Count} | Mangueras: 0\n");
                if (entregasTx.Count > 0)
                {
                     File.AppendAllText(logPath, $"   => ¡ALERTA! Hay {entregasTx.Count} entregas de {productoLike} pero no hay XML base. Las entregas no se guardarán.\n");
                }

                productoElement = new XElement(Covol + "PRODUCTO",
                    new XElement(Covol + "ClaveProducto", producto.clave_producto ?? ""),
                    new XElement(Covol + "ClaveSubProducto", producto.clave_subproducto ?? "")
                );

                if (producto.octanaje is not null)
                {
                    productoElement.Add(
                        new XElement(Covol + "Gasolina",
                            new XElement(Covol + "ComposOctanajeGasolina", producto.octanaje),
                            new XElement(Covol + "GasolinaConCombustibleNoFosil", producto.combustible_no_fosil ?? "No")
                        )
                    );
                }

                productoElement.Add(new XElement(Covol + "MarcaComercial", producto.marca_comercial ?? ""));

                var tanqueElement = new XElement(Covol + "TANQUE",
                    new XElement(Covol + "ClaveTanque", $"TQ-{productoLike}"),
                    new XElement(Covol + "LocalizacionYODescripcionTanque", $"Tanque {productoLike}"),
                    new XElement(Covol + "VigenciaCalibracionTanque", $"{fechaOperacion.Year}-12-31"),
                    new XElement(Covol + "CapacidadTotalTanque",
                        new XElement(Covol + "ValorNumerico", 0),
                        new XElement(Covol + "UM", "UM03")
                    ),
                    new XElement(Covol + "CapacidadOperativaTanque",
                        new XElement(Covol + "ValorNumerico", 0),
                        new XElement(Covol + "UM", "UM03")
                    ),
                    new XElement(Covol + "CapacidadUtilTanque",
                        new XElement(Covol + "ValorNumerico", 0),
                        new XElement(Covol + "UM", "UM03")
                    ),
                    new XElement(Covol + "FondajeTanque",
                        new XElement(Covol + "ValorNumerico", 0),
                        new XElement(Covol + "UM", "UM03")
                    ),
                    new XElement(Covol + "VolumenMinimoOperacion",
                        new XElement(Covol + "ValorNumerico", 0),
                        new XElement(Covol + "UM", "UM03")
                    ),
                    new XElement(Covol + "EstadoTanque", "O"),
                    new XElement(Covol + "MedicionTanque",
                        new XElement(Covol + "SistemaMedicionTanque", "SMT"),
                        new XElement(Covol + "LocalizODescripSistMedicionTanque", $"Medición {productoLike}"),
                        new XElement(Covol + "VigenciaCalibracionSistMedicionTanque", $"{fechaOperacion.Year}-12-31"),
                        new XElement(Covol + "IncertidumbreMedicionSistMedicionTanque", 0.010)
                    ),
                    CrearExistencias(fechaOperacion, inventario, resumen),
                    CrearRecepcionesVacias(resumen),
                    CrearEntregasVacias()
                );

                productoElement.Add(tanqueElement);
            }

            root.Add(productoElement);
        }

        var bitacora = new XElement(Covol + "BITACORA",
            new XElement(Covol + "NumeroRegistro", 1),
            new XElement(Covol + "FechaYHoraEvento", $"{fechaOperacion:yyyy-MM-dd}T23:59:59-06:00"),
            new XElement(Covol + "UsuarioResponsable", "Eucario León"),
            new XElement(Covol + "TipoEvento", 5),
            new XElement(Covol + "DescripcionEvento", $"Generacion XML Diario {fileName}")
        );

        root.Add(bitacora);

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            root
        );

        var folder = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        doc.Save(outputPath);

        return outputPath;
    }

    public async Task<int> ExportMonthXmlAsync(
        int anio,
        int mes,
        string[] productKeys,
        string[] tiposMovimiento,
        string outputFolder,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputFolder);

        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync(ct);

        var fechas = (await cn.QueryAsync<DateOnly>(new CommandDefinition(@"
            SELECT DISTINCT fecha_operacion::date
            FROM covol.transacciones
            WHERE anio = @anio
              AND mes = @mes
            ORDER BY fecha_operacion::date;",
            new { anio, mes },
            cancellationToken: ct
        ))).ToList();

        var total = 0;

        foreach (var fecha in fechas)
        {
            ct.ThrowIfCancellationRequested();

            var path = await ExportDailyXmlAsync(
                anio,
                mes,
                fecha,
                productKeys,
                tiposMovimiento,
                outputFolder,
                ct
            );

            progress?.Report($"Generado {Path.GetFileName(path)}...");

            total++;
        }

        progress?.Report($"Finalizado. XML generados: {total:N0}");
        return total;
    }

    private static XElement CrearExistencias(DateOnly fechaOperacion, dynamic? inventario, dynamic? resumen)
    {
        return new XElement(Covol + "EXISTENCIAS",
            new XElement(Covol + "VolumenExistenciasAnterior",
                new XElement(Covol + "ValorNumerico", inventario?.volumen_existencias_anterior ?? 0)
            ),
            new XElement(Covol + "VolumenAcumOpsRecepcion",
                new XElement(Covol + "ValorNumerico", resumen?.volumen_recepcion ?? 0),
                new XElement(Covol + "UM", "UM03")
            ),
            new XElement(Covol + "HoraRecepcionAcumulado", "23:59:59-06:00"),
            new XElement(Covol + "VolumenAcumOpsEntrega",
                new XElement(Covol + "ValorNumerico", resumen?.volumen_entrega ?? 0),
                new XElement(Covol + "UM", "UM03")
            ),
            new XElement(Covol + "HoraEntregaAcumulado", "23:59:59-06:00"),
            new XElement(Covol + "VolumenExistencias",
                new XElement(Covol + "ValorNumerico", inventario?.volumen_existencias ?? 0)
            ),
            new XElement(Covol + "FechaYHoraEstaMedicion", $"{fechaOperacion:yyyy-MM-dd}T23:59:59-06:00"),
            new XElement(Covol + "FechaYHoraMedicionAnterior", $"{fechaOperacion.AddDays(-1):yyyy-MM-dd}T23:59:59-06:00")
        );
    }

    private static XElement CrearRecepcionesVacias(dynamic? resumen)
    {
        return new XElement(Covol + "RECEPCIONES",
            new XElement(Covol + "TotalRecepciones", resumen?.total_recepciones ?? 0),
            new XElement(Covol + "SumaVolumenRecepcion",
                new XElement(Covol + "ValorNumerico", resumen?.volumen_recepcion ?? 0),
                new XElement(Covol + "UM", "UM03")
            )
        );
    }

    private static XElement CrearEntregasVacias()
    {
        return new XElement(Covol + "ENTREGAS",
            new XElement(Covol + "TotalEntregas", 0),
            new XElement(Covol + "SumaVolumenEntregado",
                new XElement(Covol + "ValorNumerico", 0),
                new XElement(Covol + "UM", "UM03")
            ),
            new XElement(Covol + "TotalDocumentos", 0)
        );
    }

    private static XElement CrearRecepcionesConCFDI(dynamic? resumen, List<CovolTransaction> txs, dynamic? inventario)
    {
        var root = new XElement(Covol + "RECEPCIONES",
            new XElement(Covol + "TotalRecepciones", txs.Count),
            new XElement(Covol + "SumaVolumenRecepcion",
                new XElement(Covol + "ValorNumerico", txs.Sum(x => x.Volumen ?? 0)),
                new XElement(Covol + "UM", "UM03")
            ),
            new XElement(Covol + "TotalDocumentos", txs.Count)
        );

        decimal volBase = inventario?.volumen_existencias_anterior ?? 0m;
        int i = 1;

        foreach (var t in txs)
        {
            var vol = t.Volumen ?? 0;
            var recepcionElement = new XElement(Covol + "RECEPCION",
                new XElement(Covol + "NumeroDeRegistro", i++),
                new XElement(Covol + "VolumenInicialTanque",
                    new XElement(Covol + "ValorNumerico", volBase),
                    new XElement(Covol + "UM", "UM03")
                ),
                new XElement(Covol + "VolumenFinalTanque",
                    new XElement(Covol + "ValorNumerico", volBase + vol)
                ),
                new XElement(Covol + "VolumenRecepcion",
                    new XElement(Covol + "ValorNumerico", vol),
                    new XElement(Covol + "UM", "UM03")
                ),
                new XElement(Covol + "Temperatura", 20.000),
                new XElement(Covol + "PresionAbsoluta", 101.325),
                new XElement(Covol + "FechaYHoraInicioRecepcion", $"{t.FechaTransaccion:yyyy-MM-ddTHH:mm:sszzz}"),
                new XElement(Covol + "FechaYHoraFinalRecepcion", $"{t.FechaTransaccion.AddMinutes(5):yyyy-MM-ddTHH:mm:sszzz}")
            );
            root.Add(recepcionElement);
            volBase += vol;
        }

        return root;
    }

    private static void ProrratearEntregas(List<XElement> mangueras, List<CovolTransaction> entregas)
    {
        // Round robin a mangueras
        var groups = entregas
            .Select((x, i) => new { Tx = x, Index = i % mangueras.Count })
            .GroupBy(x => x.Index)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Tx).ToList());

        for (int i = 0; i < mangueras.Count; i++)
        {
            var txs = groups.ContainsKey(i) ? groups[i] : new List<CovolTransaction>();
            
            var entregasRoot = new XElement(Covol + "ENTREGAS",
                new XElement(Covol + "TotalEntregas", txs.Count),
                new XElement(Covol + "SumaVolumenEntregado",
                    new XElement(Covol + "ValorNumerico", txs.Sum(x => x.Volumen ?? 0)),
                    new XElement(Covol + "UM", "UM03")
                ),
                new XElement(Covol + "TotalDocumentos", txs.Count)
            );

            int numRegistro = 1;
            decimal volAcumulado = 0m;

            foreach (var t in txs)
            {
                var vol = t.Volumen ?? 0;
                volAcumulado += vol;

                var entregaElem = new XElement(Covol + "ENTREGA",
                    new XElement(Covol + "NumeroDeRegistro", numRegistro++),
                    new XElement(Covol + "TipoDeRegistro", "D"),
                    new XElement(Covol + "VolumenEntregadoTotalizadorAcum",
                        new XElement(Covol + "ValorNumerico", volAcumulado),
                        new XElement(Covol + "UM", "UM03")
                    ),
                    new XElement(Covol + "VolumenEntregadoTotalizadorInsta",
                        new XElement(Covol + "ValorNumerico", vol),
                        new XElement(Covol + "UM", "UM03")
                    ),
                    new XElement(Covol + "FechaYHoraEntrega", $"{t.FechaTransaccion:yyyy-MM-ddTHH:mm:sszzz}"),
                    new XElement(Covol + "Complemento",
                        new XElement(Covol + "Complemento_Expendio",
                            new XElement(Exp + "NACIONAL",
                                new XElement(Exp + "RfcClienteOProveedor", t.RfcClienteProveedor ?? ""),
                                new XElement(Exp + "NombreClienteOProveedor", t.NombreClienteProveedor ?? ""),
                                new XElement(Exp + "PermisoProveedor", t.PermisoProveedor ?? ""),
                                new XElement(Exp + "CFDIs",
                                    new XElement(Exp + "CFDI", t.Cfdi?.ToString().ToUpper() ?? ""),
                                    new XElement(Exp + "TipoCFDI", t.TipoCfdi ?? "Ingreso"),
                                    new XElement(Exp + "PrecioCompra", t.PrecioCompra ?? 0),
                                    new XElement(Exp + "PrecioDeVentaAlPublico", t.PrecioVentaPublico ?? 0),
                                    new XElement(Exp + "PrecioVenta", t.PrecioVenta ?? 0),
                                    new XElement(Exp + "FechaYHoraTransaccion", $"{t.FechaTransaccion:yyyy-MM-ddTHH:mm:sszzz}"),
                                    new XElement(Exp + "VolumenDocumentado",
                                        new XElement(Exp + "ValorNumerico", vol),
                                        new XElement(Exp + "UM", "UM03")
                                    )
                                )
                            )
                        )
                    )
                );
                entregasRoot.Add(entregaElem);
            }

            mangueras[i].Add(entregasRoot);
        }
    }

    private static string ResolverProductoLike(string? marca)
    {
        marca ??= "";

        if (marca.Contains("MAGNA", StringComparison.OrdinalIgnoreCase))
            return "MAGNA";

        if (marca.Contains("PREMIUM", StringComparison.OrdinalIgnoreCase))
            return "PREMIUM";

        if (marca.Contains("DIESEL", StringComparison.OrdinalIgnoreCase))
            return "DIESEL";

        return marca;
    }
}