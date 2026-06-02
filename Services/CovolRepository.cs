using CovolSplitter.Winforms.Models;
using CovolSplitter.WinForms.Models;
using Dapper;
using Npgsql;

namespace CovolSplitter.WinForms.Services;

public sealed class CovolRepository
{
    private readonly string _connectionString;

    public CovolRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<long> SavePackageAsync(
        CovolFilePackage package,
        List<CovolProduct> products,
        List<CovolTransaction> transactions,
        IProgress<CovolImportProgress>? progress,
        CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync(ct);

        await using var dbTx = await cn.BeginTransactionAsync(ct);

        try
        {
            var archivoId = await cn.ExecuteScalarAsync<long>(new CommandDefinition(@"
                INSERT INTO covol.archivos
                (
                    tipo_archivo,
                    uuid_archivo,
                    nombre_archivo,
                    sha256,
                    version_xml,
                    rfc_contribuyente,
                    rfc_representante_legal,
                    rfc_proveedor,
                    clave_instalacion,
                    descripcion_instalacion,
                    fecha_reporte,
                    fecha_operacion,
                    anio,
                    mes,
                    dia,
                    anio_mes,
                    total_productos,
                    total_transacciones,
                    total_recepciones,
                    total_entregas
                )
                VALUES
                (
                    @TipoArchivo,
                    @UuidArchivo,
                    @NombreArchivo,
                    @Sha256,
                    @VersionXml,
                    @RfcContribuyente,
                    @RfcRepresentanteLegal,
                    @RfcProveedor,
                    @ClaveInstalacion,
                    @DescripcionInstalacion,
                    @FechaReporte,
                    @FechaOperacion,
                    @Anio,
                    @Mes,
                    @Dia,
                    @AnioMes,
                    @TotalProductos,
                    @TotalTransacciones,
                    @TotalRecepciones,
                    @TotalEntregas
                )
                RETURNING id;",
                package,
                dbTx,
                cancellationToken: ct
            ));

            package.Id = archivoId;

            foreach (var p in products)
            {
                p.ArchivoId = archivoId;

                p.Id = await cn.ExecuteScalarAsync<long>(new CommandDefinition(@"
                    INSERT INTO covol.productos
                    (
                        archivo_id,
                        clave_producto,
                        clave_subproducto,
                        marca_comercial,
                        octanaje,
                        combustible_no_fosil
                    )
                    VALUES
                    (
                        @ArchivoId,
                        @ClaveProducto,
                        @ClaveSubProducto,
                        @MarcaComercial,
                        @Octanaje,
                        @CombustibleNoFosil
                    )
                    RETURNING id;",
                    p,
                    dbTx,
                    cancellationToken: ct
                ));
            }

            var map = products.ToDictionary(x => x.TempId, x => x.Id);

            foreach (var t in transactions)
            {
                t.ArchivoId = archivoId;
                t.ProductoId = map[t.ProductTempId];
            }

            const int batchSize = 2000;

            for (var i = 0; i < transactions.Count; i += batchSize)
            {
                var batch = transactions
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO covol.transacciones
                    (
                        archivo_id,
                        producto_id,
                        tipo_paquete,
                        tipo_movimiento,
                        fecha_transaccion,
                        fecha_operacion,
                        anio,
                        mes,
                        dia,
                        anio_mes,
                        rfc_cliente_proveedor,
                        nombre_cliente_proveedor,
                        permiso_proveedor,
                        cfdi,
                        cfdi_texto,
                        tipo_cfdi,
                        precio_compra,
                        precio_venta_publico,
                        precio_venta,
                        volumen,
                        um,
                        numero_registro,
                        tipo_registro,
                        dispensario,
                        manguera,
                        tanque,
                        volumen_totalizador_acum,
                        volumen_totalizador_insta
                    )
                    VALUES
                    (
                        @ArchivoId,
                        @ProductoId,
                        @TipoPaquete,
                        @TipoMovimiento,
                        @FechaTransaccion,
                        @FechaOperacion,
                        @Anio,
                        @Mes,
                        @Dia,
                        @AnioMes,
                        @RfcClienteProveedor,
                        @NombreClienteProveedor,
                        @PermisoProveedor,
                        @Cfdi,
                        @CfdiTexto,
                        @TipoCfdi,
                        @PrecioCompra,
                        @PrecioVentaPublico,
                        @PrecioVenta,
                        @Volumen,
                        @Um,
                        @NumeroRegistro,
                        @TipoRegistro,
                        @Dispensario,
                        @Manguera,
                        @Tanque,
                        @VolumenTotalizadorAcum,
                        @VolumenTotalizadorInsta
                    );",
                    batch,
                    dbTx,
                    cancellationToken: ct
                ));

                progress?.Report(new CovolImportProgress
                {
                    Stage = "Guardando transacciones",
                    Percent = 85 + (int)(14.0 * Math.Min(i + batch.Count, transactions.Count) / Math.Max(1, transactions.Count)),
                    Products = products.Count,
                    Transactions = Math.Min(i + batch.Count, transactions.Count)
                });
            }

