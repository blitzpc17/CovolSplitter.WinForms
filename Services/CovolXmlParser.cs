using System.Globalization;
using System.Security.Cryptography;
using System.Xml.Linq;
using CovolSplitter.WinForms.Models;

namespace CovolSplitter.WinForms.Services
{
    public sealed class CovolXmlParser
    {
        private static readonly XNamespace Exp = "Complemento_Expendio";

        public async Task<(CovolFilePackage Package, List<CovolProduct> Products, List<CovolTransaction> Transactions)>
            ParseAsync(string filePath, IProgress<CovolImportProgress>? progress, CancellationToken ct)
        {
            progress?.Report(new CovolImportProgress
            {
                Stage = "Leyendo XML",
                Percent = 5,
                Message = Path.GetFileName(filePath)
            });

            await using var fs = File.OpenRead(filePath);
            var doc = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

            var root = doc.Root ?? throw new InvalidOperationException("XML sin nodo raíz.");
            var cv = root.Name.Namespace;

            bool isMonthly = root.Name.NamespaceName.Contains("/Mensuales", StringComparison.OrdinalIgnoreCase)
                             || Path.GetFileName(filePath).StartsWith("M_", StringComparison.OrdinalIgnoreCase);

            var fechaReporte = ParseDate(V(root, isMonthly ? cv + "FechaYHoraReporteMes" : cv + "FechaYHoraCorte"));

            var package = new CovolFilePackage
            {
                TipoArchivo = isMonthly ? "M" : "D",
                UuidArchivo = TryUuidFromFileName(filePath),
                NombreArchivo = Path.GetFileName(filePath),
                Sha256 = await Sha256Async(filePath, ct),
                VersionXml = V(root, cv + "Version"),
                RfcContribuyente = V(root, cv + "RfcContribuyente") ?? "",
                RfcRepresentanteLegal = V(root, cv + "RfcRepresentanteLegal"),
                RfcProveedor = V(root, cv + "RfcProveedor"),
                ClaveInstalacion = V(root, cv + "ClaveInstalacion") ?? "",
                DescripcionInstalacion = V(root, cv + "DescripcionInstalacion"),

                // PostgreSQL TIMESTAMPTZ con Npgsql requiere UTC.
                FechaReporte = fechaReporte?.ToUniversalTime()
            };

            var dateFromName = TryDateFromFileName(filePath);
            var periodDate = dateFromName ?? fechaReporte?.Date;

            if (periodDate is null)
                throw new InvalidOperationException("No fue posible detectar año/mes/día del archivo.");

            package.Anio = (short)periodDate.Value.Year;
            package.Mes = (short)periodDate.Value.Month;
            package.Dia = isMonthly ? null : (short)periodDate.Value.Day;
            package.FechaOperacion = isMonthly ? null : periodDate.Value.Date;

            var products = new List<CovolProduct>();
            var txs = new List<CovolTransaction>();
            long tempProductId = 0;

            foreach (var xp in root.Elements(cv + "PRODUCTO"))
            {
                ct.ThrowIfCancellationRequested();

                var product = new CovolProduct
                {
                    TempId = ++tempProductId,
                    ClaveProducto = V(xp, cv + "ClaveProducto") ?? "",
                    ClaveSubProducto = V(xp, cv + "ClaveSubProducto"),
                    MarcaComercial = V(xp, cv + "MarcaComercial"),
                    Octanaje = ParseInt(V(xp.Element(cv + "Gasolina"), cv + "ComposOctanajeGasolina")),
                    CombustibleNoFosil = V(xp.Element(cv + "Gasolina"), cv + "GasolinaConCombustibleNoFosil")
                };

                products.Add(product);

                if (isMonthly)
                    ParseMonthlyProduct(cv, xp, product, txs);
                else
                    ParseDailyProduct(cv, xp, product, txs);

                progress?.Report(new CovolImportProgress
                {
                    Stage = "Parseando",
                    Percent = Math.Min(80, 10 + products.Count * 15),
                    Products = products.Count,
                    Transactions = txs.Count,
                    Message = product.MarcaComercial
                });
            }

            package.TotalProductos = products.Count;
            package.TotalTransacciones = txs.Count;
            package.TotalRecepciones = txs.Count(x => x.TipoMovimiento == "RECEPCION");
            package.TotalEntregas = txs.Count(x => x.TipoMovimiento == "ENTREGA");

            return (package, products, txs);
        }

