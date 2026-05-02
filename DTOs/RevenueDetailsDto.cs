using System;
using System.Collections.Generic;

namespace pharmacyPOS.API.DTOs;

public class RevenueDetailsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalRoundingDiscounts { get; set; }
    public decimal NetRevenue { get; set; }
    public int PaidSalesCount { get; set; }
    public int UnpaidSalesCount { get; set; }
    public List<DailyRevenueDto>? DailyBreakdown { get; set; }
}

