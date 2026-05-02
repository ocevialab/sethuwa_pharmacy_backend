namespace pharmacyPOS.API.DTOs;

public class PaymentMethodBreakdownDto
{
    public decimal CashAmount { get; set; }
    public int CashCount { get; set; }
    public decimal CardAmount { get; set; }
    public int CardCount { get; set; }
    public decimal BankAmount { get; set; }
    public int BankCount { get; set; }
    public decimal PayLaterAmount { get; set; }
    public int PayLaterCount { get; set; }
    public decimal OtherAmount { get; set; }
    public int OtherCount { get; set; }
}

