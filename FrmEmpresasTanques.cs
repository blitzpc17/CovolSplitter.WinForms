using CovolSplitter.WinForms.Models;
using CovolSplitter.WinForms.Services;

namespace CovolSplitter.WinForms;

public partial class FrmEmpresasTanques : Form
{
    private readonly EmpresasRepository _repo;
    private List<Empresa> _empresas = new();

    public FrmEmpresasTanques(EmpresasRepository repo)
    {
        InitializeComponent();
        _repo = repo;
    }

    private async void FrmEmpresasTanques_Load(object? sender, EventArgs e)
    {
        await CargarEmpresas();
    }

    private async Task CargarEmpresas()
    {
        _empresas = await _repo.GetAllEmpresasAsync();
        lstEmpresas.DataSource = _empresas;
        lstEmpresas.DisplayMember = "Nombre";
        lstEmpresas.ValueMember = "Id";
    }

    private async void btnNuevaEmpresa_Click(object? sender, EventArgs e)
    {
        string nombre = Microsoft.VisualBasic.Interaction.InputBox("Nombre de la Empresa:", "Nueva Empresa", "");
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            await _repo.CreateEmpresaAsync(nombre);
            await CargarEmpresas();
        }
    }

    private async void btnEliminarEmpresa_Click(object? sender, EventArgs e)
    {
        if (lstEmpresas.SelectedItem is Empresa emp)
        {
            if (MessageBox.Show($"¿Eliminar empresa {emp.Nombre}?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                await _repo.DeleteEmpresaAsync(emp.Id);
                await CargarEmpresas();
            }
        }
    }

    private async void lstEmpresas_SelectedIndexChanged(object? sender, EventArgs e)
    {
        txtTanqueMagna.Text = "";
        txtTanquePremium.Text = "";
        txtTanqueDiesel.Text = "";

        if (lstEmpresas.SelectedItem is Empresa emp)
        {
            var tanques = await _repo.GetTanquesByEmpresaAsync(emp.Id);
            
            foreach (var t in tanques)
            {
                if (t.Producto.Contains("MAGNA", StringComparison.OrdinalIgnoreCase))
                    txtTanqueMagna.Text = t.XmlTanque;
                else if (t.Producto.Contains("PREMIUM", StringComparison.OrdinalIgnoreCase))
                    txtTanquePremium.Text = t.XmlTanque;
                else if (t.Producto.Contains("DIESEL", StringComparison.OrdinalIgnoreCase))
                    txtTanqueDiesel.Text = t.XmlTanque;
            }
        }
    }

    private async void btnGuardarTanques_Click(object? sender, EventArgs e)
    {
        if (lstEmpresas.SelectedItem is Empresa emp)
        {
            try
            {
                btnGuardarTanques.Enabled = false;
                await _repo.SaveTanqueAsync(emp.Id, "MAGNA", txtTanqueMagna.Text.Trim());
                await _repo.SaveTanqueAsync(emp.Id, "PREMIUM", txtTanquePremium.Text.Trim());
                await _repo.SaveTanqueAsync(emp.Id, "DIESEL", txtTanqueDiesel.Text.Trim());
                MessageBox.Show("Cambios guardados correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
            finally
            {
                btnGuardarTanques.Enabled = true;
            }
        }
    }
}
