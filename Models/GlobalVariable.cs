namespace CovolSplitter.WinForms.Models;

public sealed class GlobalVariable
{
    public long Id { get; set; }
    public string Clave { get; set; } = "";
    public string? Valor { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}