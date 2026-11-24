using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AutoService.BLL;
using AutoService.Models;

namespace AutoService.Wpf.Views;

// Строка таблицы сметы
public record EstimateRow(LineKind Kind, int Index, string KindText, string Name, string Quantity, decimal UnitPrice, decimal Sum);

public partial class EstimateView : UserControl
{
    private readonly AppServices _app = App.Services;

    private sealed record OrderChoice(int Id, string Title)
    {
        public override string ToString() => Title;
    }

    private sealed record PartChoice(int Id, string Title)
    {
        public override string ToString() => Title;
    }

    private sealed record OperationChoice(int Id, string Title)
    {
        public override string ToString() => Title;
    }

    public EstimateView()
    {
        InitializeComponent();
        RefreshData();
    }

    // ---------- обновление данных ----------

    public void RefreshData()
    {
        int? keepId = (OrderCombo.SelectedItem as OrderChoice)?.Id;
        var clients = _app.Clients.GetAll();
        var cars = _app.Cars.GetAll();

        var orders = _app.Orders.GetAll().Select(o =>
        {
            var car = cars.FirstOrDefault(c => c.Id == o.CarId);
            string clientName = clients.FirstOrDefault(c => c.Id == o.ClientId)?.FullName ?? "—";
            return new OrderChoice(o.Id, $"№{o.Id} — {car?.ToString() ?? "авто не указано"} ({clientName})");
        }).ToList();

        OrderCombo.ItemsSource = orders;
        OrderCombo.SelectedItem = orders.FirstOrDefault(o => o.Id == keepId) ?? orders.FirstOrDefault();

        // справочники подтягиваем заново: в них могли добавить новое
        PartCombo.ItemsSource = _app.PartCatalog.GetAll()
            .Select(p => new PartChoice(p.Id, $"{p.Name} — {Money(PartService.PriceWithMarkup(p))}"))
            .ToList();
        if (PartCombo.Items.Count > 0) PartCombo.SelectedIndex = 0;

        OperationCombo.ItemsSource = _app.OperationCatalog.GetAll()
            .Select(o => new OperationChoice(o.Id, $"{o.Name} — {o.NormHours:0.##} н/ч"))
            .ToList();
        if (OperationCombo.Items.Count > 0) OperationCombo.SelectedIndex = 0;

        ShowOrder();
    }

    private static string Money(decimal value) => value.ToString("N2") + " $";

    private static string KindText(LineKind kind) => kind switch
    {
        LineKind.Part => "Запчасть",
        LineKind.Work => "Работа",
        _ => "Услуга"
    };

    private void OrderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ShowOrder();

    // Показывает смету выбранного заказа
    private void ShowOrder()
    {
        if (OrderCombo.SelectedItem is not OrderChoice choice)
        {
            LinesGrid.ItemsSource = null;
            DefectsList.ItemsSource = null;
            PartsTotalText.Text = WorksTotalText.Text = ServicesTotalText.Text = DiscountText.Text = TotalText.Text = "—";
            HoursText.Text = "";
            DiscountBox.Text = "0";
            return;
        }

        var order = _app.Orders.GetById(choice.Id);

        LinesGrid.ItemsSource = _app.Calculation.BuildLines(order)
            .Select(l => new EstimateRow(l.Kind, l.Index, KindText(l.Kind), l.Name, l.Quantity, l.UnitPrice, l.Sum))
            .ToList();

        var estimate = _app.Calculation.Calculate(order);
        PartsTotalText.Text = Money(estimate.PartsTotal);
        WorksTotalText.Text = Money(estimate.WorksTotal);
        ServicesTotalText.Text = Money(estimate.ServicesTotal);
        DiscountText.Text = "− " + Money(estimate.DiscountAmount);
        TotalText.Text = Money(estimate.Total);
        HoursText.Text = estimate.NormHours <= 0
            ? "Трудозатраты: —"
            : $"Трудозатраты: {estimate.NormHours:0.##} н/ч (≈ {estimate.WorkingDays} раб. дн.)";

        DiscountBox.Text = order.DiscountPercent.ToString("0.##");

        // замечания механика из диагностики
        var defects = new List<string>();
        defects.AddRange(order.Zones.Where(z => z.Value == ZoneState.Critical).Select(z => "Неисправно: " + z.Key));
        defects.AddRange(order.Zones.Where(z => z.Value == ZoneState.Warning).Select(z => "Требует внимания: " + z.Key));
        DefectsList.ItemsSource = defects;
    }

    // ---------- вспомогательное ----------

    private bool TryGetOrder(out int orderId)
    {
        if (OrderCombo.SelectedItem is OrderChoice choice)
        {
            orderId = choice.Id;
            return true;
        }
        MessageBox.Show("Сначала выберите заказ.");
        orderId = 0;
        return false;
    }

    // Понимает и «0,8», и «0.8» независимо от настроек Windows
    private static decimal? ToDecimal(string text) =>
        decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Number,
            CultureInfo.InvariantCulture, out var value) ? value : null;

    // ---------- действия ----------

    private void AddPart_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetOrder(out int orderId)) return;
        if (PartCombo.SelectedItem is not PartChoice part)
        {
            MessageBox.Show("Выберите запчасть. Если список пуст, заполните справочник на вкладке «Справочники».");
            return;
        }
        if (!int.TryParse(QtyBox.Text.Trim(), out int quantity) || quantity <= 0)
        {
            MessageBox.Show("Количество должно быть целым числом больше нуля.", "Ошибка ввода");
            return;
        }

        try
        {
            _app.Orders.AddPart(orderId, part.Id, quantity);
            QtyBox.Text = "1";
            ShowOrder();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void AddOperation_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetOrder(out int orderId)) return;
        if (OperationCombo.SelectedItem is not OperationChoice operation)
        {
            MessageBox.Show("Выберите работу. Если список пуст, заполните справочник на вкладке «Справочники».");
            return;
        }

        _app.Orders.AddOperation(orderId, operation.Id);
        ShowOrder();
    }

    private void AddService_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetOrder(out int orderId)) return;

        decimal? price = ToDecimal(ServicePriceBox.Text);
        if (price == null)
        {
            MessageBox.Show("Стоимость услуги должна быть числом.", "Ошибка ввода");
            return;
        }

        try
        {
            _app.Orders.AddService(orderId, ServiceNameBox.Text, price.Value);
            ServiceNameBox.Clear();
            ServicePriceBox.Clear();
            ShowOrder();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void ApplyDiscount_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetOrder(out int orderId)) return;

        decimal? percent = string.IsNullOrWhiteSpace(DiscountBox.Text) ? 0 : ToDecimal(DiscountBox.Text);
        if (percent == null)
        {
            MessageBox.Show("Скидка должна быть числом, например 10.", "Ошибка ввода");
            return;
        }

        try
        {
            _app.Orders.SetDiscount(orderId, percent.Value);
            ShowOrder();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void DeleteLine_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetOrder(out int orderId)) return;
        if (LinesGrid.SelectedItem is not EstimateRow row)
        {
            MessageBox.Show("Выберите строку в таблице.");
            return;
        }

        switch (row.Kind)
        {
            case LineKind.Part: _app.Orders.RemovePart(orderId, row.Index); break;
            case LineKind.Work: _app.Orders.RemoveOperation(orderId, row.Index); break;
            default: _app.Orders.RemoveService(orderId, row.Index); break;
        }
        ShowOrder();
    }
}