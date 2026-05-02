namespace pharmacyPOS.API.DTOs;

public class YearlyTrendDto
{
    public int Year { get; set; }
    public decimal Revenue { get; set; }
    public decimal CostOfSold { get; set; }
    public decimal GrossProfit { get; set; }
}

