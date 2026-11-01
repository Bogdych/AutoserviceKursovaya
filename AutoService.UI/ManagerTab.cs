using AutoService.BLL;
using AutoService.Models;

namespace AutoService.UI;

public class ManagerTab : UserControl
{
    private readonly AppServices _app;

    private readonly ComboBox _clientCombo = new();
    private readonly TextBox _brandBox = new();
    private readonly TextBox _modelBox = new();
    private readonly TextBox _plateBox = new();

    private readonly DataGridView _ordersGrid = new();

    private readonly TextBox _mechNameBox = new();
    private readonly TextBox _mechSpecBox = new();
    private readonly ComboBox _mechanicCombo = new();
    private readonly ComboBox _statusCombo = new();

    public ManagerTab(AppServices app)
    {
        _app = app;
        Dock = DockStyle.Fill;
        BuildLayout();
        RefreshAll();
    }

    private void BuildLayout()
    {
        // Таблица заказов
        _ordersGrid.Dock = DockStyle.Fill;
        _ordersGrid.ReadOnly = true;
        _ordersGrid.AllowUserToAddRows = false;
        _ordersGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _ordersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _ordersGrid.BackgroundColor = SystemColors.Window;

        // Верхняя панель: создание заказа
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };
        _clientCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _clientCombo.Width = 220;
        _brandBox.PlaceholderText = "Марка";
        _brandBox.Width = 110;
        _modelBox.PlaceholderText = "Модель";
        _modelBox.Width = 110;
        _plateBox.PlaceholderText = "Госномер";
        _plateBox.Width = 110;

        var createButton = new Button { Text = "Создать заказ", AutoSize = true };
        createButton.Click += CreateOrder_Click;

        top.Controls.AddRange(new Control[]
        {
            new Label { Text = "Клиент:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) },
            _clientCombo, _brandBox, _modelBox, _plateBox, createButton
        });

        // Нижняя панель: механики и статусы
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 95, Padding = new Padding(8) };

        _mechNameBox.PlaceholderText = "ФИО механика";
        _mechNameBox.Width = 220;
        _mechSpecBox.PlaceholderText = "Специализация";
        _mechSpecBox.Width = 160;
        var addMechButton = new Button { Text = "Добавить механика", AutoSize = true };
        addMechButton.Click += AddMechanic_Click;

        _mechanicCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _mechanicCombo.Width = 220;
        var assignButton = new Button { Text = "Назначить механика", AutoSize = true };
        assignButton.Click += Assign_Click;

        _statusCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusCombo.Width = 160;
        _statusCombo.Items.AddRange(Enum.GetValues<RepairStatus>().Select(StatusText).ToArray());
        _statusCombo.SelectedIndex = 0;
        var statusButton = new Button { Text = "Сменить статус", AutoSize = true };
        statusButton.Click += ChangeStatus_Click;

        bottom.Controls.AddRange(new Control[]
        {
            _mechNameBox, _mechSpecBox, addMechButton,
            new Label { Text = "Механик:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) },
            _mechanicCombo, assignButton,
            new Label { Text = "Статус:", AutoSize = true, Padding = new Padding(10, 6, 0, 0) },
            _statusCombo, statusButton
        });
        bottom.SetFlowBreak(addMechButton, true); // перенос на вторую строку

        Controls.Add(_ordersGrid); // сначала Fill,
        Controls.Add(bottom);      // потом Bottom
        Controls.Add(top);         // и Top
    }

    // Обновляет выпадающие списки и таблицу
    public void RefreshAll()
    {
        _clientCombo.DataSource = _app.Clients.GetAll();
        _clientCombo.DisplayMember = "FullName";

        _mechanicCombo.DataSource = _app.Staff.GetAll();
        _mechanicCombo.DisplayMember = "FullName";

        RefreshOrders();
    }

    private void RefreshOrders()
    {
        var clients = _app.Clients.GetAll();
        var cars = _app.Cars.GetAll();
        var staff = _app.Staff.GetAll();

        _ordersGrid.DataSource = _app.Orders.GetAll().Select(o =>
        {
            var car = cars.FirstOrDefault(c => c.Id == o.CarId);
            return new
            {
                Номер = o.Id,
                Клиент = clients.FirstOrDefault(c => c.Id == o.ClientId)?.FullName ?? "—",
                Авто = car == null ? "—" : $"{car.Brand} {car.Model} ({car.PlateNumber})",
                Механик = staff.FirstOrDefault(s => s.Id == o.EmployeeId)?.FullName ?? "не назначен",
                Статус = StatusText(o.Status)
            };
        }).ToList();
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

    private int? SelectedOrderId() => _ordersGrid.CurrentRow?.Cells[0].Value as int?;

    private void CreateOrder_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_clientCombo.SelectedItem is not Client client)
                throw new ArgumentException("Сначала добавьте и выберите клиента.");

            _app.Orders.CreateWithCar(client.Id, _brandBox.Text, _modelBox.Text, _plateBox.Text);
            _brandBox.Clear();
            _modelBox.Clear();
            _plateBox.Clear();
            RefreshOrders();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void AddMechanic_Click(object? sender, EventArgs e)
    {
        try
        {
            _app.Staff.Add(_mechNameBox.Text, _mechSpecBox.Text);
            _mechNameBox.Clear();
            _mechSpecBox.Clear();
            RefreshAll();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода");
        }
    }

    private void Assign_Click(object? sender, EventArgs e)
    {
        if (SelectedOrderId() is not int orderId)
        {
            MessageBox.Show("Выберите заказ в таблице.");
            return;
        }
        if (_mechanicCombo.SelectedItem is not Employee mechanic)
        {
            MessageBox.Show("Сначала добавьте и выберите механика.");
            return;
        }

        _app.Orders.AssignMechanic(orderId, mechanic.Id);
        RefreshOrders();
    }

    private void ChangeStatus_Click(object? sender, EventArgs e)
    {
        if (SelectedOrderId() is not int orderId)
        {
            MessageBox.Show("Выберите заказ в таблице.");
            return;
        }

        // порядок пунктов списка совпадает с порядком значений RepairStatus
        _app.Orders.ChangeStatus(orderId, (RepairStatus)_statusCombo.SelectedIndex);
        RefreshOrders();
    }
}