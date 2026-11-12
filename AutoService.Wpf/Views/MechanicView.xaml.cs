using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoService.BLL;
using AutoService.Models;
using AutoService.Wpf.Car3D;

namespace AutoService.Wpf.Views;

public partial class MechanicView : UserControl
{
    private readonly AppServices _app = App.Services;
    private bool _modelRequested;
    private string? _selectedZone;

    // Элемент выпадающего списка заказов
    private sealed record OrderChoice(int Id, string Title)
    {
        public override string ToString() => Title;
    }

    public MechanicView()
    {
        InitializeComponent();
        Viewer3D.ZoneClicked += OnZoneClicked;
        HighlightLayerButtons(CarLayer.Body);
        RefreshData();
    }

    // Модель грузим один раз, когда вкладка открывается впервые
    private async void MechanicView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_modelRequested) return;
        _modelRequested = true;

        string folder = System.IO.Path.Combine(AppContext.BaseDirectory, "Models", "G63");
        await Viewer3D.LoadModelAsync(folder, new G63ZoneMap());
    }

    // ---------- заказы ----------

    public void RefreshData()
    {
        int? keepId = (OrderCombo.SelectedItem as OrderChoice)?.Id;
        var clients = _app.Clients.GetAll();
        var cars = _app.Cars.GetAll();

        var items = _app.Orders.GetAll().Select(o =>
        {
            var car = cars.FirstOrDefault(c => c.Id == o.CarId);
            string clientName = clients.FirstOrDefault(c => c.Id == o.ClientId)?.FullName ?? "—";
            return new OrderChoice(o.Id, $"№{o.Id} — {car?.ToString() ?? "авто не указано"} ({clientName})");
        }).ToList();

        OrderCombo.ItemsSource = items;
        OrderCombo.SelectedItem = items.FirstOrDefault(i => i.Id == keepId) ?? items.FirstOrDefault();
        ShowOrder();
    }

    private void OrderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedZone = null;
        Viewer3D.ClearSelection();
        ShowOrder();
        UpdateZoneText();
    }

    private void ShowOrder()
    {
        if (OrderCombo.SelectedItem is not OrderChoice choice)
        {
            Viewer3D.SetStates(new Dictionary<string, ZoneState>());
            CommentBox.Clear();
            DefectsList.ItemsSource = null;
            return;
        }

        var order = _app.Orders.GetById(choice.Id);
        CommentBox.Text = order.MechanicComment;
        ShowStates(order);
    }

    private void ShowStates(Order order)
    {
        Viewer3D.SetStates(order.Zones);

        var defects = new List<string>();
        defects.AddRange(order.Zones.Where(z => z.Value == ZoneState.Critical).Select(z => "Неисправно: " + z.Key));
        defects.AddRange(order.Zones.Where(z => z.Value == ZoneState.Warning).Select(z => "Требует внимания: " + z.Key));
        DefectsList.ItemsSource = defects;
    }

    // ---------- слои и виды ----------

    private void SetLayer(CarLayer layer)
    {
        Viewer3D.Layer = layer;
        _selectedZone = null;
        UpdateZoneText();
        HighlightLayerButtons(layer);
    }

    private void HighlightLayerButtons(CarLayer layer)
    {
        BodyButton.Background = layer == CarLayer.Body ? Brushes.LightSteelBlue : SystemColors.ControlBrush;
        InnerButton.Background = layer == CarLayer.Inner ? Brushes.LightSteelBlue : SystemColors.ControlBrush;
    }

    private void BodyLayer_Click(object sender, RoutedEventArgs e) => SetLayer(CarLayer.Body);
    private void InnerLayer_Click(object sender, RoutedEventArgs e) => SetLayer(CarLayer.Inner);

    private void ViewThreeQuarter_Click(object sender, RoutedEventArgs e) => Viewer3D.SetView(CarViewPreset.ThreeQuarter);
    private void ViewFront_Click(object sender, RoutedEventArgs e) => Viewer3D.SetView(CarViewPreset.Front);
    private void ViewRear_Click(object sender, RoutedEventArgs e) => Viewer3D.SetView(CarViewPreset.Rear);
    private void ViewLeft_Click(object sender, RoutedEventArgs e) => Viewer3D.SetView(CarViewPreset.Left);
    private void ViewRight_Click(object sender, RoutedEventArgs e) => Viewer3D.SetView(CarViewPreset.Right);
    private void ViewTop_Click(object sender, RoutedEventArgs e) => Viewer3D.SetView(CarViewPreset.Top);

    // ---------- выбранная деталь и её состояние ----------

    private void OnZoneClicked(string zone)
    {
        _selectedZone = zone;
        UpdateZoneText();
    }

    private static string StateName(ZoneState state) => state switch
    {
        ZoneState.Ok => "исправно",
        ZoneState.Warning => "требует внимания",
        ZoneState.Critical => "неисправно",
        _ => "не проверено"
    };

    private void UpdateZoneText()
    {
        if (_selectedZone == null)
        {
            ZoneText.Text = "Нажмите на деталь автомобиля";
            return;
        }

        var state = ZoneState.NotChecked;
        if (OrderCombo.SelectedItem is OrderChoice choice)
            _app.Orders.GetById(choice.Id).Zones.TryGetValue(_selectedZone, out state);

        ZoneText.Text = $"{_selectedZone}\nСостояние: {StateName(state)}";
    }

    private void ApplyState(ZoneState state)
    {
        if (OrderCombo.SelectedItem is not OrderChoice choice)
        {
            MessageBox.Show("Сначала выберите заказ.");
            return;
        }
        if (_selectedZone == null)
        {
            MessageBox.Show("Сначала нажмите на деталь автомобиля.");
            return;
        }

        _app.Orders.SetZoneState(choice.Id, _selectedZone, state);
        ShowStates(_app.Orders.GetById(choice.Id));
        UpdateZoneText();
    }

    private void StateOk_Click(object sender, RoutedEventArgs e) => ApplyState(ZoneState.Ok);
    private void StateWarning_Click(object sender, RoutedEventArgs e) => ApplyState(ZoneState.Warning);
    private void StateCritical_Click(object sender, RoutedEventArgs e) => ApplyState(ZoneState.Critical);
    private void StateReset_Click(object sender, RoutedEventArgs e) => ApplyState(ZoneState.NotChecked);

    private void SaveComment_Click(object sender, RoutedEventArgs e)
    {
        if (OrderCombo.SelectedItem is not OrderChoice choice)
        {
            MessageBox.Show("Сначала выберите заказ.");
            return;
        }

        _app.Orders.SetComment(choice.Id, CommentBox.Text);
        MessageBox.Show("Комментарий сохранён.");
    }
}