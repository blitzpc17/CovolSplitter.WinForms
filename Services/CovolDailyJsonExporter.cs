using System.Text.Encodings.Web;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace CovolSplitter.WinForms.Services;

public sealed class CovolDailyJsonExporter
{
    private readonly string _connectionString;

    public CovolDailyJsonExporter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task ExportDailyJsonAsync(
        int anio,
        int mes,
        DateOnly fechaOperacion,
        string[] productKeys,
        string[] tiposMovimiento,
        string outputPath,
        CancellationToken ct = default)
    {
        var jsonObject = await BuildDailyJsonObjectAsync(
            anio,
            mes,
            fechaOperacion,
            productKeys,
            tiposMovimiento,
            ct
        );

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(jsonObject, options);

        await File.WriteAllTextAsync(outputPath, json, ct);
    }

    public async Task<int> ExportMonthJsonAsync(
        int anio,
        int mes,
        string[] productKeys,
        string[] tiposMovimiento,
        string outputFolder,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputFolder);

        var fechas = await GetFechasConMovimientosAsync(
            anio,
            mes,
            productKeys,
            tiposMovimiento,
            ct
        );

        var count = 0;

        foreach (var fecha in fechas)
        {
            ct.ThrowIfCancellationRequested();

            var fileName = $"D_{fecha:yyyy-MM-dd}.json";
            var outputPath = Path.Combine(outputFolder, fileName);

            progress?.Report($"Generando {fileName}...");

            await ExportDailyJsonAsync(
                anio,
                mes,
                fecha,
                productKeys,
                tiposMovimiento,
                outputPath,
                ct
            );

            count++;
        }

        progress?.Report($"Finalizado. JSON generados: {count:N0}");

