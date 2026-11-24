using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AutoService.BLL;

namespace AutoService.Wpf.Views;

// Строки таблиц справочников
public record PartRow(int Id, string Name, decimal Price, decimal Markup, decimal Final);
public record OperationRow(int Id, string Name, double Hours);

public partial class CatalogView : UserControl
{
    private readonly AppServices _app = App.Services;

    public CatalogView()
    {
        InitializeComponent();
        RefreshAll();
    }

    public void RefreshAll()
    {
        RefreshParts();
        RefreshOperations();
    }

    private void RefreshParts()
    {
        PartsGrid.ItemsSource = _app.PartCatalog.GetAll()
            .Select(p => new PartRow(p.Id, p.Name, p.Price, p.MarkupPercent, PartService.PriceWithMarkup(p)))
            .ToList();
    }

    private void RefreshOperations()
    {
        OperationsGrid.ItemsSource = _app.OperationCatalog.GetAll()
            .Select(o => new OperationRow(o.Id, o.Name, o.NormHours))
            .ToList();
    }

    // Понимает и «0,8», и «0.8» независимо от настроек Windows
    private static decimal? ToDecimal(string text) =>
        decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Number,
            CultureInfo.InvariantCulture, out var value) ? value : null;

    private static double? ToDouble(string text) =>
        double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Number,
            CultureInfo.InvariantCulture, out var value) ? value : null;

    private void AddExamples_Click(object sender, RoutedEventArgs e)
    {
        _app.PartCatalog.AddExamples();
        _app.OperationCatalog.AddExamples();
        RefreshAll();
    }

    // ---------- запчасти ----------

    private void AddPart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            decimal? price = ToDecimal(PartPriceBox.Text);
            decimal? markup = string.IsNullOrWhiteSpace(PartMarkupBox.Text) ? 0 : ToDecimal(PartMarkupBox.Text);
            if (price == null || markup == null)
                throw new ArgumentException("Цена и наценка должны быть числами.");

            _app.PartCatalog.Add(PartNameBox.Text, price.Value, markup.Value);
            PartNameBox.Clear();
            PartPriceBox.Clear();
            PartMarkupBox.Clear();
            RefreshParts();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void DeletePart_Click(object sender, RoutedEventArgs e)
    {
        if (PartsGrid.SelectedItem is not PartRow row)
        {
            MessageBox.Show("Выберите запчасть в таблице.");
            return;
        }
        if (MessageBox.Show($"Удалить «{row.Name}»?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        _app.PartCatalog.Delete(row.Id);
        RefreshParts();
    }

    // ---------- операции ----------

    private void AddOperation_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            double? hours = ToDouble(OpHoursBox.Text);
            if (hours == null)
                throw new ArgumentException("Нормо-часы должны быть числом, например 0,8.");

            _app.OperationCatalog.Add(OpNameBox.Text, hours.Value);
            OpNameBox.Clear();
            OpHoursBox.Clear();
            RefreshOperations();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void DeleteOperation_Click(object sender, RoutedEventArgs e)
    {
        if (OperationsGrid.SelectedItem is not OperationRow row)
        {
            MessageBox.Show("Выберите операцию в таблице.");
            return;
        }
        if (MessageBox.Show($"Удалить «{row.Name}»?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        _app.OperationCatalog.Delete(row.Id);
        RefreshOperations();
    }
}