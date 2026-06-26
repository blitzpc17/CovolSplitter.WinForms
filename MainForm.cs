using CovolSplitter.Winforms.Models;
using CovolSplitter.WinForms.Controls;
using CovolSplitter.WinForms.Models;
using CovolSplitter.WinForms.Services;

namespace CovolSplitter.WinForms;

public enum ConsultaGridMode
{
    Ninguno,
    Archivos,
    ResumenMensual,
    DiariosMes,
    DetalleDiario,
    Inventarios
}

public partial class MainForm : Form
{
    private readonly AppLocalConfigService _localConfigService = new();
    private readonly GlobalVariablesRepository _globalVariablesRepository = new();

    private CancellationTokenSource? _cts;
    private string? _connectionString;

    private ConsultaGridMode _consultaGridMode = ConsultaGridMode.Ninguno;
    private DateOnly? _fechaDiarioSeleccionada;

    private bool _cargandoFechaDesdeCodigo;
    private bool _cargandoFiltrosAutomaticos;

    public MainForm()
    {
        InitializeComponent();
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        BloquearSistema();

        txtConnectionString.Text =
            _localConfigService.LoadConnectionString()
            ?? "Host=localhost;Port=5432;Database=covol_db;Username=postgres;Password=TU_PASSWORD;Pooling=true;Minimum Pool Size=1;Maximum Pool Size=20;";

        if (!string.IsNullOrWhiteSpace(txtConnectionString.Text) &&
            !txtConnectionString.Text.Contains("TU_PASSWORD", StringComparison.OrdinalIgnoreCase))
        {
            await ValidarConexionInicialAsync();
        }
        else
        {
            lblConfigEstado.Text = "Ingresa la cadena de conexión y presiona Probar conexión.";
            lblConfigEstado.ForeColor = Color.Firebrick;
            tabPrincipal.SelectedTab = tabConfiguracion;
        }
    }

    private async Task ValidarConexionInicialAsync()
    {
        try
        {
            var cn = txtConnectionString.Text.Trim();

            await _globalVariablesRepository.TestConnectionAsync(cn);
            await _globalVariablesRepository.EnsureVariablesTableAsync(cn);

            var dbConnectionString = await _globalVariablesRepository.GetConnectionStringAsync(cn);

            if (!string.IsNullOrWhiteSpace(dbConnectionString))
            {
                txtConnectionString.Text = dbConnectionString;
                cn = dbConnectionString;
            }

            var covolRepo = new CovolRepository(cn);
            await covolRepo.EnsureProductosXmlColumnAsync();

            _connectionString = cn;

            HabilitarSistema();

            lblConfigEstado.Text = "Conexión válida. Sistema listo.";
            lblConfigEstado.ForeColor = Color.ForestGreen;

            await CargarFiltrosAutomaticosAsync();
        }
        catch
        {
            BloquearSistema();

            lblConfigEstado.Text = "No se pudo validar la conexión. Revisa la configuración.";
            lblConfigEstado.ForeColor = Color.Firebrick;
            tabPrincipal.SelectedTab = tabConfiguracion;
        }
    }

    private void BloquearSistema()
    {
        tabConsulta.Enabled = false;
        tabExportacion.Enabled = false;

        btnImportarMensual.Enabled = false;
        btnConsultarArchivos.Enabled = false;
        btnConsultarResumen.Enabled = false;
        btnConsultarDiariosMes.Enabled = false;
        btnImportarInventarios.Enabled = false;
        btnGenerarXmlDiario.Enabled = false;
    }

    private void HabilitarSistema()
    {
        tabConsulta.Enabled = true;
        tabExportacion.Enabled = true;

        btnImportarMensual.Enabled = true;
        btnConsultarArchivos.Enabled = true;
        btnConsultarResumen.Enabled = true;
        btnConsultarDiariosMes.Enabled = true;
        btnImportarInventarios.Enabled = true;
        btnGenerarXmlDiario.Enabled = true;
    }

