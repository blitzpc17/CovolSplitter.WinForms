using CovolSplitter.WinForms.Controls;

namespace CovolSplitter.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private TabControl tabPrincipal;
    private TabPage tabConfiguracion;
    private TabPage tabConsulta;
    private TabPage tabExportacion;

    private Label lblConnectionString;
    private TextBox txtConnectionString;
    private Button btnProbarConexion;
    private Button btnGuardarConfiguracion;
    private Label lblConfigEstado;

    private GroupBox grpFiltrosConsulta;
    private CheckBox chkFiltroAnioMes;
    private Label lblAnio;
    private NumericUpDown numAnio;
    private Label lblMes;
    private NumericUpDown numMes;
    private Button btnConsultarArchivos;
    private Button btnConsultarResumen;
    private Button btnConsultarDiariosMes;
    private Button btnImportarInventarios;
    private Button btnGenerarXmlDiario;
    private Label lblProductosConsulta;
    private CheckedComboBox comboProductos;
    private Label lblMovimientosConsulta;
    private CheckedComboBox comboMovimientos;
    private Label lblDiaXml;
    private DateTimePicker dtpXmlDia;
    private CheckBox chkGenerarTodoMes;
    private Label lblDiasXml;
    private CheckedListBox checkedDiasXml;
    private Label lblConsultaConteo;
    private DataGridView dgvConsulta;

    private GroupBox grpImportacion;
    private Button btnImportarMensual;
    private Button btnCancelar;
    private ProgressBar progressBar1;
    private Label lblEstado;
    private Label lblDetalle;
    private SplitContainer splitExportacion;
    private DataGridView dgvDiarios;
    private DataGridView dgvDetalleDiario;
    private Label lblDiarios;
    private Label lblDetalleDiario;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        tabPrincipal = new TabControl();
        tabConfiguracion = new TabPage();
        lblConnectionString = new Label();
        txtConnectionString = new TextBox();
        btnProbarConexion = new Button();
        btnGuardarConfiguracion = new Button();
        lblConfigEstado = new Label();
        tabConsulta = new TabPage();
        grpFiltrosConsulta = new GroupBox();
        chkFiltroAnioMes = new CheckBox();
        lblAnio = new Label();
        numAnio = new NumericUpDown();
        lblMes = new Label();
        numMes = new NumericUpDown();
        btnConsultarArchivos = new Button();
        btnConsultarResumen = new Button();
        btnConsultarDiariosMes = new Button();
        btnImportarInventarios = new Button();
        btnGenerarXmlDiario = new Button();
        lblProductosConsulta = new Label();
        comboProductos = new CheckedComboBox();
        lblMovimientosConsulta = new Label();
        comboMovimientos = new CheckedComboBox();
        lblDiaXml = new Label();
        dtpXmlDia = new DateTimePicker();
        chkGenerarTodoMes = new CheckBox();
        lblDiasXml = new Label();
        checkedDiasXml = new CheckedListBox();
        lblConsultaConteo = new Label();
        dgvConsulta = new DataGridView();
        grpModificacionMasiva = new GroupBox();
        lblFechaCalibracion = new Label();
        dtpCalibracionMasiva = new DateTimePicker();
        btnActualizarCalibracion = new Button();
        tabExportacion = new TabPage();
        grpImportacion = new GroupBox();
        btnImportarMensual = new Button();
        btnCancelar = new Button();
        progressBar1 = new ProgressBar();
        lblEstado = new Label();
        lblDetalle = new Label();
        splitExportacion = new SplitContainer();
        dgvDiarios = new DataGridView();
        lblDiarios = new Label();
        dgvDetalleDiario = new DataGridView();
        lblDetalleDiario = new Label();
        tabPrincipal.SuspendLayout();
        tabConfiguracion.SuspendLayout();
        tabConsulta.SuspendLayout();
        grpFiltrosConsulta.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numAnio).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numMes).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvConsulta).BeginInit();
        grpModificacionMasiva.SuspendLayout();
        tabExportacion.SuspendLayout();
        grpImportacion.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitExportacion).BeginInit();
        splitExportacion.Panel1.SuspendLayout();
        splitExportacion.Panel2.SuspendLayout();
        splitExportacion.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvDiarios).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvDetalleDiario).BeginInit();
        SuspendLayout();
        // 
        // tabPrincipal
        // 
        tabPrincipal.Controls.Add(tabConfiguracion);
        tabPrincipal.Controls.Add(tabConsulta);
        tabPrincipal.Controls.Add(tabExportacion);
        tabPrincipal.Dock = DockStyle.Fill;
        tabPrincipal.Location = new Point(0, 0);
        tabPrincipal.Margin = new Padding(3, 2, 3, 2);
        tabPrincipal.Name = "tabPrincipal";
        tabPrincipal.SelectedIndex = 0;
        tabPrincipal.Size = new Size(1194, 616);
        tabPrincipal.TabIndex = 0;
        // 
        // tabConfiguracion
        // 
        tabConfiguracion.Controls.Add(lblConnectionString);
        tabConfiguracion.Controls.Add(txtConnectionString);
        tabConfiguracion.Controls.Add(btnProbarConexion);
        tabConfiguracion.Controls.Add(btnGuardarConfiguracion);
        tabConfiguracion.Controls.Add(lblConfigEstado);
        tabConfiguracion.Location = new Point(4, 24);
        tabConfiguracion.Margin = new Padding(3, 2, 3, 2);
        tabConfiguracion.Name = "tabConfiguracion";
        tabConfiguracion.Padding = new Padding(14, 12, 14, 12);
        tabConfiguracion.Size = new Size(1186, 588);
        tabConfiguracion.TabIndex = 0;
        tabConfiguracion.Text = "Configuración";
        tabConfiguracion.UseVisualStyleBackColor = true;
        // 
        // lblConnectionString
        // 
        lblConnectionString.AutoSize = true;
        lblConnectionString.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblConnectionString.Location = new Point(21, 18);
        lblConnectionString.Name = "lblConnectionString";
        lblConnectionString.Size = new Size(227, 19);
        lblConnectionString.TabIndex = 0;
        lblConnectionString.Text = "Cadena de conexión PostgreSQL";
        // 
        // txtConnectionString
        // 
        txtConnectionString.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtConnectionString.Location = new Point(24, 42);
        txtConnectionString.Margin = new Padding(3, 2, 3, 2);
        txtConnectionString.Multiline = true;
        txtConnectionString.Name = "txtConnectionString";
        txtConnectionString.ScrollBars = ScrollBars.Vertical;
        txtConnectionString.Size = new Size(1136, 68);
        txtConnectionString.TabIndex = 1;
        // 
        // btnProbarConexion
        // 
        btnProbarConexion.Location = new Point(24, 123);
        btnProbarConexion.Margin = new Padding(3, 2, 3, 2);
        btnProbarConexion.Name = "btnProbarConexion";
        btnProbarConexion.Size = new Size(149, 28);
        btnProbarConexion.TabIndex = 2;
        btnProbarConexion.Text = "Probar conexión";
        btnProbarConexion.UseVisualStyleBackColor = true;
        btnProbarConexion.Click += btnProbarConexion_Click;
        // 
        // btnGuardarConfiguracion
        // 
        btnGuardarConfiguracion.Location = new Point(187, 123);
        btnGuardarConfiguracion.Margin = new Padding(3, 2, 3, 2);
        btnGuardarConfiguracion.Name = "btnGuardarConfiguracion";
        btnGuardarConfiguracion.Size = new Size(166, 28);
        btnGuardarConfiguracion.TabIndex = 3;
        btnGuardarConfiguracion.Text = "Guardar configuración";
        btnGuardarConfiguracion.UseVisualStyleBackColor = true;
        btnGuardarConfiguracion.Click += btnGuardarConfiguracion_Click;
        // 
        // lblConfigEstado
        // 
        lblConfigEstado.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblConfigEstado.Location = new Point(24, 166);
        lblConfigEstado.Name = "lblConfigEstado";
        lblConfigEstado.Size = new Size(1136, 36);
        lblConfigEstado.TabIndex = 4;
        lblConfigEstado.Text = "Sin validar conexión.";
        // 
        // tabConsulta
        // 
        tabConsulta.Controls.Add(grpModificacionMasiva);
        tabConsulta.Controls.Add(grpFiltrosConsulta);
        tabConsulta.Controls.Add(dgvConsulta);
        tabConsulta.Location = new Point(4, 24);
        tabConsulta.Margin = new Padding(3, 2, 3, 2);
        tabConsulta.Name = "tabConsulta";
        tabConsulta.Padding = new Padding(14, 12, 14, 12);
        tabConsulta.Size = new Size(1186, 588);
        tabConsulta.TabIndex = 1;
        tabConsulta.Text = "Consulta";
        tabConsulta.UseVisualStyleBackColor = true;
        // 
        // grpFiltrosConsulta
        // 
        grpFiltrosConsulta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpFiltrosConsulta.Controls.Add(chkFiltroAnioMes);
        grpFiltrosConsulta.Controls.Add(lblAnio);
        grpFiltrosConsulta.Controls.Add(numAnio);
        grpFiltrosConsulta.Controls.Add(lblMes);
        grpFiltrosConsulta.Controls.Add(numMes);
        grpFiltrosConsulta.Controls.Add(btnConsultarArchivos);
        grpFiltrosConsulta.Controls.Add(btnConsultarResumen);
        grpFiltrosConsulta.Controls.Add(btnConsultarDiariosMes);
        grpFiltrosConsulta.Controls.Add(btnImportarInventarios);
        grpFiltrosConsulta.Controls.Add(btnGenerarXmlDiario);
        grpFiltrosConsulta.Controls.Add(lblProductosConsulta);
        grpFiltrosConsulta.Controls.Add(comboProductos);
        grpFiltrosConsulta.Controls.Add(lblMovimientosConsulta);
        grpFiltrosConsulta.Controls.Add(comboMovimientos);
        grpFiltrosConsulta.Controls.Add(lblDiaXml);
        grpFiltrosConsulta.Controls.Add(dtpXmlDia);
        grpFiltrosConsulta.Controls.Add(chkGenerarTodoMes);
        grpFiltrosConsulta.Controls.Add(lblDiasXml);
        grpFiltrosConsulta.Controls.Add(checkedDiasXml);
        grpFiltrosConsulta.Controls.Add(lblConsultaConteo);
        grpFiltrosConsulta.Location = new Point(18, 15);
        grpFiltrosConsulta.Margin = new Padding(3, 2, 3, 2);
        grpFiltrosConsulta.Name = "grpFiltrosConsulta";
        grpFiltrosConsulta.Padding = new Padding(3, 2, 3, 2);
        grpFiltrosConsulta.Size = new Size(1152, 191);
        grpFiltrosConsulta.TabIndex = 0;
        grpFiltrosConsulta.TabStop = false;
        grpFiltrosConsulta.Text = "Filtros de consulta";
        // 
        // chkFiltroAnioMes
        // 
        chkFiltroAnioMes.AutoSize = true;
        chkFiltroAnioMes.Checked = true;
        chkFiltroAnioMes.CheckState = CheckState.Checked;
        chkFiltroAnioMes.Location = new Point(16, 28);
        chkFiltroAnioMes.Margin = new Padding(3, 2, 3, 2);
        chkFiltroAnioMes.Name = "chkFiltroAnioMes";
        chkFiltroAnioMes.Size = new Size(100, 19);
        chkFiltroAnioMes.TabIndex = 0;
        chkFiltroAnioMes.Text = "Filtrar periodo";
        chkFiltroAnioMes.UseVisualStyleBackColor = true;
        // 
        // lblAnio
        // 
        lblAnio.AutoSize = true;
        lblAnio.Location = new Point(149, 29);
        lblAnio.Name = "lblAnio";
        lblAnio.Size = new Size(29, 15);
        lblAnio.TabIndex = 1;
        lblAnio.Text = "Año";
        // 
        // numAnio
        // 
        numAnio.Location = new Point(186, 27);
        numAnio.Margin = new Padding(3, 2, 3, 2);
        numAnio.Maximum = new decimal(new int[] { 2100, 0, 0, 0 });
        numAnio.Minimum = new decimal(new int[] { 2000, 0, 0, 0 });
        numAnio.Name = "numAnio";
        numAnio.Size = new Size(79, 23);
        numAnio.TabIndex = 2;
        numAnio.Value = new decimal(new int[] { 2022, 0, 0, 0 });
        numAnio.ValueChanged += numAnio_ValueChanged;
        // 
        // lblMes
        // 
        lblMes.AutoSize = true;
        lblMes.Location = new Point(284, 29);
        lblMes.Name = "lblMes";
        lblMes.Size = new Size(29, 15);
        lblMes.TabIndex = 3;
        lblMes.Text = "Mes";
        // 
        // numMes
        // 
        numMes.Location = new Point(319, 27);
        numMes.Margin = new Padding(3, 2, 3, 2);
        numMes.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
        numMes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numMes.Name = "numMes";
        numMes.Size = new Size(61, 23);
        numMes.TabIndex = 4;
        numMes.Value = new decimal(new int[] { 1, 0, 0, 0 });
        numMes.ValueChanged += numMes_ValueChanged;
        // 
        // btnConsultarArchivos
        // 
        btnConsultarArchivos.Location = new Point(411, 22);
        btnConsultarArchivos.Margin = new Padding(3, 2, 3, 2);
        btnConsultarArchivos.Name = "btnConsultarArchivos";
        btnConsultarArchivos.Size = new Size(140, 28);
        btnConsultarArchivos.TabIndex = 5;
        btnConsultarArchivos.Text = "Consultar archivos";
        btnConsultarArchivos.UseVisualStyleBackColor = true;
        btnConsultarArchivos.Click += btnConsultarArchivos_Click;
        // 
        // btnConsultarResumen
        // 
        btnConsultarResumen.Location = new Point(565, 22);
        btnConsultarResumen.Margin = new Padding(3, 2, 3, 2);
        btnConsultarResumen.Name = "btnConsultarResumen";
        btnConsultarResumen.Size = new Size(149, 28);
        btnConsultarResumen.TabIndex = 6;
        btnConsultarResumen.Text = "Resumen mensual";
        btnConsultarResumen.UseVisualStyleBackColor = true;
        btnConsultarResumen.Click += btnConsultarResumen_Click;
        // 
        // btnConsultarDiariosMes
        // 
        btnConsultarDiariosMes.Location = new Point(728, 22);
        btnConsultarDiariosMes.Margin = new Padding(3, 2, 3, 2);
        btnConsultarDiariosMes.Name = "btnConsultarDiariosMes";
        btnConsultarDiariosMes.Size = new Size(114, 28);
        btnConsultarDiariosMes.TabIndex = 7;
        btnConsultarDiariosMes.Text = "Ver diarios";
        btnConsultarDiariosMes.UseVisualStyleBackColor = true;
        btnConsultarDiariosMes.Click += btnConsultarDiariosMes_Click;
        // 
        // btnImportarInventarios
        // 
        btnImportarInventarios.Location = new Point(16, 135);
        btnImportarInventarios.Margin = new Padding(3, 2, 3, 2);
        btnImportarInventarios.Name = "btnImportarInventarios";
        btnImportarInventarios.Size = new Size(158, 28);
        btnImportarInventarios.TabIndex = 17;
        btnImportarInventarios.Text = "Importar inventarios";
        btnImportarInventarios.UseVisualStyleBackColor = true;
        btnImportarInventarios.Click += btnImportarInventarios_Click;
        // 
        // btnGenerarXmlDiario
        // 
        btnGenerarXmlDiario.Location = new Point(184, 135);
        btnGenerarXmlDiario.Margin = new Padding(3, 2, 3, 2);
        btnGenerarXmlDiario.Name = "btnGenerarXmlDiario";
        btnGenerarXmlDiario.Size = new Size(149, 28);
        btnGenerarXmlDiario.TabIndex = 18;
        btnGenerarXmlDiario.Text = "Generar XML diario";
        btnGenerarXmlDiario.UseVisualStyleBackColor = true;
        btnGenerarXmlDiario.Click += btnGenerarXmlDiario_Click;
        // 
        // lblProductosConsulta
        // 
        lblProductosConsulta.AutoSize = true;
        lblProductosConsulta.Location = new Point(16, 64);
        lblProductosConsulta.Name = "lblProductosConsulta";
        lblProductosConsulta.Size = new Size(61, 15);
        lblProductosConsulta.TabIndex = 8;
        lblProductosConsulta.Text = "Productos";
        // 
        // comboProductos
        // 
        comboProductos.DropDownHeight = 260;
        comboProductos.Location = new Point(16, 82);
        comboProductos.Margin = new Padding(3, 2, 3, 2);
        comboProductos.Name = "comboProductos";
        comboProductos.Size = new Size(438, 24);
        comboProductos.TabIndex = 9;
        // 
        // lblMovimientosConsulta
        // 
        lblMovimientosConsulta.AutoSize = true;
        lblMovimientosConsulta.Location = new Point(477, 64);
        lblMovimientosConsulta.Name = "lblMovimientosConsulta";
        lblMovimientosConsulta.Size = new Size(114, 15);
        lblMovimientosConsulta.TabIndex = 10;
        lblMovimientosConsulta.Text = "Tipo de movimiento";
        // 
        // comboMovimientos
        // 
        comboMovimientos.DropDownHeight = 180;
        comboMovimientos.Location = new Point(477, 82);
        comboMovimientos.Margin = new Padding(3, 2, 3, 2);
        comboMovimientos.Name = "comboMovimientos";
        comboMovimientos.Size = new Size(262, 24);
        comboMovimientos.TabIndex = 11;
        // 
        // lblDiaXml
        // 
        lblDiaXml.AutoSize = true;
        lblDiaXml.Location = new Point(770, 64);
        lblDiaXml.Name = "lblDiaXml";
        lblDiaXml.Size = new Size(70, 15);
        lblDiaXml.TabIndex = 12;
        lblDiaXml.Text = "Día del XML";
        // 
        // dtpXmlDia
        // 
        dtpXmlDia.CustomFormat = "dd/MM/yyyy";
        dtpXmlDia.Format = DateTimePickerFormat.Custom;
        dtpXmlDia.Location = new Point(770, 82);
        dtpXmlDia.Margin = new Padding(3, 2, 3, 2);
        dtpXmlDia.Name = "dtpXmlDia";
        dtpXmlDia.Size = new Size(132, 23);
        dtpXmlDia.TabIndex = 13;
        dtpXmlDia.ValueChanged += dtpXmlDia_ValueChanged;
        // 
        // chkGenerarTodoMes
        // 
        chkGenerarTodoMes.Location = new Point(770, 109);
        chkGenerarTodoMes.Margin = new Padding(3, 2, 3, 2);
        chkGenerarTodoMes.Name = "chkGenerarTodoMes";
        chkGenerarTodoMes.Size = new Size(132, 48);
        chkGenerarTodoMes.TabIndex = 14;
        chkGenerarTodoMes.Text = "Generar diarios de todo el mes";
        chkGenerarTodoMes.UseVisualStyleBackColor = true;
        // 
        // lblDiasXml
        // 
        lblDiasXml.AutoSize = true;
        lblDiasXml.Location = new Point(914, 64);
        lblDiasXml.Name = "lblDiasXml";
        lblDiasXml.Size = new Size(81, 15);
        lblDiasXml.TabIndex = 15;
        lblDiasXml.Text = "Días a generar";
        // 
        // checkedDiasXml
        // 
        checkedDiasXml.CheckOnClick = true;
        checkedDiasXml.FormattingEnabled = true;
        checkedDiasXml.Location = new Point(914, 82);
        checkedDiasXml.Margin = new Padding(3, 2, 3, 2);
        checkedDiasXml.Name = "checkedDiasXml";
        checkedDiasXml.Size = new Size(219, 76);
        checkedDiasXml.TabIndex = 16;
        checkedDiasXml.ItemCheck += checkedDiasXml_ItemCheck;
        // 
        // lblConsultaConteo
        // 
        lblConsultaConteo.AutoSize = true;
        lblConsultaConteo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblConsultaConteo.Location = new Point(350, 142);
        lblConsultaConteo.Name = "lblConsultaConteo";
        lblConsultaConteo.Size = new Size(124, 15);
        lblConsultaConteo.TabIndex = 19;
        lblConsultaConteo.Text = "Registros cargados: 0";
        // 
        // dgvConsulta
        // 
        dgvConsulta.AllowUserToAddRows = false;
        dgvConsulta.AllowUserToDeleteRows = false;
        dgvConsulta.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgvConsulta.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvConsulta.Location = new Point(18, 280);
        dgvConsulta.Margin = new Padding(3, 2, 3, 2);
        dgvConsulta.MultiSelect = false;
        dgvConsulta.Name = "dgvConsulta";
        dgvConsulta.ReadOnly = true;
        dgvConsulta.RowHeadersWidth = 28;
        dgvConsulta.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvConsulta.Size = new Size(1152, 296);
        dgvConsulta.TabIndex = 1;
        dgvConsulta.CellDoubleClick += dgvConsulta_CellDoubleClick;
        // 
        // grpModificacionMasiva
        // 
        grpModificacionMasiva.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpModificacionMasiva.Controls.Add(lblFechaCalibracion);
        grpModificacionMasiva.Controls.Add(dtpCalibracionMasiva);
        grpModificacionMasiva.Controls.Add(btnActualizarCalibracion);
        grpModificacionMasiva.Location = new Point(18, 212);
        grpModificacionMasiva.Margin = new Padding(3, 2, 3, 2);
        grpModificacionMasiva.Name = "grpModificacionMasiva";
        grpModificacionMasiva.Size = new Size(1152, 60);
        grpModificacionMasiva.TabIndex = 2;
        grpModificacionMasiva.TabStop = false;
        grpModificacionMasiva.Text = "Modificación Masiva de Calibraciones (Mes/Año)";
        // 
        // lblFechaCalibracion
        // 
        lblFechaCalibracion.AutoSize = true;
        lblFechaCalibracion.Location = new Point(16, 28);
        lblFechaCalibracion.Name = "lblFechaCalibracion";
        lblFechaCalibracion.Size = new Size(140, 15);
        lblFechaCalibracion.TabIndex = 0;
        lblFechaCalibracion.Text = "Nueva Fecha Calibración:";
        // 
        // dtpCalibracionMasiva
        // 
        dtpCalibracionMasiva.Format = DateTimePickerFormat.Short;
        dtpCalibracionMasiva.Location = new Point(160, 25);
        dtpCalibracionMasiva.Name = "dtpCalibracionMasiva";
        dtpCalibracionMasiva.Size = new Size(120, 23);
        dtpCalibracionMasiva.TabIndex = 1;
        // 
        // btnActualizarCalibracion
        // 
        btnActualizarCalibracion.Location = new Point(290, 24);
        btnActualizarCalibracion.Name = "btnActualizarCalibracion";
        btnActualizarCalibracion.Size = new Size(150, 26);
        btnActualizarCalibracion.TabIndex = 2;
        btnActualizarCalibracion.Text = "Actualizar Vigencia";
        btnActualizarCalibracion.UseVisualStyleBackColor = true;
        btnActualizarCalibracion.Click += btnActualizarCalibracion_Click;
        // 
        // tabExportacion
        // 
        tabExportacion.Controls.Add(grpImportacion);
        tabExportacion.Controls.Add(splitExportacion);
        tabExportacion.Location = new Point(4, 24);
        tabExportacion.Margin = new Padding(3, 2, 3, 2);
        tabExportacion.Name = "tabExportacion";
        tabExportacion.Padding = new Padding(14, 12, 14, 12);
        tabExportacion.Size = new Size(1186, 588);
        tabExportacion.TabIndex = 2;
        tabExportacion.Text = "Exportación";
        tabExportacion.UseVisualStyleBackColor = true;
        // 
        // grpImportacion
        // 
        grpImportacion.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpImportacion.Controls.Add(btnImportarMensual);
        grpImportacion.Controls.Add(btnCancelar);
        grpImportacion.Controls.Add(progressBar1);
        grpImportacion.Controls.Add(lblEstado);
        grpImportacion.Controls.Add(lblDetalle);
        grpImportacion.Location = new Point(18, 15);
        grpImportacion.Margin = new Padding(3, 2, 3, 2);
        grpImportacion.Name = "grpImportacion";
        grpImportacion.Padding = new Padding(3, 2, 3, 2);
        grpImportacion.Size = new Size(1152, 90);
        grpImportacion.TabIndex = 0;
        grpImportacion.TabStop = false;
        grpImportacion.Text = "Importación de XML";
        // 
        // btnImportarMensual
        // 
        btnImportarMensual.Location = new Point(16, 24);
        btnImportarMensual.Margin = new Padding(3, 2, 3, 2);
        btnImportarMensual.Name = "btnImportarMensual";
        btnImportarMensual.Size = new Size(184, 28);
        btnImportarMensual.TabIndex = 0;
        btnImportarMensual.Text = "Importar mensual/diario";
        btnImportarMensual.UseVisualStyleBackColor = true;
        btnImportarMensual.Click += btnImportarMensual_Click;
        // 
        // btnCancelar
        // 
        btnCancelar.Enabled = false;
        btnCancelar.Location = new Point(214, 24);
        btnCancelar.Margin = new Padding(3, 2, 3, 2);
        btnCancelar.Name = "btnCancelar";
        btnCancelar.Size = new Size(105, 28);
        btnCancelar.TabIndex = 1;
        btnCancelar.Text = "Cancelar";
        btnCancelar.UseVisualStyleBackColor = true;
        btnCancelar.Click += btnCancelar_Click;
        // 
        // progressBar1
        // 
        progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressBar1.Location = new Point(337, 29);
        progressBar1.Margin = new Padding(3, 2, 3, 2);
        progressBar1.Name = "progressBar1";
        progressBar1.Size = new Size(796, 18);
        progressBar1.TabIndex = 2;
        // 
        // lblEstado
        // 
        lblEstado.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblEstado.Location = new Point(16, 60);
        lblEstado.Name = "lblEstado";
        lblEstado.Size = new Size(315, 25);
        lblEstado.TabIndex = 3;
        lblEstado.Text = "Sin proceso.";
        // 
        // lblDetalle
        // 
        lblDetalle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblDetalle.Location = new Point(337, 60);
        lblDetalle.Name = "lblDetalle";
        lblDetalle.Size = new Size(796, 25);
        lblDetalle.TabIndex = 4;
        // 
        // splitExportacion
        // 
        splitExportacion.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        splitExportacion.Location = new Point(18, 117);
        splitExportacion.Margin = new Padding(3, 2, 3, 2);
        splitExportacion.Name = "splitExportacion";
        splitExportacion.Orientation = Orientation.Horizontal;
        // 
        // splitExportacion.Panel1
        // 
        splitExportacion.Panel1.Controls.Add(dgvDiarios);
        splitExportacion.Panel1.Controls.Add(lblDiarios);
        // 
        // splitExportacion.Panel2
        // 
        splitExportacion.Panel2.Controls.Add(dgvDetalleDiario);
        splitExportacion.Panel2.Controls.Add(lblDetalleDiario);
        splitExportacion.Size = new Size(1152, 459);
        splitExportacion.SplitterDistance = 202;
        splitExportacion.SplitterWidth = 3;
        splitExportacion.TabIndex = 1;
        // 
        // dgvDiarios
        // 
        dgvDiarios.AllowUserToAddRows = false;
        dgvDiarios.AllowUserToDeleteRows = false;
        dgvDiarios.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvDiarios.Dock = DockStyle.Fill;
        dgvDiarios.Location = new Point(0, 21);
        dgvDiarios.Margin = new Padding(3, 2, 3, 2);
        dgvDiarios.MultiSelect = false;
        dgvDiarios.Name = "dgvDiarios";
        dgvDiarios.ReadOnly = true;
        dgvDiarios.RowHeadersWidth = 28;
        dgvDiarios.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvDiarios.Size = new Size(1152, 181);
        dgvDiarios.TabIndex = 1;
        dgvDiarios.CellDoubleClick += dgvDiarios_CellDoubleClick;
        // 
        // lblDiarios
        // 
        lblDiarios.Dock = DockStyle.Top;
        lblDiarios.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblDiarios.Location = new Point(0, 0);
        lblDiarios.Name = "lblDiarios";
        lblDiarios.Size = new Size(1152, 21);
        lblDiarios.TabIndex = 0;
        lblDiarios.Text = "Diarios derivados. Doble clic para ver detalle.";
        // 
        // dgvDetalleDiario
        // 
        dgvDetalleDiario.AllowUserToAddRows = false;
        dgvDetalleDiario.AllowUserToDeleteRows = false;
        dgvDetalleDiario.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvDetalleDiario.Dock = DockStyle.Fill;
        dgvDetalleDiario.Location = new Point(0, 21);
        dgvDetalleDiario.Margin = new Padding(3, 2, 3, 2);
        dgvDetalleDiario.MultiSelect = false;
        dgvDetalleDiario.Name = "dgvDetalleDiario";
        dgvDetalleDiario.ReadOnly = true;
        dgvDetalleDiario.RowHeadersWidth = 28;
        dgvDetalleDiario.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvDetalleDiario.Size = new Size(1152, 233);
        dgvDetalleDiario.TabIndex = 1;
        // 
        // lblDetalleDiario
        // 
        lblDetalleDiario.Dock = DockStyle.Top;
        lblDetalleDiario.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblDetalleDiario.Location = new Point(0, 0);
        lblDetalleDiario.Name = "lblDetalleDiario";
        lblDetalleDiario.Size = new Size(1152, 21);
        lblDetalleDiario.TabIndex = 0;
        lblDetalleDiario.Text = "Detalle diario";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1194, 616);
        Controls.Add(tabPrincipal);
        Margin = new Padding(3, 2, 3, 2);
        MinimumSize = new Size(1052, 580);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "COVOL Splitter - PostgreSQL";
        Load += MainForm_Load;
        tabPrincipal.ResumeLayout(false);
        tabConfiguracion.ResumeLayout(false);
        tabConfiguracion.PerformLayout();
        tabConsulta.ResumeLayout(false);
        grpFiltrosConsulta.ResumeLayout(false);
        grpFiltrosConsulta.PerformLayout();
        grpModificacionMasiva.ResumeLayout(false);
        grpModificacionMasiva.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numAnio).EndInit();
        ((System.ComponentModel.ISupportInitialize)numMes).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvConsulta).EndInit();
        tabExportacion.ResumeLayout(false);
        grpImportacion.ResumeLayout(false);
        splitExportacion.Panel1.ResumeLayout(false);
        splitExportacion.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitExportacion).EndInit();
        splitExportacion.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvDiarios).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvDetalleDiario).EndInit();
        ResumeLayout(false);
    }

    private GroupBox grpModificacionMasiva;
    private Label lblFechaCalibracion;
    private DateTimePicker dtpCalibracionMasiva;
    private Button btnActualizarCalibracion;
}