            await GenerateDerivedDailyFilesAsync(cn, dbTx, archivoId, package, ct);

            await dbTx.CommitAsync(ct);

            progress?.Report(new CovolImportProgress
            {
                Stage = "Finalizado",
                Percent = 100,
                Products = products.Count,
                Transactions = transactions.Count
            });

            return archivoId;
        }
        catch
        {
            await dbTx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task GenerateDerivedDailyFilesAsync(
        NpgsqlConnection cn,
        NpgsqlTransaction dbTx,
        long archivoId,
        CovolFilePackage package,
        CancellationToken ct)
    {
        if (package.TipoArchivo != "M")
            return;

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO covol.diarios_derivados
            (
                archivo_mensual_id,
                fecha_operacion,
                anio,
                mes,
                dia,
                anio_mes,
                total_recepciones,
                total_entregas,
                total_volumen_recepciones,
                total_volumen_entregas
            )
            SELECT
                @archivoId AS archivo_mensual_id,
                t.fecha_operacion,
                t.anio,
                t.mes,
                t.dia,
                t.anio_mes,
                COUNT(*) FILTER (WHERE t.tipo_movimiento = 'RECEPCION') AS total_recepciones,
                COUNT(*) FILTER (WHERE t.tipo_movimiento = 'ENTREGA') AS total_entregas,
                COALESCE(SUM(t.volumen) FILTER (WHERE t.tipo_movimiento = 'RECEPCION'), 0) AS total_volumen_recepciones,
                COALESCE(SUM(t.volumen) FILTER (WHERE t.tipo_movimiento = 'ENTREGA'), 0) AS total_volumen_entregas
            FROM covol.transacciones t
            WHERE t.archivo_id = @archivoId
            GROUP BY
                t.fecha_operacion,
                t.anio,
                t.mes,
                t.dia,
                t.anio_mes
            ON CONFLICT (archivo_mensual_id, fecha_operacion)
            DO UPDATE SET
                total_recepciones = EXCLUDED.total_recepciones,
                total_entregas = EXCLUDED.total_entregas,
                total_volumen_recepciones = EXCLUDED.total_volumen_recepciones,
                total_volumen_entregas = EXCLUDED.total_volumen_entregas;",
            new { archivoId },
            dbTx,
            cancellationToken: ct
        ));
    }

    public async Task<IEnumerable<dynamic>> GetArchivosAsync(int? anio = null, int? mes = null)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        return await cn.QueryAsync(@"
            SELECT
                a.id,
                CASE
                    WHEN a.tipo_archivo = 'M' THEN 'Mensual'
                    WHEN a.tipo_archivo = 'D' THEN 'Diario'
                    ELSE a.tipo_archivo
                END AS tipo,
                a.nombre_archivo,
                a.rfc_contribuyente,
                a.rfc_proveedor,
                a.clave_instalacion,
                a.descripcion_instalacion,
                a.fecha_reporte,
                a.fecha_operacion,
                a.anio,
                a.mes,
                a.dia,
                a.total_productos,
                a.total_transacciones,
                a.total_recepciones,
                a.total_entregas,
                a.created_at
            FROM covol.archivos a
            WHERE (@anio IS NULL OR a.anio = @anio)
              AND (@mes IS NULL OR a.mes = @mes)
            ORDER BY a.created_at DESC, a.id DESC;",
            new { anio, mes }
        );
    }

    public async Task<IEnumerable<dynamic>> GetMonthlySummaryAsync(int anio, int mes, string? rfc = null)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        return await cn.QueryAsync(@"
            SELECT
                p.marca_comercial,
                t.tipo_movimiento,
                COUNT(*) AS movimientos,
                SUM(COALESCE(t.volumen, 0)) AS volumen,
                SUM(COALESCE(t.volumen, 0) * COALESCE(t.precio_venta, 0)) AS importe_estimado
            FROM covol.transacciones t
            JOIN covol.archivos a ON a.id = t.archivo_id
            JOIN covol.productos p ON p.id = t.producto_id
            WHERE t.anio = @anio
              AND t.mes = @mes
              AND (@rfc IS NULL OR a.rfc_contribuyente = @rfc)
            GROUP BY
                p.marca_comercial,
                t.tipo_movimiento
            ORDER BY
                p.marca_comercial,
                t.tipo_movimiento;",
            new { anio, mes, rfc }
        );
    }

    public async Task<IEnumerable<FilterOption>> GetProductosFiltroAsync(int anio, int mes)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        return await cn.QueryAsync<FilterOption>(@"
            SELECT DISTINCT
                (
                    p.clave_producto || '|' ||
                    COALESCE(p.clave_subproducto, '') || '|' ||
                    COALESCE(p.marca_comercial, '')
                ) AS ""Value"",
                (
                    COALESCE(p.marca_comercial, 'SIN MARCA') ||
                    ' - ' ||
                    COALESCE(p.clave_producto, '') ||
                    CASE
                        WHEN p.clave_subproducto IS NULL OR p.clave_subproducto = '' THEN ''
                        ELSE ' / ' || p.clave_subproducto
                    END
                ) AS ""Text""
            FROM covol.transacciones t
            JOIN covol.productos p ON p.id = t.producto_id
            WHERE t.anio = @anio
              AND t.mes = @mes
            ORDER BY ""Text"";",
            new { anio, mes }
        );
    }

    public async Task<IEnumerable<FilterOption>> GetTiposMovimientoFiltroAsync(int anio, int mes)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        return await cn.QueryAsync<FilterOption>(@"
            SELECT DISTINCT
                t.tipo_movimiento AS ""Value"",
                t.tipo_movimiento AS ""Text""
            FROM covol.transacciones t
            WHERE t.anio = @anio
              AND t.mes = @mes
            ORDER BY t.tipo_movimiento;",
            new { anio, mes }
        );
    }

    public async Task<IEnumerable<dynamic>> GetDiariosPorMesAsync(
        int anio,
        int mes,
        string[] productKeys,
        string[] tiposMovimiento)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        return await cn.QueryAsync(@"
            SELECT
                t.fecha_operacion,
                t.anio,
                t.mes,
                t.dia,
                p.marca_comercial,
                p.clave_producto,
                p.clave_subproducto,
                t.tipo_movimiento,
                COUNT(*) AS movimientos,
                SUM(COALESCE(t.volumen, 0)) AS volumen,
                SUM(COALESCE(t.volumen, 0) * COALESCE(t.precio_venta, 0)) AS importe_estimado
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
            GROUP BY
                t.fecha_operacion,
                t.anio,
                t.mes,
                t.dia,
                p.marca_comercial,
                p.clave_producto,
                p.clave_subproducto,
                t.tipo_movimiento
            ORDER BY
                t.fecha_operacion,
                p.marca_comercial,
                t.tipo_movimiento;",
            new
            {
                anio,
                mes,
                productKeys,
                tiposMovimiento,
                filtrarProductos = productKeys.Length > 0,
                filtrarMovimientos = tiposMovimiento.Length > 0
            }
        );
    }

    public async Task<IEnumerable<dynamic>> GetDetalleDiarioPorFiltrosAsync(
        int anio,
        int mes,
        DateOnly fechaOperacion,
        string[] productKeys,
        string[] tiposMovimiento)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        var fechaOperacionDb = fechaOperacion.ToDateTime(TimeOnly.MinValue);

        return await cn.QueryAsync(@"
            SELECT
                t.id,
                t.fecha_transaccion,
                t.fecha_operacion,
                p.marca_comercial,
                p.clave_producto,
                p.clave_subproducto,
                t.tipo_movimiento,
                t.rfc_cliente_proveedor,
                t.nombre_cliente_proveedor,
                t.cfdi_texto,
                t.tipo_cfdi,
                t.precio_compra,
                t.precio_venta_publico,
                t.precio_venta,
                t.volumen,
                t.um,
                t.dispensario,
                t.manguera,
                t.tanque,
                t.numero_registro,
                t.tipo_registro
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
                t.fecha_transaccion,
                p.marca_comercial,
                t.tipo_movimiento;",
            new
            {
                anio,
                mes,
                fechaOperacion = fechaOperacionDb,
                productKeys,
                tiposMovimiento,
                filtrarProductos = productKeys.Length > 0,
                filtrarMovimientos = tiposMovimiento.Length > 0
            }
        );
    }

    public async Task<IEnumerable<dynamic>> GetDerivedDailyFilesAsync(long archivoMensualId)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        return await cn.QueryAsync(@"
            SELECT
                id,
                archivo_mensual_id,
                fecha_operacion,
                anio,
                mes,
                dia,
                anio_mes,
                total_recepciones,
                total_entregas,
                total_volumen_recepciones,
                total_volumen_entregas
            FROM covol.diarios_derivados
            WHERE archivo_mensual_id = @archivoMensualId
            ORDER BY fecha_operacion;",
            new { archivoMensualId }
        );
    }

    public async Task<IEnumerable<dynamic>> GetDailyTransactionsAsync(
        long archivoId,
        DateOnly fechaOperacion,
        string? tipoMovimiento = null)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        var fechaOperacionDb = fechaOperacion.ToDateTime(TimeOnly.MinValue);

        return await cn.QueryAsync(@"
            SELECT
                t.id,
                t.fecha_transaccion,
                t.fecha_operacion,
                p.marca_comercial,
                t.tipo_movimiento,
                t.rfc_cliente_proveedor,
                t.nombre_cliente_proveedor,
                t.cfdi_texto,
                t.tipo_cfdi,
                t.precio_compra,
                t.precio_venta_publico,
                t.precio_venta,
                t.volumen,
                t.um,
                t.dispensario,
                t.manguera,
                t.tanque,
                t.numero_registro,
                t.tipo_registro
            FROM covol.transacciones t
            JOIN covol.productos p ON p.id = t.producto_id
            WHERE t.archivo_id = @archivoId
              AND t.fecha_operacion = @fechaOperacion
              AND (@tipoMovimiento IS NULL OR t.tipo_movimiento = @tipoMovimiento)
            ORDER BY t.fecha_transaccion, p.marca_comercial;",
            new
            {
                archivoId,
                fechaOperacion = fechaOperacionDb,
                tipoMovimiento
            }
        );
    }





    public async Task<IEnumerable<FilterOption>> GetDiasDisponiblesXmlAsync(int anio, int mes)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        return await cn.QueryAsync<FilterOption>(@"
        SELECT
            t.dia::text AS ""Value"",
            (
                LPAD(t.dia::text, 2, '0') ||
                '/' ||
                LPAD(t.mes::text, 2, '0') ||
                '/' ||
                t.anio::text
            ) AS ""Text""
        FROM covol.transacciones t
        WHERE t.anio = @anio
          AND t.mes = @mes
        GROUP BY
            t.anio,
            t.mes,
            t.dia
        ORDER BY
            t.dia;",
            new { anio, mes }
        );
    }

    public async Task<bool> TieneMovimientosPeriodoAsync(int anio, int mes)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        return await cn.ExecuteScalarAsync<bool>(@"
        SELECT EXISTS (
            SELECT 1
            FROM covol.transacciones
            WHERE anio = @anio
              AND mes = @mes
            LIMIT 1
        );",
            new { anio, mes }
        );
    }


}