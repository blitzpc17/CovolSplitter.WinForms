using Dapper;
using Npgsql;

namespace CovolSplitter.WinForms.Services;

public sealed class GlobalVariablesRepository
{
    public async Task TestConnectionAsync(string connectionString, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);

        await cn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT 1;",
                cancellationToken: ct
            )
        );
    }

    public async Task EnsureVariablesTableAsync(string connectionString, CancellationToken ct = default)
    {
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);

        var sql = @"
            CREATE TABLE IF NOT EXISTS public.variablesglobales (
                id BIGSERIAL PRIMARY KEY,
                clave VARCHAR(100) NOT NULL UNIQUE,
                valor TEXT NULL,
                descripcion TEXT NULL,
                activo BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );

            CREATE OR REPLACE FUNCTION public.fn_variablesglobales_updated_at()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.updated_at = now();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS trg_variablesglobales_updated_at ON public.variablesglobales;

            CREATE TRIGGER trg_variablesglobales_updated_at
            BEFORE UPDATE ON public.variablesglobales
            FOR EACH ROW
            EXECUTE FUNCTION public.fn_variablesglobales_updated_at();

            INSERT INTO public.variablesglobales
            (
                clave,
                valor,
                descripcion,
                activo
            )
            VALUES
            (
                'SISTEMA_CONNECTION_STRING',
                NULL,
                'Cadena de conexión principal de PostgreSQL para COVOL Splitter',
                TRUE
            )
            ON CONFLICT (clave)
            DO NOTHING;
        ";

        await cn.ExecuteAsync(
            new CommandDefinition(
                sql,
                cancellationToken: ct
            )
        );
    }

    public async Task SaveConnectionStringAsync(string connectionString, CancellationToken ct = default)
    {
        await EnsureVariablesTableAsync(connectionString, ct);

        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);

        await cn.ExecuteAsync(
            new CommandDefinition(
                @"
                INSERT INTO public.variablesglobales
                (
                    clave,
                    valor,
                    descripcion,
                    activo
                )
                VALUES
                (
                    'SISTEMA_CONNECTION_STRING',
                    @ConnectionString,
                    'Cadena de conexión principal de PostgreSQL para COVOL Splitter',
                    TRUE
                )
                ON CONFLICT (clave)
                DO UPDATE SET
                    valor = EXCLUDED.valor,
                    descripcion = EXCLUDED.descripcion,
                    activo = TRUE,
                    updated_at = now();
                ",
                new
                {
                    ConnectionString = connectionString
                },
                cancellationToken: ct
            )
        );
    }

    public async Task<string?> GetConnectionStringAsync(string connectionString, CancellationToken ct = default)
    {
        await EnsureVariablesTableAsync(connectionString, ct);

        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);

        return await cn.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                @"
                SELECT valor
                FROM public.variablesglobales
                WHERE clave = 'SISTEMA_CONNECTION_STRING'
                  AND activo = TRUE
                LIMIT 1;
                ",
                cancellationToken: ct
            )
        );
    }
}