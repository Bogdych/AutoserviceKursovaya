using System.Windows;
using System.Windows.Controls;

namespace AutoService.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // При переходе на вкладку подтягиваем свежие данные
    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // выпадающие списки внутри вкладок тоже шлют это событие, их пропускаем
        if (e.OriginalSource != MainTabs) return;

        ClientsPage?.RefreshData();
        ManagerPage?.RefreshData();
    }
}