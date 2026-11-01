using AutoService.DAL;
using AutoService.Models;

namespace AutoService.BLL;

public class OrderService
{
    private readonly IRepository<Order> _orders;
     private readonly IRepository<Car> _cars;

    public OrderService(IRepository<Order> orders, IRepository<Car> cars)
    {
        _orders = orders;
        _cars = cars;
    }
    

    public List<Order> GetAll() => _orders.GetAll();

    public Order GetById(int orderId) =>
        _orders.GetById(orderId)
        ?? throw new InvalidOperationException($"Заказ {orderId} не найден.");

    public Order Create(int clientId, int carId)
    {
        var order = new Order { ClientId = clientId, CarId = carId };
        _orders.Add(order);
        return order;
    }
    
    public Order CreateWithCar(int clientId, string brand, string model, string plate)
    {
        if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(plate))
            throw new ArgumentException("Укажите марку и госномер автомобиля.");

        var car = new Car
        {
            ClientId = clientId,
            Brand = brand.Trim(),
            Model = model.Trim(),
            PlateNumber = plate.Trim().ToUpper()
        };
        _cars.Add(car);

        return Create(clientId, car.Id);
    }

    // Клик по зоне схемы авто: меняем её состояние
    public void SetZoneState(int orderId, string zoneName, ZoneState state)
    {
        var order = GetById(orderId);
        order.Zones[zoneName] = state;
        _orders.Update(order);
    }

    public void AddPart(int orderId, int partId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Количество должно быть больше нуля.");

        var order = GetById(orderId);
        order.Parts.Add(new OrderPart { PartId = partId, Quantity = quantity });
        _orders.Update(order);
    }

    public void AddOperation(int orderId, int operationId)
    {
        var order = GetById(orderId);
        order.OperationIds.Add(operationId);
        _orders.Update(order);
    }

    public void AssignMechanic(int orderId, int employeeId)
    {
        var order = GetById(orderId);
        order.EmployeeId = employeeId;
        _orders.Update(order);
    }

    public void ChangeStatus(int orderId, RepairStatus status)
    {
        var order = GetById(orderId);
        order.Status = status;
        _orders.Update(order);
    }
}