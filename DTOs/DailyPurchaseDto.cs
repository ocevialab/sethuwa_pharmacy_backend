using System;

namespace pharmacyPOS.API.DTOs;

public class DailyPurchaseDto
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

