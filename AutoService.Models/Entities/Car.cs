namespace AutoService.Models;

public class Car : IEntity
{
    public int Id { get; set; }
    public int ClientId { get; set; }   // владелец
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;

    // так автомобиль показывается в списках: «Skoda Octavia (A123BC)»
    public override string ToString() => $"{Brand} {Model} ({PlateNumber})";
}