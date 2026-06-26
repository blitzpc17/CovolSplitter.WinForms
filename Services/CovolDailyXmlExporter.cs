using System.Xml.Linq;
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
                a.descripcion_instalacion
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
            SELECT DISTINCT
                p.id AS producto_id,
                p.clave_producto,
                p.clave_subproducto,
                p.marca_comercial,
                p.octanaje,
                p.combustible_no_fosil
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
            ORDER BY p.marca_comercial;",
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
            new XElement(Covol + "NumeroPozos", 0),
            new XElement(Covol + "NumeroTanques", 3),
            new XElement(Covol + "NumeroDuctosEntradaSalida", 0),
            new XElement(Covol + "NumeroDuctosTransporteDistribucion", 0),
            new XElement(Covol + "NumeroDispensarios", 4),
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
                ORDER BY producto_like
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
                  AND t.fecha_operacion = @fechaOperacion
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

            var productoElement = new XElement(Covol + "PRODUCTO",
                new XElement(Covol + "ClaveProducto", producto.clave_producto ?? ""),
                new XElement(Covol + "ClaveSubProducto", producto.clave_subproducto ?? ""),
                new XElement(Covol + "MarcaComercial", producto.marca_comercial ?? "")
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
                CrearRecepciones(resumen),
                CrearEntregas(resumen)
            );

            productoElement.Add(tanqueElement);
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

        var fechas = (await cn.QueryAsync<DateTime>(new CommandDefinition(@"
            SELECT DISTINCT fecha_operacion::timestamp
            FROM covol.transacciones
            WHERE anio = @anio
              AND mes = @mes
            ORDER BY fecha_operacion;",
            new { anio, mes },
            cancellationToken: ct
        ))).Select(DateOnly.FromDateTime).ToList();

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

    private static XElement CrearRecepciones(dynamic? resumen)
    {
        return new XElement(Covol + "RECEPCIONES",
            new XElement(Covol + "TotalRecepciones", resumen?.total_recepciones ?? 0),
            new XElement(Covol + "SumaVolumenRecepcion",
                new XElement(Covol + "ValorNumerico", resumen?.volumen_recepcion ?? 0),
                new XElement(Covol + "UM", "UM03")
            )
        );
    }

    private static XElement CrearEntregas(dynamic? resumen)
    {
        return new XElement(Covol + "ENTREGAS",
            new XElement(Covol + "TotalEntregas", resumen?.total_entregas ?? 0),
            new XElement(Covol + "SumaVolumenEntregado",
                new XElement(Covol + "ValorNumerico", resumen?.volumen_entrega ?? 0),
                new XElement(Covol + "UM", "UM03")
            )
        );
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