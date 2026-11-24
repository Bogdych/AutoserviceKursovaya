using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using AutoService.BLL;

namespace AutoService.Wpf;

public partial class App : Application
{
    private static AppServices? _services;

    // Один общий набор сервисов на всю программу (создаётся при первом обращении)
    public static AppServices Services =>
        _services ??= new AppServices(Path.Combine(AppContext.BaseDirectory, "Data"));

    public App()
    {
        // Числа в таблицах показываем по правилам системного языка (запятая в дробях)
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        // Вместо молчаливого закрытия показываем текст ошибки
        DispatcherUnhandledException += (s, e) =>
        {
            ShowError(e.Exception);
            e.Handled = true;

            // если главное окно так и не открылось, закрываем программу, чтобы она не висела невидимкой
            if (MainWindow == null || !MainWindow.IsVisible) Shutdown(1);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowError(e.ExceptionObject as Exception);
    }

    private static void ShowError(Exception? ex)
    {
        MessageBox.Show(ex?.ToString() ?? "Неизвестная ошибка", "Ошибка программы");
    }
}