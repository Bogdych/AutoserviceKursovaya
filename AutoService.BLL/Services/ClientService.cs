using AutoService.DAL;
using AutoService.Models;

namespace AutoService.BLL;

public class ClientService
{
    private readonly IRepository<Client> _clients;
    private readonly IRepository<Car> _cars;

    public ClientService(IRepository<Client> clients, IRepository<Car> cars)
    {
        _clients = clients;
        _cars = cars;
    }

    public List<Client> GetAll() => _clients.GetAll();

    public List<Car> GetCars(int clientId) =>
        _cars.GetAll().Where(c => c.ClientId == clientId).ToList();

    // Клиент приехал: записываем его сразу вместе с автомобилем
    public Client AddWithCar(string fullName, string phone, string brand, string model, string plate)
    {
        CheckClient(fullName, phone);
        CheckCar(brand, plate);   // сначала проверяем всё, чтобы не осталось клиента без авто

        var client = new Client { FullName = fullName.Trim(), Phone = phone.Trim() };
        _clients.Add(client);
        CreateCar(client.Id, brand, model, plate);
        return client;
    }

    // Старый клиент приехал на другой машине
    public Car AddCar(int clientId, string brand, string model, string plate)
    {
        if (_clients.GetById(clientId) == null)
            throw new ArgumentException("Клиент не найден.");

        CheckCar(brand, plate);
        return CreateCar(clientId, brand, model, plate);
    }

    private static void CheckClient(string fullName, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Введите ФИО клиента.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Введите телефон клиента.");
    }

    private void CheckCar(string brand, string plate)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("Укажите марку автомобиля.");
        if (string.IsNullOrWhiteSpace(plate))
            throw new ArgumentException("Укажите госномер автомобиля.");

        string normalized = plate.Trim().ToUpper();
        if (_cars.GetAll().Any(c => c.PlateNumber == normalized))
            throw new ArgumentException($"Автомобиль с госномером {normalized} уже есть в базе.");
    }

    private Car CreateCar(int clientId, string brand, string model, string plate)
    {
        var car = new Car
        {
            ClientId = clientId,
            Brand = brand.Trim(),
            Model = model.Trim(),
            PlateNumber = plate.Trim().ToUpper()
        };
        _cars.Add(car);
        return car;
    }
}