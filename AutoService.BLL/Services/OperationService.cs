using AutoService.DAL;
using AutoService.Models;

namespace AutoService.BLL;

public class OperationService
{
    private readonly IRepository<TechOperation> _operations;

    public OperationService(IRepository<TechOperation> operations)
    {
        _operations = operations;
    }

    public List<TechOperation> GetAll() => _operations.GetAll();

    public TechOperation Add(string name, double normHours)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Введите название операции.");
        if (normHours <= 0)
            throw new ArgumentException("Норма времени должна быть больше нуля.");

        string clean = name.Trim();
        if (_operations.GetAll().Any(o => o.Name.Equals(clean, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Операция с таким названием уже есть в справочнике.");

        var operation = new TechOperation { Name = clean, NormHours = normHours };
        _operations.Add(operation);
        return operation;
    }

    public void Delete(int id) => _operations.Delete(id);

    // Заполняет пустой справочник примерами
    public void AddExamples()
    {
        if (_operations.GetAll().Count > 0) return;

        Add("Замена передних тормозных колодок", 0.8);
        Add("Замена задних тормозных колодок", 0.8);
        Add("Замена передних тормозных дисков", 1.2);
        Add("Замена масла и масляного фильтра", 0.5);
        Add("Замена воздушного фильтра", 0.3);
        Add("Замена свечей зажигания", 0.7);
        Add("Замена переднего амортизатора", 1.5);
        Add("Замена аккумулятора", 0.3);
        Add("Диагностика ходовой части", 0.6);
        Add("Развал-схождение", 1.0);
    }
}