using CovolSplitter.WinForms.Models;
using Dapper;
using Npgsql;

namespace CovolSplitter.WinForms.Services;

public sealed class EmpresasRepository
{
    private readonly string _connectionString;

    public EmpresasRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitTablesAsync()
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync();

        await cn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS covol.empresas (
                id SERIAL PRIMARY KEY,
                nombre VARCHAR(255) NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS covol.empresa_tanques (
                id SERIAL PRIMARY KEY,
                empresa_id INT REFERENCES covol.empresas(id) ON DELETE CASCADE,
                producto VARCHAR(50) NOT NULL,
                xml_tanque TEXT NOT NULL,
                UNIQUE (empresa_id, producto)
            );
        ");
    }

    public async Task<List<Empresa>> GetAllEmpresasAsync()
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return (await cn.QueryAsync<Empresa>("SELECT id AS Id, nombre AS Nombre FROM covol.empresas ORDER BY nombre")).ToList();
    }

    public async Task<int> CreateEmpresaAsync(string nombre)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return await cn.ExecuteScalarAsync<int>(
            "INSERT INTO covol.empresas (nombre) VALUES (@nombre) RETURNING id;", 
            new { nombre });
    }

    public async Task UpdateEmpresaAsync(int id, string nombre)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync(
            "UPDATE covol.empresas SET nombre = @nombre WHERE id = @id;", 
            new { id, nombre });
    }

    public async Task DeleteEmpresaAsync(int id)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.ExecuteAsync("DELETE FROM covol.empresas WHERE id = @id;", new { id });
    }

    public async Task<List<EmpresaTanque>> GetTanquesByEmpresaAsync(int empresaId)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        return (await cn.QueryAsync<EmpresaTanque>(
            "SELECT id AS Id, empresa_id AS EmpresaId, producto AS Producto, xml_tanque AS XmlTanque FROM covol.empresa_tanques WHERE empresa_id = @empresaId", 
            new { empresaId })).ToList();
    }

    public async Task SaveTanqueAsync(int empresaId, string producto, string xmlTanque)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        
        // Ensure not saving empty strings as empty records, only if there is content or to clear it
        if (string.IsNullOrWhiteSpace(xmlTanque))
        {
            await cn.ExecuteAsync(@"
                DELETE FROM covol.empresa_tanques WHERE empresa_id = @empresaId AND producto = @producto;
            ", new { empresaId, producto });
        }
        else
        {
            await cn.ExecuteAsync(@"
                INSERT INTO covol.empresa_tanques (empresa_id, producto, xml_tanque)
                VALUES (@empresaId, @producto, @xmlTanque)
                ON CONFLICT (empresa_id, producto) 
                DO UPDATE SET xml_tanque = EXCLUDED.xml_tanque;
            ", new { empresaId, producto, xmlTanque });
        }
    }

    public async Task<int> ActualizarFechasCalibracionMasivaAsync(DateOnly nuevaFecha)
    {
        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync();

        var tanques = await cn.QueryAsync<dynamic>(@"SELECT id, xml_tanque FROM covol.empresa_tanques WHERE xml_tanque IS NOT NULL AND xml_tanque != ''");
        int updatedCount = 0;
        System.Xml.Linq.XNamespace covol = "https://www.sat.gob.mx/esquemas/ControlesVolumetricos";
        System.Xml.Linq.XNamespace covolAlterno = "https://www.sat.gob.mx/ControlesVolumetricos";

        foreach (var tanque in tanques)
        {
            string xmlBase = tanque.xml_tanque;
            bool changed = false;

            try
            {
                string xmlString = xmlBase.Trim();
                if (xmlString.StartsWith("<Covol:TANQUE") && !xmlString.Contains("</Covol:TANQUE>"))
                    xmlString += "\n</Covol:TANQUE>";
                else if (xmlString.StartsWith("<TANQUE") && !xmlString.Contains("</TANQUE>"))
                    xmlString += "\n</TANQUE>";

                string xmlToParse = $"<root xmlns:Covol=\"{covolAlterno}\">{xmlString}</root>";
                var doc = System.Xml.Linq.XDocument.Parse(xmlToParse);
                
                // The actual TANQUE element is the first child of root
                var nodoRaiz = doc.Elements().FirstOrDefault();
                if (nodoRaiz == null) continue;
                
                var tanqueElement = nodoRaiz.Elements().FirstOrDefault();
                if (tanqueElement == null) continue;

                var nodoVigenciaTanque = tanqueElement.Descendants(covol + "VigenciaCalibracionTanque").FirstOrDefault()
                                      ?? tanqueElement.Descendants(covolAlterno + "VigenciaCalibracionTanque").FirstOrDefault();
                if (nodoVigenciaTanque != null)
                {
                    nodoVigenciaTanque.Value = nuevaFecha.ToString("yyyy-MM-dd");
                    changed = true;
                }

                var nodoVigenciaSist = tanqueElement.Descendants(covol + "VigenciaCalibracionSistMedicionTanque").FirstOrDefault()
                                    ?? tanqueElement.Descendants(covolAlterno + "VigenciaCalibracionSistMedicionTanque").FirstOrDefault();
                if (nodoVigenciaSist != null)
                {
                    nodoVigenciaSist.Value = nuevaFecha.ToString("yyyy-MM-dd");
                    changed = true;
                }

                if (changed)
                {
                    string newXml = tanqueElement.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
                    await cn.ExecuteAsync(@"
                        UPDATE covol.empresa_tanques 
                        SET xml_tanque = @xml 
                        WHERE id = @id;",
                        new { xml = newXml, id = tanque.id }
                    );
                    updatedCount++;
                }
            }
            catch
            {
                // Ignorar XML mal formado
            }
        }
        return updatedCount;
    }
}
