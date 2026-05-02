using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.DTOs;

public class PurchaseDetailsDto
{
    public decimal TotalPurchaseAmount { get; set; }
    public int TotalPurchaseCount { get; set; }
    public decimal PaidPurchasesAmount { get; set; }
    public int PaidPurchasesCount { get; set; }
    public decimal UnpaidPurchasesAmount { get; set; }
    public int UnpaidPurchasesCount { get; set; }
    public List<DailyPurchaseDto>? DailyBreakdown { get; set; }
}

