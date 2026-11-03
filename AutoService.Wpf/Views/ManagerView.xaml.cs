using System.Windows;
using System.Windows.Controls;
using AutoService.BLL;
using AutoService.Models;

namespace AutoService.Wpf.Views;

// Одна строка таблицы заказов
public record OrderRow(int Id, string Client, string Car, string Mechanic, string Specialization, string Status);

public partial class ManagerView : UserControl
{
    private readonly AppServices _app = App.Services;

    public ManagerView()
    {
        InitializeComponent();

        StatusCombo.ItemsSource = Enum.GetValues<RepairStatus>().Select(StatusText).ToList();
        StatusCombo.SelectedIndex = 0;

        RefreshData();
    }

    public void RefreshData()
    {
        // клиенты (прежний выбор сохраняем)
        int? keepClientId = (ClientCombo.SelectedItem as Client)?.Id;
        var clients = _app.Clients.GetAll();
        ClientCombo.ItemsSource = clients;
        ClientCombo.SelectedItem = clients.FirstOrDefault(c => c.Id == keepClientId) ?? clients.FirstOrDefault();

        // механики (в списке показывается «ФИО — специализация»)
        MechanicCombo.ItemsSource = _app.Staff.GetAll();
        if (MechanicCombo.Items.Count > 0)
            MechanicCombo.SelectedIndex = 0;

        RefreshOrders();
    }

    private void RefreshOrders()
    {
        int? keepId = (OrdersGrid.SelectedItem as OrderRow)?.Id;

        var clients = _app.Clients.GetAll();
        var cars = _app.Cars.GetAll();
        var staff = _app.Staff.GetAll();

        var rows = _app.Orders.GetAll().Select(o =>
        {
            var car = cars.FirstOrDefault(c => c.Id == o.CarId);
            var mechanic = staff.FirstOrDefault(s => s.Id == o.EmployeeId);
            return new OrderRow(
                o.Id,
                clients.FirstOrDefault(c => c.Id == o.ClientId)?.FullName ?? "—",
                car?.ToString() ?? "—",
                mechanic?.FullName ?? "не назначен",
                mechanic?.Specialization ?? "—",
                StatusText(o.Status));
        }).ToList();

        OrdersGrid.ItemsSource = rows;
        OrdersGrid.SelectedItem = rows.FirstOrDefault(r => r.Id == keepId); // выделение не теряется
    }

    private static string StatusText(RepairStatus status) => status switch
    {
        RepairStatus.Queued => "В очереди",
        RepairStatus.InProgress => "В работе",
        RepairStatus.OnHold => "Приостановлено (нет запчастей)",
        RepairStatus.TestDrive => "Тест-драйв",
        RepairStatus.Done => "Завершено",
        _ => status.ToString()
    };

    // Список автомобилей зависит от выбранного клиента
    private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClientCombo.SelectedItem is Client client)
        {
            CarCombo.ItemsSource = _app.Clients.GetCars(client.Id);
            CarCombo.SelectedIndex = CarCombo.Items.Count > 0 ? 0 : -1;
        }
        else
        {
            CarCombo.ItemsSource = null;
        }
    }

    private void CreateOrder_Click(object sender, RoutedEventArgs e)
    {
        if (ClientCombo.SelectedItem is not Client client)
        {
            MessageBox.Show("Сначала добавьте клиента на вкладке «Клиенты».");
            return;
        }
        if (CarCombo.SelectedItem is not Car car)
        {
            MessageBox.Show("У клиента нет автомобиля. Добавьте его на вкладке «Клиенты».");
            return;
        }

        _app.Orders.Create(client.Id, car.Id);
        RefreshOrders();
    }

    private void AddMechanic_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _app.Staff.Add(MechNameBox.Text, MechSpecBox.Text);
            MechNameBox.Clear();
            MechSpecBox.Clear();
            RefreshData();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void Assign_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not OrderRow row)
        {
            MessageBox.Show("Выберите заказ в таблице.");
            return;
        }
        if (MechanicCombo.SelectedItem is not Employee mechanic)
        {
            MessageBox.Show("Сначала добавьте и выберите механика.");
            return;
        }

        _app.Orders.AssignMechanic(row.Id, mechanic.Id);
        RefreshOrders();
    }

    private void ChangeStatus_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not OrderRow row)
        {
            MessageBox.Show("Выберите заказ в таблице.");
            return;
        }

        // порядок пунктов списка совпадает с порядком значений RepairStatus
        _app.Orders.ChangeStatus(row.Id, (RepairStatus)StatusCombo.SelectedIndex);
        RefreshOrders();
    }
}