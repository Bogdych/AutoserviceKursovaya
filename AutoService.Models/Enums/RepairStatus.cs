namespace AutoService.Models;

public enum RepairStatus
{
    Queued,     // в очереди
    InProgress, // в работе
    OnHold,     // приостановлено (нет запчастей)
    TestDrive,  // тест-драйв
    Done        // завершено
}