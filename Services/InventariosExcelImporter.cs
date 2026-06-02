using ClosedXML.Excel;
using CovolSplitter.WinForms.Models;

namespace CovolSplitter.WinForms.Services;

public sealed class InventariosExcelImporter
{
    public List<InventarioDiario> LeerInventarios(
        string filePath,
        int anio,
        int mes)
    {
        var result = new List<InventarioDiario>();

        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();

        var header = DetectarEncabezados(ws);

        if (header.FechaCol <= 0)
            throw new InvalidOperationException("No se encontró la columna FECHA en el archivo de inventarios.");

        var productos = new Dictionary<string, int>();

        if (header.MagnaCol > 0)
            productos["MAGNA"] = header.MagnaCol;

        if (header.PremiumCol > 0)
            productos["PREMIUM"] = header.PremiumCol;

        if (header.DieselCol > 0)
            productos["DIESEL"] = header.DieselCol;

        if (productos.Count == 0)
            throw new InvalidOperationException("No se encontraron columnas de productos: MAGNA, PREMIUM o DIESEL.");

        var firstDataRow = header.HeaderRow + 1;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? firstDataRow;

        for (var row = firstDataRow; row <= lastRow; row++)
        {
            var fecha = ObtenerFechaDesdeCelda(ws.Cell(row, header.FechaCol), anio, mes, row - firstDataRow + 1);

            if (fecha is null)
                continue;

            // Validación fuerte: solo importar si coincide con el periodo seleccionado.
            if (fecha.Value.Year != anio || fecha.Value.Month != mes)
                continue;

            var dia = fecha.Value.Day;

            foreach (var producto in productos)
            {
                var value = ObtenerDecimal(ws.Cell(row, producto.Value));

                if (value is null)
                    continue;

                decimal? anterior = null;

                var prevRow = BuscarFilaAnteriorMismoMes(ws, header.FechaCol, producto.Value, row, anio, mes);

                if (prevRow > 0)
                {
                    var prevValue = ObtenerDecimal(ws.Cell(prevRow, producto.Value));

                    if (prevValue is not null)
                        anterior = prevValue.Value;
                }

                result.Add(new InventarioDiario
                {
                    Anio = anio,
                    Mes = mes,
                    Dia = dia,
                    FechaOperacion = fecha.Value.Date,
                    ProductoLike = producto.Key,
                    VolumenExistencias = value.Value,
                    VolumenExistenciasAnterior = anterior,
                    ArchivoOrigen = Path.GetFileName(filePath)
                });
            }
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException(
                $"No se encontraron inventarios válidos para el periodo seleccionado {mes:D2}/{anio}. " +
                "Verifica que el Excel corresponda al mismo mes y año."
            );
        }

        return result;
    }

    private static int BuscarFilaAnteriorMismoMes(
        IXLWorksheet ws,
        int fechaCol,
        int productoCol,
        int currentRow,
        int anio,
        int mes)
    {
        for (var row = currentRow - 1; row >= 1; row--)
        {
            var fecha = ObtenerFechaDesdeCelda(ws.Cell(row, fechaCol), anio, mes, 0);

            if (fecha is null)
                continue;

            if (fecha.Value.Year == anio && fecha.Value.Month == mes)
                return row;
        }

        return 0;
    }

    private static (int HeaderRow, int FechaCol, int MagnaCol, int PremiumCol, int DieselCol) DetectarEncabezados(IXLWorksheet ws)
    {
        var maxRow = Math.Min(20, ws.LastRowUsed()?.RowNumber() ?? 20);
        var maxCol = Math.Min(30, ws.LastColumnUsed()?.ColumnNumber() ?? 30);

        for (var row = 1; row <= maxRow; row++)
        {
            var fechaCol = 0;
            var magnaCol = 0;
            var premiumCol = 0;
            var dieselCol = 0;

            for (var col = 1; col <= maxCol; col++)
            {
                var text = ws.Cell(row, col).GetString().Trim().ToUpperInvariant();

                if (text.Contains("FECHA"))
                    fechaCol = col;

                if (text.Contains("MAGNA"))
                    magnaCol = col;

                if (text.Contains("PREMIUM"))
                    premiumCol = col;

                if (text.Contains("DIESEL"))
                    dieselCol = col;
            }

            if (fechaCol > 0 && (magnaCol > 0 || premiumCol > 0 || dieselCol > 0))
                return (row, fechaCol, magnaCol, premiumCol, dieselCol);
        }

        return (0, 0, 0, 0, 0);
    }

    private static DateTime? ObtenerFechaDesdeCelda(IXLCell cell, int anioModulo, int mesModulo, int fallbackDia)
    {
        try
        {
            if (cell.IsEmpty())
                return null;

            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime().Date;

            if (cell.TryGetValue<DateTime>(out var fecha))
                return fecha.Date;

            var text = cell.GetString().Trim();

            if (DateTime.TryParse(text, out fecha))
                return fecha.Date;

            if (double.TryParse(text, out var serial) && serial > 30000)
                return DateTime.FromOADate(serial).Date;

            // Si viene solo día, lo amarramos al periodo seleccionado.
            if (int.TryParse(text, out var diaDirecto) && diaDirecto >= 1 && diaDirecto <= 31)
            {
                var ultimoDia = DateTime.DaysInMonth(anioModulo, mesModulo);

                if (diaDirecto <= ultimoDia)
                    return new DateTime(anioModulo, mesModulo, diaDirecto);
            }

            // Fallback por posición de fila solo si es válido.
            if (fallbackDia >= 1 && fallbackDia <= DateTime.DaysInMonth(anioModulo, mesModulo))
                return new DateTime(anioModulo, mesModulo, fallbackDia);
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static decimal? ObtenerDecimal(IXLCell cell)
    {
        try
        {
            if (cell.IsEmpty())
                return null;

            if (cell.DataType == XLDataType.Number)
                return cell.GetValue<decimal>();

            var text = cell.GetString()
                .Trim()
                .Replace(",", "");

            if (decimal.TryParse(
                    text,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var value))
                return value;

            if (decimal.TryParse(text, out value))
                return value;
        }
        catch
        {
            return null;
        }

        return null;
    }
}