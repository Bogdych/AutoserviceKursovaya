using AutoService.BLL;

namespace AutoService.UI;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new AppServices(Path.Combine(AppContext.BaseDirectory, "Data"));
        Application.Run(new MainForm(services));
    }
}