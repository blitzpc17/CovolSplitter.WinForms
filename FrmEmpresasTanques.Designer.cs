namespace CovolSplitter.WinForms;

partial class FrmEmpresasTanques
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lstEmpresas = new ListBox();
        btnNuevaEmpresa = new Button();
        btnEliminarEmpresa = new Button();
        txtTanqueMagna = new TextBox();
        txtTanquePremium = new TextBox();
        txtTanqueDiesel = new TextBox();
        btnGuardarTanques = new Button();
        label1 = new Label();
        label2 = new Label();
        label3 = new Label();
        label4 = new Label();
        SuspendLayout();
        
        // lstEmpresas
        lstEmpresas.FormattingEnabled = true;
        lstEmpresas.ItemHeight = 15;
        lstEmpresas.Location = new Point(12, 33);
        lstEmpresas.Name = "lstEmpresas";
        lstEmpresas.Size = new Size(200, 394);
        lstEmpresas.TabIndex = 0;
        lstEmpresas.SelectedIndexChanged += lstEmpresas_SelectedIndexChanged;
        
        // label4
        label4.AutoSize = true;
        label4.Location = new Point(12, 12);
        label4.Name = "label4";
        label4.Size = new Size(59, 15);
        label4.TabIndex = 9;
        label4.Text = "Empresas:";
        
        // btnNuevaEmpresa
        btnNuevaEmpresa.Location = new Point(12, 435);
        btnNuevaEmpresa.Name = "btnNuevaEmpresa";
        btnNuevaEmpresa.Size = new Size(95, 23);
        btnNuevaEmpresa.TabIndex = 1;
        btnNuevaEmpresa.Text = "Nueva";
        btnNuevaEmpresa.UseVisualStyleBackColor = true;
        btnNuevaEmpresa.Click += btnNuevaEmpresa_Click;
        
        // btnEliminarEmpresa
        btnEliminarEmpresa.Location = new Point(117, 435);
        btnEliminarEmpresa.Name = "btnEliminarEmpresa";
        btnEliminarEmpresa.Size = new Size(95, 23);
        btnEliminarEmpresa.TabIndex = 2;
        btnEliminarEmpresa.Text = "Eliminar";
        btnEliminarEmpresa.UseVisualStyleBackColor = true;
        btnEliminarEmpresa.Click += btnEliminarEmpresa_Click;
        
        // txtTanqueMagna
        txtTanqueMagna.Location = new Point(230, 33);
        txtTanqueMagna.Multiline = true;
        txtTanqueMagna.Name = "txtTanqueMagna";
        txtTanqueMagna.ScrollBars = ScrollBars.Vertical;
        txtTanqueMagna.Size = new Size(350, 110);
        txtTanqueMagna.TabIndex = 3;
        
        // label1
        label1.AutoSize = true;
        label1.Location = new Point(230, 12);
        label1.Name = "label1";
        label1.Size = new Size(137, 15);
        label1.TabIndex = 6;
        label1.Text = "XML Tanque Magna (PR03)";
        
        // txtTanquePremium
        txtTanquePremium.Location = new Point(230, 172);
        txtTanquePremium.Multiline = true;
        txtTanquePremium.Name = "txtTanquePremium";
        txtTanquePremium.ScrollBars = ScrollBars.Vertical;
        txtTanquePremium.Size = new Size(350, 110);
        txtTanquePremium.TabIndex = 4;
        
        // label2
        label2.AutoSize = true;
        label2.Location = new Point(230, 151);
        label2.Name = "label2";
        label2.Size = new Size(149, 15);
        label2.TabIndex = 7;
        label2.Text = "XML Tanque Premium (PR08)";
        
        // txtTanqueDiesel
        txtTanqueDiesel.Location = new Point(230, 317);
        txtTanqueDiesel.Multiline = true;
        txtTanqueDiesel.Name = "txtTanqueDiesel";
        txtTanqueDiesel.ScrollBars = ScrollBars.Vertical;
        txtTanqueDiesel.Size = new Size(350, 110);
        txtTanqueDiesel.TabIndex = 5;
        
        // label3
        label3.AutoSize = true;
        label3.Location = new Point(230, 296);
        label3.Name = "label3";
        label3.Size = new Size(134, 15);
        label3.TabIndex = 8;
        label3.Text = "XML Tanque Diesel (PR07)";
        
        // btnGuardarTanques
        btnGuardarTanques.Location = new Point(230, 435);
        btnGuardarTanques.Name = "btnGuardarTanques";
        btnGuardarTanques.Size = new Size(350, 23);
        btnGuardarTanques.TabIndex = 10;
        btnGuardarTanques.Text = "Guardar Cambios XML Tanques";
        btnGuardarTanques.UseVisualStyleBackColor = true;
        btnGuardarTanques.Click += btnGuardarTanques_Click;
        
        // FrmEmpresasTanques
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(600, 470);
        Controls.Add(btnGuardarTanques);
        Controls.Add(label4);
        Controls.Add(label3);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(txtTanqueDiesel);
        Controls.Add(txtTanquePremium);
        Controls.Add(txtTanqueMagna);
        Controls.Add(btnEliminarEmpresa);
        Controls.Add(btnNuevaEmpresa);
        Controls.Add(lstEmpresas);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "FrmEmpresasTanques";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Configuración de Tanques por Empresa";
        Load += FrmEmpresasTanques_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    private ListBox lstEmpresas;
    private Button btnNuevaEmpresa;
    private Button btnEliminarEmpresa;
    private TextBox txtTanqueMagna;
    private TextBox txtTanquePremium;
    private TextBox txtTanqueDiesel;
    private Button btnGuardarTanques;
    private Label label1;
    private Label label2;
    private Label label3;
    private Label label4;
}
