namespace AutoService.BLL;

public class Estimate
{
    public decimal PartsTotal { get; set; }      // запчасти с наценкой
    public decimal WorksTotal { get; set; }      // работы по нормо-часам
    public decimal ServicesTotal { get; set; }   // доп. услуги
    public decimal DiscountAmount { get; set; }  // сумма скидки
    public decimal Total { get; set; }           // итог к оплате
    public double NormHours { get; set; }        // всего нормо-часов
    public int WorkingDays { get; set; }         // примерный срок в рабочих днях
}