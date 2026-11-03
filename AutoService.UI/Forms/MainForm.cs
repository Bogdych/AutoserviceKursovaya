using AutoService.BLL;

namespace AutoService.UI;

public partial class MainForm : Form
{
    private readonly AppServices _app;

    public MainForm(AppServices app)
    {
        _app = app;
        InitializeComponent();

        Text = "Автосервис";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;

        var clientsTab = new ClientsTab(_app);
        var managerTab = new ManagerTab(_app);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreatePage("Клиенты", clientsTab));
        tabs.TabPages.Add(CreatePage("Менеджер", managerTab));

        // при переходе на вкладку подтягиваем свежие данные
        tabs.SelectedIndexChanged += (s, e) =>
        {
            clientsTab.RefreshClients();
            managerTab.RefreshAll();
        };

        Controls.Add(tabs);
    }

    private static TabPage CreatePage(string title, Control content)
    {
        var page = new TabPage(title);
        page.Controls.Add(content);
        return page;
    }
}