    private async void btnProbarConexion_Click(object sender, EventArgs e)
    {
        await ProbarConexionAsync(false);
    }

    private async void btnGuardarConfiguracion_Click(object sender, EventArgs e)
    {
        await ProbarConexionAsync(true);
    }

    private async Task ProbarConexionAsync(bool guardar)
    {
        var cn = txtConnectionString.Text.Trim();

        if (string.IsNullOrWhiteSpace(cn))
        {
            MessageBox.Show(
                this,
                "La cadena de conexión es obligatoria.",
                "Configuración",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        btnProbarConexion.Enabled = false;
        btnGuardarConfiguracion.Enabled = false;
        lblConfigEstado.ForeColor = Color.Black;
        lblConfigEstado.Text = "Probando conexión...";

        try
        {
            await _globalVariablesRepository.TestConnectionAsync(cn);
            await _globalVariablesRepository.EnsureVariablesTableAsync(cn);

            if (guardar)
            {
                await _globalVariablesRepository.SaveConnectionStringAsync(cn);
                _localConfigService.SaveConnectionString(cn);
            }

            _connectionString = cn;

            HabilitarSistema();

            lblConfigEstado.Text = guardar
                ? "Conexión válida y configuración guardada."
                : "Conexión válida.";

            lblConfigEstado.ForeColor = Color.ForestGreen;

            await CargarFiltrosAutomaticosAsync();

            MessageBox.Show(
                this,
                lblConfigEstado.Text,
                "Configuración",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            BloquearSistema();

            lblConfigEstado.Text = "Error de conexión.";
            lblConfigEstado.ForeColor = Color.Firebrick;

            MessageBox.Show(
                this,
                ex.Message,
                "Error de conexión",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnProbarConexion.Enabled = true;
            btnGuardarConfiguracion.Enabled = true;
        }
    }

    private async void numAnio_ValueChanged(object sender, EventArgs e)
    {
        await CargarFiltrosAutomaticosAsync();
    }

    private async void numMes_ValueChanged(object sender, EventArgs e)
    {
        await CargarFiltrosAutomaticosAsync();
    }

    private async Task CargarFiltrosAutomaticosAsync()
    {
        if (_cargandoFiltrosAutomaticos)
            return;

        if (string.IsNullOrWhiteSpace(_connectionString))
            return;

        try
        {
            _cargandoFiltrosAutomaticos = true;

            var anio = Convert.ToInt32(numAnio.Value);
            var mes = Convert.ToInt32(numMes.Value);

            var repo = new CovolRepository(_connectionString);

            ConfigurarFechaXml();

            var tieneMovimientos = await repo.TieneMovimientosPeriodoAsync(anio, mes);

            if (!tieneMovimientos)
            {
                comboProductos.SetItems(Array.Empty<FilterOption>(), "TODOS LOS PRODUCTOS");
                comboMovimientos.SetItems(Array.Empty<FilterOption>(), "TODOS LOS MOVIMIENTOS");
                CargarDiasXml(new List<FilterOption>());

                lblConsultaConteo.Text = $"No hay movimientos cargados para {mes:D2}/{anio}.";
                return;
            }

            var productos = (await repo.GetProductosFiltroAsync(anio, mes)).ToList();
            var movimientos = (await repo.GetTiposMovimientoFiltroAsync(anio, mes)).ToList();
            var dias = (await repo.GetDiasDisponiblesXmlAsync(anio, mes)).ToList();

            comboProductos.SetItems(productos, "TODOS LOS PRODUCTOS");
            comboMovimientos.SetItems(movimientos, "TODOS LOS MOVIMIENTOS");

            CargarDiasXml(dias);

            lblConsultaConteo.Text =
                $"Filtros cargados automáticamente. Productos: {productos.Count:N0} | Movimientos: {movimientos.Count:N0} | Días: {dias.Count:N0}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error al cargar filtros",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            _cargandoFiltrosAutomaticos = false;
        }
    }

    private void CargarDiasXml(List<FilterOption> dias)
    {
        checkedDiasXml.ItemCheck -= checkedDiasXml_ItemCheck;
        checkedDiasXml.Items.Clear();

        checkedDiasXml.Items.Add(new FilterOption
        {
            Value = "TODOS",
            Text = "TODOS LOS DÍAS"
        }, false);

        foreach (var dia in dias)
            checkedDiasXml.Items.Add(dia, false);

        checkedDiasXml.ItemCheck += checkedDiasXml_ItemCheck;
    }

    private void checkedDiasXml_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (checkedDiasXml.Items[e.Index] is not FilterOption option)
            return;

        BeginInvoke(new Action(() =>
        {
            if (option.Value == "TODOS" && e.NewValue == CheckState.Checked)
            {
                for (var i = 0; i < checkedDiasXml.Items.Count; i++)
                {
                    if (i != e.Index)
                        checkedDiasXml.SetItemChecked(i, false);
                }
            }

            if (option.Value != "TODOS" && e.NewValue == CheckState.Checked)
            {
                for (var i = 0; i < checkedDiasXml.Items.Count; i++)
                {
                    if (checkedDiasXml.Items[i] is FilterOption item && item.Value == "TODOS")
                    {
                        checkedDiasXml.SetItemChecked(i, false);
                        break;
                    }
                }
            }
        }));
    }

    private void AgregarDiaXmlSeleccionado(DateOnly fecha)
    {
        var anio = Convert.ToInt32(numAnio.Value);
        var mes = Convert.ToInt32(numMes.Value);

        if (fecha.Year != anio || fecha.Month != mes)
            return;

        var diaValue = fecha.Day.ToString();
        var diaText = $"{fecha.Day:D2}/{mes:D2}/{anio}";

        checkedDiasXml.ItemCheck -= checkedDiasXml_ItemCheck;

        for (var i = 0; i < checkedDiasXml.Items.Count; i++)
        {
            if (checkedDiasXml.Items[i] is not FilterOption item)
                continue;

            if (item.Value == "TODOS")
                checkedDiasXml.SetItemChecked(i, false);

            if (item.Value == diaValue)
            {
                checkedDiasXml.SetItemChecked(i, true);
                checkedDiasXml.ItemCheck += checkedDiasXml_ItemCheck;
                return;
            }
        }

        checkedDiasXml.Items.Add(new FilterOption
        {
            Value = diaValue,
            Text = diaText
        }, true);

        checkedDiasXml.ItemCheck += checkedDiasXml_ItemCheck;
    }

    private int[] ObtenerDiasXmlSeleccionados()
    {
        var dias = new List<int>();

        foreach (var item in checkedDiasXml.CheckedItems)
        {
            if (item is not FilterOption option)
                continue;

            if (option.Value == "TODOS")
                return Array.Empty<int>();

            if (int.TryParse(option.Value, out var dia))
                dias.Add(dia);
        }

        return dias
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
    }

    private void ConfigurarFechaXml()
    {
        var anio = Convert.ToInt32(numAnio.Value);
        var mes = Convert.ToInt32(numMes.Value);

        var primero = new DateTime(anio, mes, 1);
        var ultimo = primero.AddMonths(1).AddDays(-1);

        _cargandoFechaDesdeCodigo = true;

        dtpXmlDia.MinDate = primero;
        dtpXmlDia.MaxDate = ultimo;

        if (dtpXmlDia.Value < primero || dtpXmlDia.Value > ultimo)
            dtpXmlDia.Value = primero;

        _cargandoFechaDesdeCodigo = false;
    }

    private async void btnConsultarArchivos_Click(object sender, EventArgs e)
    {
        if (!TieneConexionActiva())
            return;

        try
        {
            btnConsultarArchivos.Enabled = false;

            int? anio = chkFiltroAnioMes.Checked ? Convert.ToInt32(numAnio.Value) : null;
            int? mes = chkFiltroAnioMes.Checked ? Convert.ToInt32(numMes.Value) : null;

            var repo = new CovolRepository(_connectionString!);
            var rows = (await repo.GetArchivosAsync(anio, mes)).ToList();

            _consultaGridMode = ConsultaGridMode.Archivos;
            _fechaDiarioSeleccionada = null;

            SetGridData(dgvConsulta, rows);
            lblConsultaConteo.Text = $"Registros cargados: {rows.Count:N0}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error al consultar archivos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnConsultarArchivos.Enabled = true;
        }
    }

    private async void btnConsultarResumen_Click(object sender, EventArgs e)
    {
        if (!TieneConexionActiva())
            return;

        try
        {
            btnConsultarResumen.Enabled = false;

            await CargarFiltrosAutomaticosAsync();

            var repo = new CovolRepository(_connectionString!);

            var rows = (await repo.GetMonthlySummaryAsync(
                Convert.ToInt32(numAnio.Value),
                Convert.ToInt32(numMes.Value)
            )).ToList();

            _consultaGridMode = ConsultaGridMode.ResumenMensual;
            _fechaDiarioSeleccionada = null;

            SetGridData(dgvConsulta, rows);
            lblConsultaConteo.Text = $"Registros cargados: {rows.Count:N0}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error al consultar resumen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnConsultarResumen.Enabled = true;
        }
    }

    private async void btnConsultarDiariosMes_Click(object sender, EventArgs e)
    {
        await ConsultarDiariosMesAsync();
    }

    private async Task ConsultarDiariosMesAsync()
    {
        if (!TieneConexionActiva())
            return;

        try
        {
            btnConsultarDiariosMes.Enabled = false;

            await CargarFiltrosAutomaticosAsync();

            var anio = Convert.ToInt32(numAnio.Value);
            var mes = Convert.ToInt32(numMes.Value);

            var productKeys = comboProductos.GetSelectedValues();
            var tiposMovimiento = comboMovimientos.GetSelectedValues();

            var repo = new CovolRepository(_connectionString!);

            var rows = (await repo.GetDiariosPorMesAsync(
                anio,
                mes,
                productKeys,
                tiposMovimiento
            )).ToList();

            _consultaGridMode = ConsultaGridMode.DiariosMes;
            _fechaDiarioSeleccionada = null;

            SetGridData(dgvConsulta, rows);
            lblConsultaConteo.Text = $"Diarios cargados: {rows.Count:N0}";

            ConfigurarFechaXml();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error al consultar diarios",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnConsultarDiariosMes.Enabled = true;
        }
    }

    private async void btnImportarInventarios_Click(object sender, EventArgs e)
    {
        if (!TieneConexionActiva())
            return;

        using var ofd = new OpenFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            Title = "Selecciona archivo de inventarios"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            btnImportarInventarios.Enabled = false;

            var anio = Convert.ToInt32(numAnio.Value);
            var mes = Convert.ToInt32(numMes.Value);

            var importer = new InventariosExcelImporter();
            var inventarios = importer.LeerInventarios(ofd.FileName, anio, mes);

            var repo = new InventariosRepository(_connectionString!);
            await repo.GuardarInventariosAsync(inventarios);

            var rows = (await repo.GetInventariosMesAsync(anio, mes)).ToList();

            _consultaGridMode = ConsultaGridMode.Inventarios;

            SetGridData(dgvConsulta, rows);

            await CargarFiltrosAutomaticosAsync();

            lblConsultaConteo.Text =
                $"Inventarios importados/actualizados: {inventarios.Count:N0} | Registros en BD: {rows.Count:N0}";

            MessageBox.Show(
                this,
                "Inventarios importados correctamente.",
                "Inventarios",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error al importar inventarios",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnImportarInventarios.Enabled = true;
        }
    }

    private async void btnGenerarXmlDiario_Click(object sender, EventArgs e)
    {
        if (!TieneConexionActiva())
            return;

        var anio = Convert.ToInt32(numAnio.Value);
        var mes = Convert.ToInt32(numMes.Value);

        var productKeys = comboProductos.GetSelectedValues();
        var tiposMovimiento = comboMovimientos.GetSelectedValues();

        try
        {
            btnGenerarXmlDiario.Enabled = false;

            var exporter = new CovolDailyXmlExporter(_connectionString!);
            var diasSeleccionados = ObtenerDiasXmlSeleccionados();

            if (chkGenerarTodoMes.Checked || diasSeleccionados.Length > 0)
            {
                using var fbd = new FolderBrowserDialog
                {
                    Description = "Selecciona carpeta para guardar los XML diarios"
                };

                if (fbd.ShowDialog(this) != DialogResult.OK)
                    return;

                int total;

                if (chkGenerarTodoMes.Checked)
                {
                    var progress = new Progress<string>(msg =>
                    {
                        lblConsultaConteo.Text = msg;
                    });

                    total = await exporter.ExportMonthXmlAsync(
                        anio,
                        mes,
                        productKeys,
                        tiposMovimiento,
                        fbd.SelectedPath,
                        progress
                    );
                }
                else
                {
                    total = 0;

                    foreach (var dia in diasSeleccionados)
                    {
                        var fecha = new DateOnly(anio, mes, dia);
                        var fileName = $"D_{fecha:yyyy-MM-dd}_COVOL_DIARIO.xml";
                        var path = Path.Combine(fbd.SelectedPath, fileName);

                        lblConsultaConteo.Text = $"Generando {fileName}...";

                        await exporter.ExportDailyXmlAsync(
                            anio,
                            mes,
                            fecha,
                            productKeys,
                            tiposMovimiento,
                            path
                        );

                        total++;
                    }
                }

                MessageBox.Show(
                    this,
                    $"Se generaron {total:N0} archivos XML diarios.",
                    "XML diario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return;
            }

            var fechaUnica = DateOnly.FromDateTime(dtpXmlDia.Value);

            using var sfd = new SaveFileDialog
            {
                Filter = "XML (*.xml)|*.xml",
                Title = "Guardar XML diario",
                FileName = $"D_{fechaUnica:yyyy-MM-dd}_COVOL_DIARIO.xml"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            await exporter.ExportDailyXmlAsync(
                anio,
                mes,
                fechaUnica,
                productKeys,
                tiposMovimiento,
                sfd.FileName
            );

            MessageBox.Show(
                this,
                "XML diario generado correctamente.",
                "XML diario",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error al generar XML diario",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnGenerarXmlDiario.Enabled = true;
        }
    }

    private async void dgvConsulta_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (!TieneConexionActiva())
            return;

        if (e.RowIndex < 0)
            return;

        if (_consultaGridMode != ConsultaGridMode.DiariosMes)
            return;

        if (!dgvConsulta.Columns.Contains("fecha_operacion"))
            return;

        var row = dgvConsulta.Rows[e.RowIndex];
        var rawFecha = row.Cells["fecha_operacion"].Value;

        if (rawFecha is null || rawFecha == DBNull.Value)
        {
            MessageBox.Show(
                this,
                "El registro seleccionado no tiene fecha de operación.",
                "Consulta",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        var fecha = DateOnly.FromDateTime(Convert.ToDateTime(rawFecha));

        await CargarDetalleDiarioAsync(fecha);
    }

    private async void dtpXmlDia_ValueChanged(object sender, EventArgs e)
    {
        if (_cargandoFechaDesdeCodigo)
            return;

        var fecha = DateOnly.FromDateTime(dtpXmlDia.Value);

        AgregarDiaXmlSeleccionado(fecha);

        if (_consultaGridMode == ConsultaGridMode.DiariosMes ||
            _consultaGridMode == ConsultaGridMode.DetalleDiario)
        {
            await CargarDetalleDiarioAsync(fecha);
        }
    }

    private async Task CargarDetalleDiarioAsync(DateOnly fecha)
    {
        if (!TieneConexionActiva())
            return;

        try
        {
            var anio = Convert.ToInt32(numAnio.Value);
            var mes = Convert.ToInt32(numMes.Value);

            var productKeys = comboProductos.GetSelectedValues();
            var tiposMovimiento = comboMovimientos.GetSelectedValues();

            var repo = new CovolRepository(_connectionString!);

            var detalle = (await repo.GetDetalleDiarioPorFiltrosAsync(
                anio,
                mes,
                fecha,
                productKeys,
                tiposMovimiento
            )).ToList();

            _consultaGridMode = ConsultaGridMode.DetalleDiario;
            _fechaDiarioSeleccionada = fecha;

            _cargandoFechaDesdeCodigo = true;
            dtpXmlDia.Value = fecha.ToDateTime(TimeOnly.MinValue);
            _cargandoFechaDesdeCodigo = false;

            SetGridData(dgvConsulta, detalle);
            lblConsultaConteo.Text = $"Detalle diario {fecha:dd/MM/yyyy} | Registros: {detalle.Count:N0}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error al consultar detalle diario",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private async void btnImportarMensual_Click(object sender, EventArgs e)
    {
        if (!TieneConexionActiva())
            return;

        using var ofd = new OpenFileDialog
        {
            Filter = "XML Covol (*.xml)|*.xml",
            Title = "Selecciona XML mensual o diario"
        };

        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        btnImportarMensual.Enabled = false;
        btnCancelar.Enabled = true;

        _cts = new CancellationTokenSource();

        progressBar1.Value = 0;
        lblEstado.Text = "Iniciando importación...";
        lblDetalle.Text = Path.GetFileName(ofd.FileName);

        var progress = new Progress<CovolImportProgress>(p =>
        {
            progressBar1.Value = Math.Max(0, Math.Min(100, p.Percent));
            lblEstado.Text = $"{p.Stage} {p.Percent}% - Productos: {p.Products:N0} - Movs: {p.Transactions:N0}";

            if (!string.IsNullOrWhiteSpace(p.Message))
                lblDetalle.Text = p.Message;
        });

                try
        {
            var sha256 = await CovolXmlParser.Sha256Async(ofd.FileName, _cts.Token);
            var repo = new CovolRepository(_connectionString!);
            var existingId = await repo.GetArchivoIdBySha256Async(sha256, _cts.Token);

            if (existingId.HasValue)
            {
                var promptResult = MessageBox.Show(
                    this,
                    "Este archivo XML ya ha sido importado previamente.\n¿Deseas reemplazarlo con la nueva importación? La información anterior será eliminada.",
                    "Archivo existente",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (promptResult == DialogResult.No)
                {
                    btnImportarMensual.Enabled = true;
                    btnCancelar.Enabled = false;
                    _cts?.Dispose();
                    _cts = null;
                    lblEstado.Text = "Importación cancelada por el usuario.";
                    return;
                }

                lblEstado.Text = "Eliminando importación anterior...";
                await repo.DeleteArchivoAsync(existingId.Value, _cts.Token);
                lblEstado.Text = "Iniciando importación...";
            }

            var service = new CovolImportService(
                new CovolXmlParser(),
                repo
            );

            var archivoId = await Task.Run(() =>
                service.ImportAsync(ofd.FileName, progress, _cts.Token)
            );

            MessageBox.Show(
                this,
                $"Importación finalizada. Archivo ID: {archivoId}",
                "OK",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            await CargarDiariosDerivadosAsync(archivoId);
            await CargarFiltrosAutomaticosAsync();
        }
                catch (OperationCanceledException)
        {
            MessageBox.Show(
                this,
                "Importación cancelada.",
                "Cancelado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
        catch (Npgsql.PostgresException pex) when (pex.SqlState == "23505")
        {
            MessageBox.Show(
                this,
                "Este archivo XML ya ha sido importado previamente.\n\nPara exportar sus diarios, seleccione el mes correspondiente en los filtros y presione 'Generar XML Diario'.",
                "Archivo ya importado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            btnImportarMensual.Enabled = true;
            btnCancelar.Enabled = false;

            _cts?.Dispose();
            _cts = null;
        }
    }

    private void btnCancelar_Click(object sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    private async Task CargarDiariosDerivadosAsync(long archivoMensualId)
    {
        if (!TieneConexionActiva())
            return;

        var repo = new CovolRepository(_connectionString!);
        var rows = (await repo.GetDerivedDailyFilesAsync(archivoMensualId)).ToList();

        SetGridData(dgvDiarios, rows);
    }

    private async void dgvDiarios_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (!TieneConexionActiva())
            return;

        if (e.RowIndex < 0)
            return;

        var row = dgvDiarios.Rows[e.RowIndex];

        if (row.Cells["archivo_mensual_id"].Value is null ||
            row.Cells["archivo_mensual_id"].Value == DBNull.Value ||
            row.Cells["fecha_operacion"].Value is null ||
            row.Cells["fecha_operacion"].Value == DBNull.Value)
            return;

        var archivoId = Convert.ToInt64(row.Cells["archivo_mensual_id"].Value);
        var fechaValue = row.Cells["fecha_operacion"].Value;
        var fecha = fechaValue switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => DateOnly.FromDateTime(Convert.ToDateTime(fechaValue))
        };
        var repo = new CovolRepository(_connectionString!);
        var txs = (await repo.GetDailyTransactionsAsync(archivoId, fecha)).ToList();

        SetGridData(dgvDetalleDiario, txs);
    }

    private void SetGridData(DataGridView grid, object data)
    {
        grid.DataSource = null;
        grid.Columns.Clear();
        grid.AutoGenerateColumns = true;
        grid.DataSource = data;

        AplicarFormatoGrid(grid);

        int count = 0;
        if (data is System.Collections.ICollection collection)
            count = collection.Count;
        else if (data != null)
            count = grid.Rows.Count;

        if (grid == dgvConsulta && lblConsultaConteo != null)
            lblConsultaConteo.Text = $"Registros encontrados: {count:N0}";
        else if (grid == dgvDiarios && lblDiarios != null)
            lblDiarios.Text = $"Diarios Generados: {count:N0}";
        else if (grid == dgvDetalleDiario && lblDetalleDiario != null)
            lblDetalleDiario.Text = $"Detalle (Transacciones): {count:N0}";
    }

    private void AplicarFormatoGrid(DataGridView grid)
    {
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.RowHeadersWidth = 28;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);

        foreach (DataGridViewColumn col in grid.Columns)
        {
            col.Frozen = false;
            col.MinimumWidth = 80;

            if (col.Name.Contains("id", StringComparison.OrdinalIgnoreCase))
                col.Width = 70;
            else if (col.Name.Contains("fecha", StringComparison.OrdinalIgnoreCase))
                col.Width = 135;
            else if (col.Name.Contains("nombre", StringComparison.OrdinalIgnoreCase))
                col.Width = 260;
            else if (col.Name.Contains("marca", StringComparison.OrdinalIgnoreCase))
                col.Width = 170;
            else if (col.Name.Contains("cfdi", StringComparison.OrdinalIgnoreCase))
                col.Width = 250;
            else if (col.Name.Contains("volumen", StringComparison.OrdinalIgnoreCase))
                col.Width = 130;
            else if (col.Name.Contains("importe", StringComparison.OrdinalIgnoreCase))
                col.Width = 150;
            else
                col.Width = Math.Max(col.Width, 110);
        }

        FrozenColumnIfExists(grid, "fecha_operacion");
    }

    private void FrozenColumnIfExists(DataGridView grid, string columnName)
    {
        if (!grid.Columns.Contains(columnName))
            return;

        grid.Columns[columnName].Frozen = true;
    }

    private bool TieneConexionActiva()
    {
        if (!string.IsNullOrWhiteSpace(_connectionString))
            return true;

        MessageBox.Show(
            this,
            "Primero debes configurar y validar la conexión a PostgreSQL.",
            "Configuración requerida",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
        );

        tabPrincipal.SelectedTab = tabConfiguracion;
        return false;
    }
}