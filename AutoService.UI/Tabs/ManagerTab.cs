using AutoService.BLL;
using AutoService.Models;

namespace AutoService.UI;

public class ManagerTab : UserControl
{
    private readonly AppServices _app;

    private readonly ComboBox _clientCombo = new();
    private readonly ComboBox _carCombo = new();
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

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Padding = new Padding(0, 6, 6, 0)
    };

    private void BuildLayout()
    {
        GridHelper.Configure(_ordersGrid);

        // Верхняя панель: клиент -> его автомобиль -> создать заказ
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(8) };

        _clientCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _clientCombo.Width = 260;
        _clientCombo.SelectedIndexChanged += (s, e) => LoadClientCars();

        _carCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _carCombo.Width = 280;

        var createButton = new Button { Text = "Создать заказ", AutoSize = true };
        createButton.Click += CreateOrder_Click;

        top.Controls.AddRange(new Control[]
        {
            MakeLabel("Клиент:"), _clientCombo,
            MakeLabel("Автомобиль:"), _carCombo,
            createButton
        });

        // Нижняя панель: механики и статусы
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 95, Padding = new Padding(8) };

        _mechNameBox.PlaceholderText = "ФИО механика";
        _mechNameBox.Width = 220;
        _mechSpecBox.PlaceholderText = "Специализация";
        _mechSpecBox.Width = 180;
        var addMechButton = new Button { Text = "Добавить механика", AutoSize = true };
        addMechButton.Click += AddMechanic_Click;

        _mechanicCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _mechanicCombo.Width = 320; // показывает «ФИО — специализация»
        var assignButton = new Button { Text = "Назначить механика", AutoSize = true };
        assignButton.Click += Assign_Click;

        _statusCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _statusCombo.Width = 200;
        _statusCombo.Items.AddRange(Enum.GetValues<RepairStatus>().Select(StatusText).ToArray());
        _statusCombo.SelectedIndex = 0;
        var statusButton = new Button { Text = "Сменить статус", AutoSize = true };
        statusButton.Click += ChangeStatus_Click;

        bottom.Controls.AddRange(new Control[]
        {
            _mechNameBox, _mechSpecBox, addMechButton,
            MakeLabel("Механик:"), _mechanicCombo, assignButton,
            MakeLabel("Статус:"), _statusCombo, statusButton
        });
        bottom.SetFlowBreak(addMechButton, true);

        Controls.Add(_ordersGrid); // сначала Fill,
        Controls.Add(bottom);      // потом Bottom
        Controls.Add(top);         // и Top
    }

    public void RefreshAll()
    {
        _clientCombo.DataSource = _app.Clients.GetAll();
        _clientCombo.DisplayMember = "FullName";

        _mechanicCombo.DataSource = _app.Staff.GetAll(); // текст берётся из Employee.ToString()

        LoadClientCars();
        RefreshOrders();
    }

    // Список автомобилей зависит от выбранного клиента
    private void LoadClientCars()
    {
        if (_clientCombo.SelectedItem is Client client)
            _carCombo.DataSource = _app.Clients.GetCars(client.Id);
        else
            _carCombo.DataSource = null;
    }

    private void RefreshOrders()
    {
        var clients = _app.Clients.GetAll();
        var cars = _app.Cars.GetAll();
        var staff = _app.Staff.GetAll();

        _ordersGrid.DataSource = _app.Orders.GetAll().Select(o =>
        {
            var car = cars.FirstOrDefault(c => c.Id == o.CarId);
            var mechanic = staff.FirstOrDefault(s => s.Id == o.EmployeeId);
            return new
            {
                Номер = o.Id,
                Клиент = clients.FirstOrDefault(c => c.Id == o.ClientId)?.FullName ?? "—",
                Авто = car?.ToString() ?? "—",
                Механик = mechanic?.FullName ?? "не назначен",
                Специализация = mechanic?.Specialization ?? "—",
                Статус = StatusText(o.Status)
            };
        }).ToList();

        _ordersGrid.Columns["Номер"]!.HeaderText = "№";
        GridHelper.FixedWidth(_ordersGrid.Columns["Номер"]!, 60);
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
        if (_clientCombo.SelectedItem is not Client client)
        {
            MessageBox.Show("Сначала добавьте клиента на вкладке «Клиенты».");
            return;
        }
        if (_carCombo.SelectedItem is not Car car)
        {
            MessageBox.Show("У клиента нет автомобиля. Добавьте его на вкладке «Клиенты».");
            return;
        }

        _app.Orders.Create(client.Id, car.Id);
        RefreshOrders();
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