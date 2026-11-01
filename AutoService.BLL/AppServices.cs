using AutoService.DAL;
using AutoService.Models;

namespace AutoService.BLL;

public class AppServices
{
    public IRepository<Car> Cars { get; }
    public IRepository<Employee> Employees { get; }
    public IRepository<Part> Parts { get; }
    public IRepository<TechOperation> Operations { get; }

    public ClientService Clients { get; }
    public EmployeeService Staff { get; }
    public OrderService Orders { get; }
    public CalculationService Calculation { get; }

    public AppServices(string dataFolder)
    {
        var clients = new JsonRepository<Client>(Path.Combine(dataFolder, "clients.json"));
        var orders = new JsonRepository<Order>(Path.Combine(dataFolder, "orders.json"));

        Cars = new JsonRepository<Car>(Path.Combine(dataFolder, "cars.json"));
        Employees = new JsonRepository<Employee>(Path.Combine(dataFolder, "employees.json"));
        Parts = new JsonRepository<Part>(Path.Combine(dataFolder, "parts.json"));
        Operations = new JsonRepository<TechOperation>(Path.Combine(dataFolder, "operations.json"));

        Clients = new ClientService(clients);
        Staff = new EmployeeService(Employees);
        Orders = new OrderService(orders, Cars);
        Calculation = new CalculationService(Parts, Operations);
    }
}