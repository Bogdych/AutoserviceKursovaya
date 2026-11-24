using AutoService.DAL;
using AutoService.Models;

namespace AutoService.BLL;

public class PartService
{
    private readonly IRepository<Part> _parts;

    public PartService(IRepository<Part> parts)
    {
        _parts = parts;
    }

    public List<Part> GetAll() => _parts.GetAll();

    public Part Add(string name, decimal price, decimal markupPercent)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Введите название запчасти.");
        if (price <= 0)
            throw new ArgumentException("Цена должна быть больше нуля.");
        if (markupPercent < 0)
            throw new ArgumentException("Наценка не может быть отрицательной.");

        string clean = name.Trim();
        if (_parts.GetAll().Any(p => p.Name.Equals(clean, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Запчасть с таким названием уже есть в справочнике.");

        var part = new Part { Name = clean, Price = price, MarkupPercent = markupPercent };
        _parts.Add(part);
        return part;
    }

    public void Delete(int id) => _parts.Delete(id);

    // Цена для клиента: закупочная цена плюс наценка
    public static decimal PriceWithMarkup(Part part) => part.Price * (1 + part.MarkupPercent / 100m);

    // Заполняет пустой справочник примерами (для проверки и демонстрации)
     // Заполняет пустой справочник примерами (цены в долларах США)
    public void AddExamples()
    {
        if (_parts.GetAll().Count > 0) return;

        Add("Колодки тормозные передние", 45, 20);
        Add("Колодки тормозные задние", 38, 20);
        Add("Диск тормозной передний", 60, 25);
        Add("Масляный фильтр", 8, 30);
        Add("Моторное масло 5W-30 (4 л)", 35, 20);
        Add("Воздушный фильтр", 12, 30);
        Add("Свеча зажигания", 6, 30);
        Add("Амортизатор передний", 90, 20);
        Add("Аккумулятор", 110, 15);
        Add("Лампа фары", 8, 30);
    }
}