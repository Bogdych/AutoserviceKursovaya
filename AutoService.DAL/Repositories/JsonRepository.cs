using System.Text.Json;
using System.Text.Json.Serialization;
using AutoService.Models;

namespace AutoService.DAL;

public class JsonRepository<T> : IRepository<T> where T : IEntity
{
    private readonly string _filePath;
    private readonly List<T> _items;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,                         // читаемый JSON с отступами
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // кириллица читаемо
        Converters = { new JsonStringEnumConverter() } // enum как текст, а не число
    };

    public JsonRepository(string filePath)
    {
        _filePath = filePath;
        _items = Load();
    }

    private List<T> Load()
    {
        if (!File.Exists(_filePath))
            return new List<T>();

        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>();
    }

    private void Save()
    {
        string? folder = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(_filePath, JsonSerializer.Serialize(_items, Options));
    }

    public List<T> GetAll() => new List<T>(_items);

    public T? GetById(int id) => _items.FirstOrDefault(x => x.Id == id);

    public void Add(T item)
    {
        item.Id = _items.Count == 0 ? 1 : _items.Max(x => x.Id) + 1;
        _items.Add(item);
        Save();
    }

    public void Update(T item)
    {
        int index = _items.FindIndex(x => x.Id == item.Id);
        if (index < 0)
            throw new InvalidOperationException($"Запись с Id={item.Id} не найдена.");

        _items[index] = item;
        Save();
    }

    public void Delete(int id)
    {
        _items.RemoveAll(x => x.Id == id);
        Save();
    }
}