using AutoService.DAL;
using AutoService.Models;

namespace AutoService.BLL;

public class CalculationService
{
    // Стоимость одного нормо-часа (потом можно вынести в настройки)
    public const decimal NormHourRate = 1500m;

    private readonly IRepository<Part> _parts;
    private readonly IRepository<TechOperation> _operations;

    public CalculationService(IRepository<Part> parts, IRepository<TechOperation> operations)
    {
        _parts = parts;
        _operations = operations;
    }

    public Estimate Calculate(Order order)
    {
        // 1. Запчасти: цена + наценка, умножить на количество
        decimal partsTotal = 0;
        foreach (var item in order.Parts)
        {
            var part = _parts.GetById(item.PartId);
            if (part == null) continue;

            decimal priceWithMarkup = part.Price * (1 + part.MarkupPercent / 100m);
            partsTotal += priceWithMarkup * item.Quantity;
        }

        // 2. Работы: сумма нормо-часов, умноженная на стоимость часа
        double totalHours = 0;
        foreach (int operationId in order.OperationIds)
        {
            var operation = _operations.GetById(operationId);
            if (operation != null)
                totalHours += operation.NormHours;
        }
        decimal worksTotal = (decimal)totalHours * NormHourRate;

        // 3. Дополнительные услуги
        decimal servicesTotal = order.Services.Sum(s => s.Price);

        // 4. Скидка и итог
        decimal subtotal = partsTotal + worksTotal + servicesTotal;
        decimal discount = subtotal * order.DiscountPercent / 100m;

        return new Estimate
        {
            PartsTotal = partsTotal,
            WorksTotal = worksTotal,
            ServicesTotal = servicesTotal,
            DiscountAmount = discount,
            Total = subtotal - discount
        };
    }
}