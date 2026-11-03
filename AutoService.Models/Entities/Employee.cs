namespace AutoService.Models;

public class Employee : IEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    // так механик показывается в списках: «ФИО — специализация»
    public override string ToString() =>
        string.IsNullOrWhiteSpace(Specialization) ? FullName : $"{FullName} — {Specialization}";
}