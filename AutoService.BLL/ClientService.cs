using AutoService.DAL;
using AutoService.Models;

namespace AutoService.BLL;

public class ClientService
{
    private readonly IRepository<Client> _clients;

    public ClientService(IRepository<Client> clients)
    {
        _clients = clients;
    }

    public List<Client> GetAll() => _clients.GetAll();

    public Client Add(string fullName, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Введите ФИО клиента.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Введите телефон клиента.");

        var client = new Client { FullName = fullName.Trim(), Phone = phone.Trim() };
        _clients.Add(client);
        return client;
    }
}