using AutoService.BLL;

namespace AutoService.UI;

public class ClientsTab : UserControl
{
    private readonly AppServices _app;
    private readonly DataGridView _grid = new();
    private readonly TextBox _nameBox = new();
    private readonly TextBox _phoneBox = new();
    private readonly TextBox _brandBox = new();
    private readonly TextBox _modelBox = new();
    private readonly TextBox _plateBox = new();

    public ClientsTab(AppServices app)
    {
        _app = app;
        Dock = DockStyle.Fill;
        BuildLayout();
        RefreshClients();
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Width = 90,
        Height = 27,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private void BuildLayout()
    {
        GridHelper.Configure(_grid);

        _nameBox.PlaceholderText = "ФИО";
        _nameBox.Width = 260;
        _phoneBox.PlaceholderText = "Телефон";
        _phoneBox.Width = 160;
        _brandBox.PlaceholderText = "Марка";
        _brandBox.Width = 120;
        _modelBox.PlaceholderText = "Модель";
        _modelBox.Width = 120;
        _plateBox.PlaceholderText = "Госномер";
        _plateBox.Width = 110;

        var addClientButton = new Button { Text = "Принять клиента", AutoSize = true };
        addClientButton.Click += AddClient_Click;

        var addCarButton = new Button { Text = "Добавить авто выбранному клиенту", AutoSize = true };
        addCarButton.Click += AddCar_Click;

        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 110, Padding = new Padding(8) };
        panel.Controls.AddRange(new Control[]
        {
            MakeLabel("Клиент:"), _nameBox, _phoneBox,
            MakeLabel("Автомобиль:"), _brandBox, _modelBox, _plateBox,
            addClientButton, addCarButton
        });
        panel.SetFlowBreak(_phoneBox, true); // вторая строка начинается с «Автомобиль:»

        Controls.Add(_grid);  // сначала Fill,
        Controls.Add(panel);  // потом Top
    }

    public void RefreshClients()
    {
        _grid.DataSource = _app.Clients.GetAll().Select(c => new
        {
            Номер = c.Id,
            ФИО = c.FullName,
            Телефон = c.Phone,
            Автомобили = string.Join(", ", _app.Clients.GetCars(c.Id))
        }).ToList();

        _grid.Columns["Номер"]!.HeaderText = "№";
        GridHelper.FixedWidth(_grid.Columns["Номер"]!, 60);
        GridHelper.FixedWidth(_grid.Columns["Телефон"]!, 170);
    }

    private int? SelectedClientId() => _grid.CurrentRow?.Cells[0].Value as int?;

    private void ClearCarInputs()
    {
        _brandBox.Clear();
        _modelBox.Clear();
        _plateBox.Clear();
    }

    private void AddClient_Click(object? sender, EventArgs e)
    {
        try
        {
            _app.Clients.AddWithCar(_nameBox.Text, _phoneBox.Text, _brandBox.Text, _modelBox.Text, _plateBox.Text);
            _nameBox.Clear();
            _phoneBox.Clear();
            ClearCarInputs();
            RefreshClients();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void AddCar_Click(object? sender, EventArgs e)
    {
        if (SelectedClientId() is not int clientId)
        {
            MessageBox.Show("Выберите клиента в таблице.");
            return;
        }

        try
        {
            _app.Clients.AddCar(clientId, _brandBox.Text, _modelBox.Text, _plateBox.Text);
            ClearCarInputs();
            RefreshClients();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }
}