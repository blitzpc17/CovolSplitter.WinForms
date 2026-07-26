namespace CovolSplitter.WinForms.Models;

public class EmpresaTanque
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Producto { get; set; } = string.Empty; // Ej. MAGNA, PREMIUM, DIESEL
    public string XmlTanque { get; set; } = string.Empty;
}
