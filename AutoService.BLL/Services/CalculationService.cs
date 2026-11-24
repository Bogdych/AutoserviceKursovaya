using AutoService.DAL;
using AutoService.Models;

namespace AutoService.BLL;

public enum LineKind { Part, Work, Service }

// Одна строка сметы (Index нужен, чтобы знать, какую позицию удалять)
public record EstimateLine(LineKind Kind, int Index, string Name, string Quantity, decimal UnitPrice, decimal Sum);

public class CalculationService
{
    // Стоимость одного нормо-часа в долларах США
    public const decimal NormHourRate = 40m;

    // Сколько нормо-часов работы приходится на один рабочий день
    public const double WorkHoursPerDay = 8;

    private readonly IRepository<Part> _parts;
    private readonly IRepository<TechOperation> _operations;

    public CalculationService(IRepository<Part> parts, IRepository<TechOperation> operations)
    {
        _parts = parts;
        _operations = operations;
    }

    // Все позиции заказа в виде строк сметы
    public List<EstimateLine> BuildLines(Order order)
    {
        var lines = new List<EstimateLine>();

        // запчасти: цена с наценкой, умноженная на количество
        for (int i = 0; i < order.Parts.Count; i++)
        {
            var item = order.Parts[i];
            var part = _parts.GetById(item.PartId);
            if (part == null)
            {
                lines.Add(new(LineKind.Part, i, "(запчасть удалена из справочника)", item.Quantity.ToString(), 0, 0));
                continue;
            }

            decimal unit = Math.Round(PartService.PriceWithMarkup(part), 2);
            lines.Add(new(LineKind.Part, i, part.Name, item.Quantity.ToString(), unit, unit * item.Quantity));
        }

        // работы: нормо-часы, умноженные на стоимость часа
        for (int i = 0; i < order.OperationIds.Count; i++)
        {
            var operation = _operations.GetById(order.OperationIds[i]);
            if (operation == null)
            {
                lines.Add(new(LineKind.Work, i, "(операция удалена из справочника)", "—", 0, 0));
                continue;
            }

            decimal sum = Math.Round((decimal)operation.NormHours * NormHourRate, 2);
            lines.Add(new(LineKind.Work, i, operation.Name, $"{operation.NormHours:0.##} н/ч", NormHourRate, sum));
        }

        // дополнительные услуги
        for (int i = 0; i < order.Services.Count; i++)
        {
            var service = order.Services[i];
            lines.Add(new(LineKind.Service, i, service.Name, "1", service.Price, service.Price));
        }

        return lines;
    }

    public Estimate Calculate(Order order)
    {
        var lines = BuildLines(order);

        decimal parts = lines.Where(l => l.Kind == LineKind.Part).Sum(l => l.Sum);
        decimal works = lines.Where(l => l.Kind == LineKind.Work).Sum(l => l.Sum);
        decimal services = lines.Where(l => l.Kind == LineKind.Service).Sum(l => l.Sum);

        decimal subtotal = parts + works + services;
        decimal discount = Math.Round(subtotal * order.DiscountPercent / 100m, 2);

        double hours = order.OperationIds.Sum(id => _operations.GetById(id)?.NormHours ?? 0);

        return new Estimate
        {
            PartsTotal = parts,
            WorksTotal = works,
            ServicesTotal = services,
            DiscountAmount = discount,
            Total = subtotal - discount,
            NormHours = hours,
            WorkingDays = hours <= 0 ? 0 : (int)Math.Ceiling(hours / WorkHoursPerDay)
        };
    }
}