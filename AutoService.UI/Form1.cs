using AutoService.BLL;

namespace AutoService.UI;

public partial class Form1 : Form
{
    private readonly AppServices _app;
    private readonly DataGridView _clientsGrid = new();
    private readonly TextBox _nameBox = new();
    private readonly TextBox _phoneBox = new();

    public Form1(AppServices app)
    {
        _app = app;
        InitializeComponent();

        Text = "Автосервис";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateClientsTab());

        var managerTab = new ManagerTab(_app);
        var managerPage = new TabPage("Менеджер");
        managerPage.Controls.Add(managerTab);
        tabs.TabPages.Add(managerPage);

        tabs.TabPages.Add(new TabPage("Механик"));
        tabs.SelectedIndexChanged += (s, e) => managerTab.RefreshAll(); // подтянуть новых клиентов
        Controls.Add(tabs);

        RefreshClients();
    }

    private TabPage CreateClientsTab()
    {
        var page = new TabPage("Клиенты");

        // Верхняя панель: поля ввода и кнопка
        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(8) };
        _nameBox.PlaceholderText = "ФИО";
        _nameBox.Width = 250;
        _phoneBox.PlaceholderText = "Телефон";
        _phoneBox.Width = 150;

        var addButton = new Button { Text = "Добавить", AutoSize = true };
        addButton.Click += AddClient_Click;

        topPanel.Controls.AddRange(new Control[] { _nameBox, _phoneBox, addButton });

        // Таблица клиентов
        _clientsGrid.Dock = DockStyle.Fill;
        _clientsGrid.ReadOnly = true;
        _clientsGrid.AllowUserToAddRows = false;
        _clientsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _clientsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _clientsGrid.BackgroundColor = SystemColors.Window;

        page.Controls.Add(_clientsGrid); // сначала Fill,
        page.Controls.Add(topPanel);     // потом Top
        return page;
    }

    private void AddClient_Click(object? sender, EventArgs e)
    {
        try
        {
            _app.Clients.Add(_nameBox.Text, _phoneBox.Text);
            _nameBox.Clear();
            _phoneBox.Clear();
            RefreshClients();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void RefreshClients()
    {
        _clientsGrid.DataSource = _app.Clients.GetAll();
        _clientsGrid.Columns["Id"]!.HeaderText = "№";
        _clientsGrid.Columns["FullName"]!.HeaderText = "ФИО";
        _clientsGrid.Columns["Phone"]!.HeaderText = "Телефон";
    }
}