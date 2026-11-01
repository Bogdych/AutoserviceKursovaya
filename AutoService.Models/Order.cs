namespace AutoService.Models;

public class OrderPart
{
    public int PartId { get; set; }
    public int Quantity { get; set; }
}

public class AdditionalService
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class Order : IEntity
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int CarId { get; set; }
    public int? EmployeeId { get; set; }        // механик, может быть ещё не назначен
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public RepairStatus Status { get; set; } = RepairStatus.Queued;

    // диагностика: название зоны -> состояние
    public Dictionary<string, ZoneState> Zones { get; set; } = new();
    public string MechanicComment { get; set; } = string.Empty;

    public List<int> OperationIds { get; set; } = new();
    public List<OrderPart> Parts { get; set; } = new();
    public List<AdditionalService> Services { get; set; } = new();
    public decimal DiscountPercent { get; set; }
}