        return count;
    }

    private async Task<List<DateOnly>> GetFechasConMovimientosAsync(
        int anio,
        int mes,
        string[] productKeys,
        string[] tiposMovimiento,
        CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync(ct);

        var rows = await cn.QueryAsync<DateTime>(new CommandDefinition(@"
            SELECT DISTINCT
                t.fecha_operacion
            FROM covol.transacciones t
            JOIN covol.productos p ON p.id = t.producto_id
            WHERE t.anio = @anio
              AND t.mes = @mes
              AND (
                    @filtrarProductos = FALSE
                    OR (
                        p.clave_producto || '|' ||
                        COALESCE(p.clave_subproducto, '') || '|' ||
                        COALESCE(p.marca_comercial, '')
                    ) = ANY(@productKeys)
              )
              AND (
                    @filtrarMovimientos = FALSE
                    OR t.tipo_movimiento = ANY(@tiposMovimiento)
              )
            ORDER BY t.fecha_operacion;",
            new
            {
                anio,
                mes,
                productKeys,
                tiposMovimiento,
                filtrarProductos = productKeys.Length > 0,
                filtrarMovimientos = tiposMovimiento.Length > 0
            },
            cancellationToken: ct
        ));

        return rows
            .Select(DateOnly.FromDateTime)
            .ToList();
    }

    private async Task<object> BuildDailyJsonObjectAsync(
        int anio,
        int mes,
        DateOnly fechaOperacion,
        string[] productKeys,
        string[] tiposMovimiento,
        CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync(ct);

        var fechaOperacionDb = fechaOperacion.ToDateTime(TimeOnly.MinValue);

        var header = await cn.QueryFirstOrDefaultAsync(new CommandDefinition(@"
            SELECT
                a.rfc_contribuyente,
                a.rfc_representante_legal,
                a.rfc_proveedor,
                a.clave_instalacion,
                a.descripcion_instalacion,
                @fechaOperacion::date AS fecha_operacion
            FROM covol.archivos a
            WHERE a.anio = @anio
              AND a.mes = @mes
            ORDER BY a.tipo_archivo, a.id
            LIMIT 1;",
            new
            {
                anio,
                mes,
                fechaOperacion = fechaOperacionDb
            },
            cancellationToken: ct
        ));

        if (header is null)
            throw new InvalidOperationException("No se encontró información base para generar el JSON diario.");

        var rows = (await cn.QueryAsync(new CommandDefinition(@"
            SELECT
                p.clave_producto,
                p.clave_subproducto,
                p.marca_comercial,
                p.octanaje,
                p.combustible_no_fosil,
                t.tipo_movimiento,
                t.fecha_transaccion,
                t.rfc_cliente_proveedor,
                t.nombre_cliente_proveedor,
                t.permiso_proveedor,
                t.cfdi_texto,
                t.tipo_cfdi,
                t.precio_compra,
                t.precio_venta_publico,
                t.precio_venta,
                t.volumen,
                t.um,
                t.numero_registro,
                t.tipo_registro,
                t.dispensario,
                t.manguera,
                t.tanque,
                t.volumen_totalizador_acum,
                t.volumen_totalizador_insta
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
              AND (
                    @filtrarMovimientos = FALSE
                    OR t.tipo_movimiento = ANY(@tiposMovimiento)
              )
            ORDER BY
                p.marca_comercial,
                t.tipo_movimiento,
                t.fecha_transaccion;",
            new
            {
                anio,
                mes,
                fechaOperacion = fechaOperacionDb,
                productKeys,
                tiposMovimiento,
                filtrarProductos = productKeys.Length > 0,
                filtrarMovimientos = tiposMovimiento.Length > 0
            },
            cancellationToken: ct
        ))).ToList();

        var productos = rows
            .GroupBy(x => new
            {
                x.clave_producto,
                x.clave_subproducto,
                x.marca_comercial,
                x.octanaje,
                x.combustible_no_fosil
            })
            .Select(g => new
            {
                claveProducto = g.Key.clave_producto,
                claveSubProducto = g.Key.clave_subproducto,
                marcaComercial = g.Key.marca_comercial,
                gasolina = new
                {
                    composOctanajeGasolina = g.Key.octanaje,
                    gasolinaConCombustibleNoFosil = g.Key.combustible_no_fosil
                },
                recepciones = g
                    .Where(x => x.tipo_movimiento == "RECEPCION")
                    .Select(x => new
                    {
                        numeroRegistro = x.numero_registro,
                        tipoRegistro = x.tipo_registro,
                        tanque = x.tanque,
                        fechaYHoraTransaccion = x.fecha_transaccion,
                        rfcClienteOProveedor = x.rfc_cliente_proveedor,
                        nombreClienteOProveedor = x.nombre_cliente_proveedor,
                        permisoProveedor = x.permiso_proveedor,
                        cfdi = x.cfdi_texto,
                        tipoCfdi = x.tipo_cfdi,
                        precioCompra = x.precio_compra,
                        precioDeVentaAlPublico = x.precio_venta_publico,
                        precioVenta = x.precio_venta,
                        volumenDocumentado = new
                        {
                            valorNumerico = x.volumen,
                            um = x.um
                        }
                    })
                    .ToList(),
                entregas = g
                    .Where(x => x.tipo_movimiento == "ENTREGA")
                    .Select(x => new
                    {
                        numeroRegistro = x.numero_registro,
                        tipoRegistro = x.tipo_registro,
                        dispensario = x.dispensario,
                        manguera = x.manguera,
                        tanque = x.tanque,
                        fechaYHoraTransaccion = x.fecha_transaccion,
                        rfcClienteOProveedor = x.rfc_cliente_proveedor,
                        nombreClienteOProveedor = x.nombre_cliente_proveedor,
                        cfdi = x.cfdi_texto,
                        tipoCfdi = x.tipo_cfdi,
                        precioCompra = x.precio_compra,
                        precioDeVentaAlPublico = x.precio_venta_publico,
                        precioVenta = x.precio_venta,
                        volumenDocumentado = new
                        {
                            valorNumerico = x.volumen,
                            um = x.um
                        },
                        volumenEntregadoTotalizadorAcum = x.volumen_totalizador_acum,
                        volumenEntregadoTotalizadorInsta = x.volumen_totalizador_insta
                    })
                    .ToList()
            })
            .ToList();

        return new
        {
            tipoArchivo = "DIARIO_DERIVADO",
            generadoDesdeBaseDatos = true,
            version = "1.0",
            rfcContribuyente = header.rfc_contribuyente,
            rfcRepresentanteLegal = header.rfc_representante_legal,
            rfcProveedor = header.rfc_proveedor,
            claveInstalacion = header.clave_instalacion,
            descripcionInstalacion = header.descripcion_instalacion,
            fechaOperacion = fechaOperacion.ToString("yyyy-MM-dd"),
            anio,
            mes,
            dia = fechaOperacion.Day,
            totalProductos = productos.Count,
            totalRegistros = rows.Count,
            totalRecepciones = rows.Count(x => x.tipo_movimiento == "RECEPCION"),
            totalEntregas = rows.Count(x => x.tipo_movimiento == "ENTREGA"),
            productos
        };
    }
}