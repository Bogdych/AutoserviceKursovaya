using System.IO;
using System.Windows;
using AutoService.BLL;

namespace AutoService.Wpf;

public partial class App : Application
{
    // Один общий набор сервисов на всю программу
    public static AppServices Services { get; } =
        new AppServices(Path.Combine(AppContext.BaseDirectory, "Data"));
}