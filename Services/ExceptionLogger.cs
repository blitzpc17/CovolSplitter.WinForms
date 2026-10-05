using Dapper;
using Npgsql;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CovolSplitter.WinForms.Services;

public static class ExceptionLogger
{
    // Mantenemos la cadena de conexión global si es posible
    public static string? GlobalConnectionString { get; set; }

    public static async Task LogExceptionAsync(Exception ex, string? connectionString = null)
    {
        var connStr = connectionString ?? GlobalConnectionString;

        // Si no hay conexión guardada, no podemos guardar en BD, pero podríamos escribir en un archivo txt local como fallback
        if (string.IsNullOrWhiteSpace(connStr))
            return;

        try
        {
            var stackTrace = new StackTrace(ex, true);
            var frame = stackTrace.GetFrame(0);
            var clase = frame?.GetMethod()?.DeclaringType?.Name ?? "Desconocida";
            var metodo = frame?.GetMethod()?.Name ?? "Desconocido";

            using (var cn = new NpgsqlConnection(connStr))
            {
                await cn.OpenAsync();
                var sql = @"
                    INSERT INTO public.excepciones_log (clase, metodo, mensaje, stacktrace)
                    VALUES (@Clase, @Metodo, @Mensaje, @StackTrace);
                ";
                await cn.ExecuteAsync(sql, new
                {
                    Clase = clase,
                    Metodo = metodo,
                    Mensaje = ex.Message,
                    StackTrace = ex.StackTrace ?? string.Empty
                });
            }
        }
        catch
        {
            // Ignorar errores al guardar logs para no causar un bucle infinito
        }
    }

    public static void LogException(Exception ex, string? connectionString = null)
    {
        // Fire and forget
        _ = LogExceptionAsync(ex, connectionString);
    }
}
