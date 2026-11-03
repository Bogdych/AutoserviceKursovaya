namespace AutoService.Models;

public class Part : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }          // закупочная цена
    public decimal MarkupPercent { get; set; }  // наценка в процентах
}