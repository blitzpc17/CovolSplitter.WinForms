using Dapper;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace CovolSplitter.WinForms.Services;

public static class DatabaseSetupService
{
    public static async Task CreateDatabaseAndSchemaAsync(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database;
        builder.Database = "postgres"; // Connect to default database

        using (var cn = new NpgsqlConnection(builder.ToString()))
        {
            await cn.OpenAsync();
            
            // Check if DB exists
            bool exists = await cn.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT FROM pg_database WHERE datname = @dbName)", 
                new { dbName });
            
            if (!exists)
            {
                await cn.ExecuteAsync($"CREATE DATABASE \"{dbName}\";");
            }
        }

        // Connect to the actual database
        using (var cn = new NpgsqlConnection(connectionString))
        {
            await cn.OpenAsync();
            
            var sql = @"
                CREATE SCHEMA IF NOT EXISTS covol;

                -- 1. Tabla de Variables Globales (Public)
                CREATE TABLE IF NOT EXISTS public.variablesglobales (
                    id BIGSERIAL PRIMARY KEY,
                    clave VARCHAR(100) NOT NULL UNIQUE,
                    valor TEXT,
                    descripcion TEXT,
                    activo BOOLEAN NOT NULL DEFAULT TRUE,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
                );

                -- 1.5. Tabla de Excepciones / Logs (Public)
                CREATE TABLE IF NOT EXISTS public.excepciones_log (
                    id BIGSERIAL PRIMARY KEY,
                    fecha TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    clase VARCHAR(255),
                    metodo VARCHAR(255),
                    mensaje TEXT,
                    stacktrace TEXT
                );
                
                -- 2. Tabla Empresas
                CREATE TABLE IF NOT EXISTS covol.empresas (
                    id SERIAL PRIMARY KEY,
                    nombre VARCHAR(255) NOT NULL UNIQUE
                );

                -- 3. Tabla Empresa Tanques
                CREATE TABLE IF NOT EXISTS covol.empresa_tanques (
                    id SERIAL PRIMARY KEY,
                    empresa_id INT REFERENCES covol.empresas(id) ON DELETE CASCADE,
                    producto VARCHAR(50) NOT NULL,
                    xml_tanque TEXT NOT NULL,
                    UNIQUE (empresa_id, producto)
                );

                -- 4. Tabla Archivos
                CREATE TABLE IF NOT EXISTS covol.archivos (
                    id BIGSERIAL PRIMARY KEY,
                    tipo_archivo VARCHAR(50) DEFAULT 'M',
                    uuid_archivo UUID,
                    nombre_archivo VARCHAR(255) NOT NULL,
                    sha256 VARCHAR(64) NOT NULL,
                    version_xml VARCHAR(50),
                    rfc_contribuyente VARCHAR(20) NOT NULL,
                    rfc_representante_legal VARCHAR(20),
                    rfc_proveedor VARCHAR(20),
                    clave_instalacion VARCHAR(100) NOT NULL,
                    descripcion_instalacion TEXT,
                    numero_pozos INT NOT NULL DEFAULT 0,
                    numero_tanques INT NOT NULL DEFAULT 0,
                    numero_ductos_entrada_salida INT NOT NULL DEFAULT 0,
                    numero_ductos_transporte_distribucion INT NOT NULL DEFAULT 0,
                    numero_dispensarios INT NOT NULL DEFAULT 0,
                    fecha_reporte TIMESTAMPTZ,
                    fecha_operacion DATE,
                    anio SMALLINT NOT NULL,
                    mes SMALLINT NOT NULL,
                    dia SMALLINT,
                    anio_mes INT NOT NULL DEFAULT 0,
                    total_productos INT NOT NULL DEFAULT 0,
                    total_transacciones INT NOT NULL DEFAULT 0,
                    total_recepciones INT NOT NULL DEFAULT 0,
                    total_entregas INT NOT NULL DEFAULT 0,
                    created_at TIMESTAMPTZ DEFAULT NOW(),
                    updated_at TIMESTAMPTZ DEFAULT NOW()
                );

                -- 5. Tabla Productos
                CREATE TABLE IF NOT EXISTS covol.productos (
                    id BIGSERIAL PRIMARY KEY,
                    archivo_id BIGINT NOT NULL REFERENCES covol.archivos(id) ON DELETE CASCADE,
                    clave_producto VARCHAR(100) NOT NULL,
                    clave_subproducto VARCHAR(100),
                    marca_comercial VARCHAR(255),
                    octanaje INT,
                    combustible_no_fosil VARCHAR(100),
                    xml_producto_base TEXT
                );

                -- 6. Tabla Transacciones
                CREATE TABLE IF NOT EXISTS covol.transacciones (
                    id BIGSERIAL PRIMARY KEY,
                    archivo_id BIGINT NOT NULL REFERENCES covol.archivos(id) ON DELETE CASCADE,
                    producto_id BIGINT NOT NULL REFERENCES covol.productos(id) ON DELETE CASCADE,
                    tipo_paquete VARCHAR(50) DEFAULT 'M',
                    tipo_movimiento VARCHAR(100) NOT NULL,
                    fecha_transaccion TIMESTAMPTZ NOT NULL,
                    fecha_operacion DATE NOT NULL,
                    anio SMALLINT NOT NULL,
                    mes SMALLINT NOT NULL,
                    dia SMALLINT NOT NULL,
                    anio_mes INT NOT NULL DEFAULT 0,
                    rfc_cliente_proveedor VARCHAR(20),
                    nombre_cliente_proveedor VARCHAR(255),
                    permiso_proveedor VARCHAR(100),
                    cfdi UUID,
                    cfdi_texto VARCHAR(255),
                    tipo_cfdi VARCHAR(50),
                    precio_compra DECIMAL(18, 6),
                    precio_venta_publico DECIMAL(18, 6),
                    precio_venta DECIMAL(18, 6),
                    volumen DECIMAL(18, 6),
                    um VARCHAR(50),
                    numero_registro INT,
                    tipo_registro VARCHAR(50),
                    dispensario VARCHAR(100),
                    manguera VARCHAR(100),
                    tanque VARCHAR(100),
                    volumen_totalizador_acum DECIMAL(18, 6),
                    volumen_totalizador_insta DECIMAL(18, 6)
                );

                -- 7. Tabla Diarios Derivados
                CREATE TABLE IF NOT EXISTS covol.diarios_derivados (
                    id BIGSERIAL PRIMARY KEY,
                    archivo_mensual_id BIGINT NOT NULL REFERENCES covol.archivos(id) ON DELETE CASCADE,
                    fecha_operacion DATE NOT NULL,
                    anio INT NOT NULL,
                    mes INT NOT NULL,
                    dia INT NOT NULL,
                    anio_mes INT NOT NULL,
                    total_recepciones INT NOT NULL DEFAULT 0,
                    total_entregas INT NOT NULL DEFAULT 0,
                    total_volumen_recepciones DECIMAL(18,6) NOT NULL DEFAULT 0,
                    total_volumen_entregas DECIMAL(18,6) NOT NULL DEFAULT 0,
                    UNIQUE (archivo_mensual_id, fecha_operacion)
                );

                -- 8. Tabla Inventarios Diarios
                CREATE TABLE IF NOT EXISTS covol.inventarios_diarios (
                    id BIGSERIAL PRIMARY KEY,
                    anio INT NOT NULL,
                    mes INT NOT NULL,
                    dia INT NOT NULL,
                    fecha_operacion DATE NOT NULL,
                    producto_like VARCHAR(100) NOT NULL,
                    volumen_existencias DECIMAL(18, 6) NOT NULL,
                    volumen_existencias_anterior DECIMAL(18, 6),
                    archivo_origen VARCHAR(255),
                    created_at TIMESTAMPTZ DEFAULT NOW(),
                    updated_at TIMESTAMPTZ DEFAULT NOW(),
                    UNIQUE (anio, mes, dia, producto_like)
                );

                -- Crear índices si no existen
                CREATE INDEX IF NOT EXISTS idx_covol_archivos_sha256 ON covol.archivos(sha256);
                CREATE INDEX IF NOT EXISTS idx_covol_productos_archivo ON covol.productos(archivo_id);
                CREATE INDEX IF NOT EXISTS idx_covol_trans_archivo ON covol.transacciones(archivo_id);
                CREATE INDEX IF NOT EXISTS idx_covol_trans_producto ON covol.transacciones(producto_id);
                CREATE INDEX IF NOT EXISTS idx_covol_trans_fecha_op ON covol.transacciones(fecha_operacion);
            ";
            await cn.ExecuteAsync(sql);
        }
    }
}