        private static void ParseMonthlyProduct(
            XNamespace cv,
            XElement xp,
            CovolProduct product,
            List<CovolTransaction> txs)
        {
            var rep = xp.Element(cv + "REPORTEDEVOLUMENMENSUAL");
            if (rep is null)
                return;

            ParseMonthlySection(rep.Element(cv + "RECEPCIONES"), product, "RECEPCION", txs);
            ParseMonthlySection(rep.Element(cv + "ENTREGAS"), product, "ENTREGA", txs);
        }

        private static void ParseMonthlySection(
            XElement? section,
            CovolProduct product,
            string movimiento,
            List<CovolTransaction> txs)
        {
            if (section is null)
                return;

            foreach (var nacional in section.Descendants(Exp + "NACIONAL"))
            {
                var tx = ParseNacional(
                    product.TempId,
                    movimiento,
                    "M",
                    nacional,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                );

                if (tx is not null)
                    txs.Add(tx);
            }
        }

        private static void ParseDailyProduct(
            XNamespace cv,
            XElement xp,
            CovolProduct product,
            List<CovolTransaction> txs)
        {
            foreach (var tanque in xp.Elements(cv + "TANQUE"))
            {
                var claveTanque = V(tanque, cv + "ClaveTanque");

                foreach (var recepcion in tanque.Descendants(cv + "RECEPCION"))
                {
                    var nacional = recepcion.Descendants(Exp + "NACIONAL").FirstOrDefault();

                    var tx = ParseNacional(
                        product.TempId,
                        "RECEPCION",
                        "D",
                        nacional,
                        null,
                        null,
                        claveTanque,
                        ParseInt(V(recepcion, cv + "NumeroDeRegistro")),
                        V(recepcion, cv + "TipoDeRegistro"),
                        ParseDecimal(V(recepcion.Element(cv + "VolumenInicialTanque"), cv + "ValorNumerico")),
                        null
                    );

                    if (tx is not null)
                        txs.Add(tx);
                }
            }

            foreach (var dispensario in xp.Elements(cv + "DISPENSARIO"))
            {
                var claveDisp = V(dispensario, cv + "ClaveDispensario");

                foreach (var manguera in dispensario.Elements(cv + "MANGUERA"))
                {
                    var identificadorManguera = V(manguera.Element(cv + "IdentificadorManguera"));

                    foreach (var entrega in manguera.Descendants(cv + "ENTREGA"))
                    {
                        var nacional = entrega.Descendants(Exp + "NACIONAL").FirstOrDefault();

                        var tx = ParseNacional(
                            product.TempId,
                            "ENTREGA",
                            "D",
                            nacional,
                            claveDisp,
                            identificadorManguera,
                            null,
                            ParseInt(V(entrega, cv + "NumeroDeRegistro")),
                            V(entrega, cv + "TipoDeRegistro"),
                            ParseDecimal(V(entrega.Element(cv + "VolumenEntregadoTotalizadorAcum"), cv + "ValorNumerico")),
                            ParseDecimal(V(entrega.Element(cv + "VolumenEntregadoTotalizadorInsta"), cv + "ValorNumerico"))
                        );

                        if (tx is not null)
                            txs.Add(tx);
                    }
                }
            }
        }

