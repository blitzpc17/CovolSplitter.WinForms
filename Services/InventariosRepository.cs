using Dapper;
using Npgsql;
using CovolSplitter.WinForms.Models;

namespace CovolSplitter.WinForms.Services;

public sealed class InventariosRepository
{
    private readonly string _connectionString;

    public InventariosRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task GuardarInventariosAsync(
        IEnumerable<InventarioDiario> inventarios,
        CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync(ct);

        await using var tx = await cn.BeginTransactionAsync(ct);

        try
        {
            foreach (var item in inventarios)
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO covol.inventarios_diarios
                    (
                        anio,
                        mes,
                        dia,
                        fecha_operacion,
                        producto_like,
                        volumen_existencias,
                        volumen_existencias_anterior,
                        archivo_origen
                    )
                    VALUES
                    (
                        @Anio,
                        @Mes,
                        @Dia,
                        @FechaOperacion,
                        @ProductoLike,
                        @VolumenExistencias,
                        @VolumenExistenciasAnterior,
                        @ArchivoOrigen
                    )
                    ON CONFLICT (anio, mes, dia, producto_like)
                    DO UPDATE SET
                        fecha_operacion = EXCLUDED.fecha_operacion,
                        volumen_existencias = EXCLUDED.volumen_existencias,
                        volumen_existencias_anterior = EXCLUDED.volumen_existencias_anterior,
                        archivo_origen = EXCLUDED.archivo_origen,
                        updated_at = now();",
                    item,
                    tx,
                    cancellationToken: ct
                ));
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IEnumerable<dynamic>> GetInventariosMesAsync(int anio, int mes)
    {
        await using var cn = new NpgsqlConnection(_connectionString);

        return await cn.QueryAsync(@"
            SELECT
                fecha_operacion,
                dia,
                producto_like,
                volumen_existencias_anterior,
                volumen_existencias,
                archivo_origen,
                updated_at
            FROM covol.inventarios_diarios
            WHERE anio = @anio
              AND mes = @mes
            ORDER BY fecha_operacion, producto_like;",
            new { anio, mes }
        );
    }
}