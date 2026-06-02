namespace CovolSplitter.WinForms.Models;

public sealed class InventarioDiario
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int Dia { get; set; }
    public DateTime FechaOperacion { get; set; }
    public string ProductoLike { get; set; } = "";
    public decimal VolumenExistencias { get; set; }
    public decimal? VolumenExistenciasAnterior { get; set; }
    public string? ArchivoOrigen { get; set; }
}