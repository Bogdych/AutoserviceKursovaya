namespace AutoService.Models;

public enum ZoneState
{
    NotChecked, // ещё не проверено (серый)
    Ok,         // исправно (зелёный)
    Warning,    // требует внимания (жёлтый)
    Critical    // неисправно (красный)
}