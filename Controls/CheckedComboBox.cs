using CovolSplitter.Winforms.Models;
using CovolSplitter.WinForms.Models;

namespace CovolSplitter.WinForms.Controls;

public sealed class CheckedComboBox : UserControl
{
    private readonly ComboBox _combo;
    private readonly CheckedListBox _checkedList;
    private readonly ToolStripDropDown _dropDown;
    private readonly ToolStripControlHost _host;

    public event EventHandler? SelectionChanged;

    public CheckedComboBox()
    {
        Height = 32;

        _combo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _combo.DropDown += Combo_DropDown;
        _combo.MouseDown += Combo_MouseDown;

        _checkedList = new CheckedListBox
        {
            CheckOnClick = true,
            BorderStyle = BorderStyle.None
        };

        _checkedList.ItemCheck += CheckedList_ItemCheck;

        _host = new ToolStripControlHost(_checkedList)
        {
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            AutoSize = false
        };

        _dropDown = new ToolStripDropDown
        {
            Padding = Padding.Empty,
            AutoClose = true
        };

        _dropDown.Items.Add(_host);

        Controls.Add(_combo);

        UpdateText();
    }

    public int DropDownHeight { get; set; } = 220;

    public void SetItems(IEnumerable<FilterOption> items, string allText)
    {
        _checkedList.ItemCheck -= CheckedList_ItemCheck;

        _checkedList.Items.Clear();

        _checkedList.Items.Add(new FilterOption
        {
            Value = "TODOS",
            Text = allText
        }, true);

        foreach (var item in items)
            _checkedList.Items.Add(item, false);

        _checkedList.ItemCheck += CheckedList_ItemCheck;

        UpdateText();
    }

    public string[] GetSelectedValues()
    {
        var values = new List<string>();

        foreach (var item in _checkedList.CheckedItems)
        {
            if (item is not FilterOption option)
                continue;

            if (option.Value == "TODOS")
                return Array.Empty<string>();

            values.Add(option.Value);
        }

        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray();
    }

    public void SelectAll()
    {
        if (_checkedList.Items.Count == 0)
            return;

        for (var i = 0; i < _checkedList.Items.Count; i++)
            _checkedList.SetItemChecked(i, i == 0);

        UpdateText();
    }

    private void Combo_MouseDown(object? sender, MouseEventArgs e)
    {
        ShowDropDown();
    }

    private void Combo_DropDown(object? sender, EventArgs e)
    {
        BeginInvoke(new Action(ShowDropDown));
    }

    private void ShowDropDown()
    {
        _dropDown.Close();

        var width = Math.Max(Width, 380);

        _checkedList.Width = width;
        _checkedList.Height = DropDownHeight;

        _host.Size = new Size(width, DropDownHeight);

        _dropDown.Show(this, new Point(0, Height));
    }

    private void CheckedList_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_checkedList.Items[e.Index] is not FilterOption option)
            return;

        BeginInvoke(new Action(() =>
        {
            if (option.Value == "TODOS" && e.NewValue == CheckState.Checked)
            {
                for (var i = 0; i < _checkedList.Items.Count; i++)
                {
                    if (i != e.Index)
                        _checkedList.SetItemChecked(i, false);
                }
            }

            if (option.Value != "TODOS" && e.NewValue == CheckState.Checked)
            {
                for (var i = 0; i < _checkedList.Items.Count; i++)
                {
                    if (_checkedList.Items[i] is FilterOption item && item.Value == "TODOS")
                    {
                        _checkedList.SetItemChecked(i, false);
                        break;
                    }
                }
            }

            var algunoMarcado = false;

            for (var i = 0; i < _checkedList.Items.Count; i++)
            {
                if (_checkedList.GetItemChecked(i))
                {
                    algunoMarcado = true;
                    break;
                }
            }

            if (!algunoMarcado && _checkedList.Items.Count > 0)
                _checkedList.SetItemChecked(0, true);

            UpdateText();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }));
    }

    private void UpdateText()
    {
        var selected = _checkedList.CheckedItems
            .Cast<object>()
            .OfType<FilterOption>()
            .ToList();

        string text;

        if (selected.Count == 0)
            text = "Seleccione...";
        else if (selected.Any(x => x.Value == "TODOS"))
            text = selected.First(x => x.Value == "TODOS").Text;
        else if (selected.Count == 1)
            text = selected[0].Text;
        else
            text = $"{selected.Count} seleccionados";

        _combo.DataSource = null;
        _combo.Items.Clear();
        _combo.Items.Add(text);
        _combo.SelectedIndex = 0;
    }
}