using System;

namespace pharmacyPOS.API.DTOs;

public class DailyRevenueDto
{
    public DateOnly Date { get; set; }
    public decimal Revenue { get; set; }
    public int SalesCount { get; set; }
}