        private static CovolTransaction? ParseNacional(
            long productTempId,
            string movimiento,
            string paquete,
            XElement? nacional,
            string? dispensario,
            string? manguera,
            string? tanque,
            int? numeroRegistro,
            string? tipoRegistro,
            decimal? totalizadorAcum,
            decimal? totalizadorInsta)
        {
            if (nacional is null)
                return null;

            var cfdi = nacional.Element(Exp + "CFDIs");
            var fecha = ParseDate(V(cfdi, Exp + "FechaYHoraTransaccion"));

            if (fecha is null)
                return null;

            var fechaLocal = fecha.Value;
            var fechaUtc = fechaLocal.ToUniversalTime();

            var vol = cfdi?.Element(Exp + "VolumenDocumentado");

            var dto = new CovolTransaction
            {
                ProductTempId = productTempId,
                TipoPaquete = paquete,
                TipoMovimiento = movimiento,

                // TIMESTAMPTZ: se guarda en UTC.
                FechaTransaccion = fechaUtc,

                // DATE / anio / mes / dia: se conserva la fecha local del XML.
                FechaOperacion = fechaLocal.Date,
                Anio = (short)fechaLocal.Year,
                Mes = (short)fechaLocal.Month,
                Dia = (short)fechaLocal.Day,

                RfcClienteProveedor = V(nacional, Exp + "RfcClienteOProveedor"),
                NombreClienteProveedor = V(nacional, Exp + "NombreClienteOProveedor"),
                PermisoProveedor = V(nacional, Exp + "PermisoProveedor"),
                CfdiTexto = V(cfdi, Exp + "CFDI"),
                TipoCfdi = V(cfdi, Exp + "TipoCFDI"),
                PrecioCompra = ParseDecimal(V(cfdi, Exp + "PrecioCompra")),
                PrecioVentaPublico = ParseDecimal(V(cfdi, Exp + "PrecioDeVentaAlPublico")),
                PrecioVenta = ParseDecimal(V(cfdi, Exp + "PrecioVenta")),
                Volumen = ParseDecimal(V(vol, Exp + "ValorNumerico")),
                Um = V(vol, Exp + "UM"),
                NumeroRegistro = numeroRegistro,
                TipoRegistro = tipoRegistro,
                Dispensario = dispensario,
                Manguera = manguera,
                Tanque = tanque,
                VolumenTotalizadorAcum = totalizadorAcum,
                VolumenTotalizadorInsta = totalizadorInsta
            };

            if (Guid.TryParse(dto.CfdiTexto, out var g))
                dto.Cfdi = g;

            return dto;
        }

        private static string? V(XElement? parent, XName? childName = null)
        {
            if (parent is null)
                return null;

            var e = childName is null ? parent : parent.Element(childName);
            var s = e?.Value?.Trim();

            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private static DateTimeOffset? ParseDate(string? value)
        {
            return DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var d
            )
                ? d
                : null;
        }

        private static decimal? ParseDecimal(string? value)
        {
            return decimal.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var d
            )
                ? d
                : null;
        }

        private static int? ParseInt(string? value)
        {
            return int.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var i
            )
                ? i
                : null;
        }

        private static DateTime? TryDateFromFileName(string filePath)
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                Path.GetFileName(filePath),
                @"_(\d{4}-\d{2}-\d{2})_"
            );

            return m.Success &&
                   DateTime.TryParseExact(
                       m.Groups[1].Value,
                       "yyyy-MM-dd",
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.None,
                       out var d
                   )
                ? d
                : null;
        }

        private static Guid? TryUuidFromFileName(string filePath)
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                Path.GetFileName(filePath),
                @"^[MD]_([0-9a-fA-F-]{36})_"
            );

            return m.Success && Guid.TryParse(m.Groups[1].Value, out var g)
                ? g
                : null;
        }

        private static async Task<string> Sha256Async(string filePath, CancellationToken ct)
        {
            await using var fs = File.OpenRead(filePath);
            var bytes = await SHA256.HashDataAsync(fs, ct);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}