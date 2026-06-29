using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovolSplitter.WinForms.Models
{   

    public sealed class CovolFilePackage
    {
        public long Id { get; set; }
        public string TipoArchivo { get; set; } = "M";
        public Guid? UuidArchivo { get; set; }
        public string NombreArchivo { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public string? VersionXml { get; set; }
        public string RfcContribuyente { get; set; } = "";
        public string? RfcRepresentanteLegal { get; set; }
        public string? RfcProveedor { get; set; }
        public string ClaveInstalacion { get; set; } = "";
        public string? DescripcionInstalacion { get; set; }
        public int NumeroPozos { get; set; }
        public int NumeroTanques { get; set; }
        public int NumeroDuctosEntradaSalida { get; set; }
        public int NumeroDuctosTransporteDistribucion { get; set; }
        public int NumeroDispensarios { get; set; }
        public DateTimeOffset? FechaReporte { get; set; }
        public DateTime? FechaOperacion { get; set; }
        public short Anio { get; set; }
        public short Mes { get; set; }
        public short? Dia { get; set; }
        public int AnioMes => (Anio * 100) + Mes;
        public int TotalProductos { get; set; }
        public int TotalTransacciones { get; set; }
        public int TotalRecepciones { get; set; }
        public int TotalEntregas { get; set; }
    }

    public sealed class CovolProduct
    {
        public long TempId { get; set; }
        public long Id { get; set; }
        public long ArchivoId { get; set; }
        public string ClaveProducto { get; set; } = "";
        public string? ClaveSubProducto { get; set; }
        public string? MarcaComercial { get; set; }
        public int? Octanaje { get; set; }
        public string? CombustibleNoFosil { get; set; }
        public string? XmlProductoBase { get; set; }
    }

    public sealed class CovolTransaction
    {
        public long ProductTempId { get; set; }
        public long ArchivoId { get; set; }
        public long ProductoId { get; set; }
        public string TipoPaquete { get; set; } = "M";
        public string TipoMovimiento { get; set; } = "";
        public DateTimeOffset FechaTransaccion { get; set; }
        public DateTime FechaOperacion { get; set; }
        public short Anio { get; set; }
        public short Mes { get; set; }
        public short Dia { get; set; }
        public int AnioMes => (Anio * 100) + Mes;
        public string? RfcClienteProveedor { get; set; }
        public string? NombreClienteProveedor { get; set; }
        public string? PermisoProveedor { get; set; }
        public Guid? Cfdi { get; set; }
        public string? CfdiTexto { get; set; }
        public string? TipoCfdi { get; set; }
        public decimal? PrecioCompra { get; set; }
        public decimal? PrecioVentaPublico { get; set; }
        public decimal? PrecioVenta { get; set; }
        public decimal? Volumen { get; set; }
        public string? Um { get; set; }
        public int? NumeroRegistro { get; set; }
        public string? TipoRegistro { get; set; }
        public string? Dispensario { get; set; }
        public string? Manguera { get; set; }
        public string? Tanque { get; set; }
        public decimal? VolumenTotalizadorAcum { get; set; }
        public decimal? VolumenTotalizadorInsta { get; set; }
    }

    public sealed class CovolImportProgress
    {
        public string Stage { get; init; } = "";
        public int Percent { get; init; }
        public int Products { get; init; }
        public int Transactions { get; init; }
        public string? Message { get; init; }
    }
}
