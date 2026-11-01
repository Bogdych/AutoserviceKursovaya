namespace AutoService.Models;

public class TechOperation : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double NormHours { get; set; }       // нормо-часы
}