using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Utilities;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class FinanceController : ControllerBase
{
    private readonly ThilankaPharmacyDbContext _context;
    private readonly ILogger<FinanceController> _logger;

    public FinanceController(
        ThilankaPharmacyDbContext context,
        ILogger<FinanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /*  GET REVENUE TRENDS - MONTHLY OR YEARLY  */
    [RequirePermission("finance:view_reports")]
    [HttpGet("revenue-trend")]
    public async Task<IActionResult> GetRevenueTrend(string period = "monthly", int? year = null, int? startYear = null, int? endYear = null)
    {
        _logger.LogInformation("Fetching revenue trend - Period: {Period}, Year: {Year}, StartYear: {StartYear}, EndYear: {EndYear}",
            period, year, startYear, endYear);

        period = period?.ToLower() ?? "monthly";

        if (period == "monthly")
        {
            // Monthly view: Show 12 months of a specific year
            if (!year.HasValue)
            {
                year = DateTime.UtcNow.Year;
            }

            var result = new List<MonthlyTrendDto>();

            for (int month = 1; month <= 12; month++)
            {
                var start = new DateTime(year.Value, month, 1);
                var end = start.AddMonths(1);

                // 1. Revenue = Paid Sales (from Payments table)
                var revenue = await _context.Payments
                    .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
                    .SumAsync(p => (decimal?)p.PaymentAmount) ?? 0;

                // 2. Cost of Sold (COGS) — from StockMovement
                var cogs = await (
                    from sm in _context.StockMovements
                    join stock in _context.Stocks on sm.Stock_Id equals stock.StockId
                    where sm.Reason == "Sale" &&
                          sm.CreatedAt >= start &&
                          sm.CreatedAt < end
                    select (decimal?)(sm.QuantityChanged * stock.CostPrice)
                ).SumAsync() ?? 0m;

                // 3. Gross Profit
                decimal profit = revenue - cogs;

                result.Add(new MonthlyTrendDto
                {
                    Month = start.ToString("MMM"),
                    Revenue = revenue,
                    CostOfSold = cogs,
                    GrossProfit = profit
                });
            }

            return Ok(new { Period = "monthly", Year = year.Value, Data = result });
        }
        else if (period == "yearly")
        {
            // Yearly view: Show multiple years
            if (!startYear.HasValue || !endYear.HasValue)
            {
                // Default to last 5 years if not specified
                var currentYear = DateTime.UtcNow.Year;
                startYear = currentYear - 4;
                endYear = currentYear;
            }

            if (startYear.Value > endYear.Value)
            {
                return BadRequest("Start year must be less than or equal to end year.");
            }

            var result = new List<YearlyTrendDto>();

            for (int y = startYear.Value; y <= endYear.Value; y++)
            {
                var start = new DateTime(y, 1, 1);
                var end = start.AddYears(1);

                // 1. Revenue = Paid Sales (from Payments table)
                var revenue = await _context.Payments
                    .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
                    .SumAsync(p => (decimal?)p.PaymentAmount) ?? 0;

                // 2. Cost of Sold (COGS) — from StockMovement
                var cogs = await (
                    from sm in _context.StockMovements
                    join stock in _context.Stocks on sm.Stock_Id equals stock.StockId
                    where sm.Reason == "Sale" &&
                          sm.CreatedAt >= start &&
                          sm.CreatedAt < end
                    select (decimal?)(sm.QuantityChanged * stock.CostPrice)
                ).SumAsync() ?? 0m;

                // 3. Gross Profit
                decimal profit = revenue - cogs;

                result.Add(new YearlyTrendDto
                {
                    Year = y,
                    Revenue = revenue,
                    CostOfSold = cogs,
                    GrossProfit = profit
                });
            }

            return Ok(new { Period = "yearly", StartYear = startYear.Value, EndYear = endYear.Value, Data = result });
        }
        else
        {
            return BadRequest("Period must be 'monthly' or 'yearly'.");
        }
    }

    /*  GET SUPPLIER EXPENSES - MONTHLY OR YEARLY  */
    [RequirePermission("finance:view_reports")]
    [HttpGet("supplier-expenses")]
    public async Task<IActionResult> GetSupplierExpenses(string period = "monthly", int? month = null, int? year = null, int? startYear = null, int? endYear = null)
    {
        _logger.LogInformation("Fetching supplier expenses - Period: {Period}, Month: {Month}, Year: {Year}, StartYear: {StartYear}, EndYear: {EndYear}",
            period, month, year, startYear, endYear);

        period = period?.ToLower() ?? "monthly";

        if (period == "monthly")
        {
            // Monthly view: Show expenses for a specific month
            if (!month.HasValue || !year.HasValue)
            {
                var now = DateTime.UtcNow;
                month = now.Month;
                year = now.Year;
            }

            var start = new DateOnly(year.Value, month.Value, 1);
            var end = start.AddMonths(1);

            var result = await (
                from p in _context.Purchases
                join s in _context.Suppliers on p.SupplierId equals s.SupplierId
                where p.InvoiceDate >= start && p.InvoiceDate < end
                group p by new { s.SupplierName } into g
                select new SupplierExpenseDto
                {
                    SupplierName = g.Key.SupplierName,
                    TotalExpense = g.Sum(x => x.TotalAmount),
                    InvoiceCount = g.Count()
                }
            ).ToListAsync();

            return Ok(new { Period = "monthly", Month = month.Value, Year = year.Value, Data = result });
        }
        else if (period == "yearly")
        {
            // Yearly view: Show expenses aggregated by year
            if (!startYear.HasValue || !endYear.HasValue)
            {
                var currentYear = DateTime.UtcNow.Year;
                startYear = currentYear - 4;
                endYear = currentYear;
            }

            if (startYear.Value > endYear.Value)
            {
                return BadRequest("Start year must be less than or equal to end year.");
            }

            var start = new DateOnly(startYear.Value, 1, 1);
            var end = new DateOnly(endYear.Value + 1, 1, 1);

            var result = await (
                from p in _context.Purchases
                join s in _context.Suppliers on p.SupplierId equals s.SupplierId
                where p.InvoiceDate >= start && p.InvoiceDate < end
                group p by new { s.SupplierName, Year = p.InvoiceDate.Year } into g
                select new
                {
                    SupplierName = g.Key.SupplierName,
                    Year = g.Key.Year,
                    TotalExpense = g.Sum(x => x.TotalAmount),
                    InvoiceCount = g.Count()
                }
            ).ToListAsync();

            // Group by supplier and aggregate across years
            var groupedResult = result
                .GroupBy(x => x.SupplierName)
                .Select(g => new SupplierExpenseDto
                {
                    SupplierName = g.Key,
                    TotalExpense = g.Sum(x => x.TotalExpense),
                    InvoiceCount = g.Sum(x => x.InvoiceCount)
                })
                .OrderByDescending(x => x.TotalExpense)
                .ToList();

            return Ok(new { Period = "yearly", StartYear = startYear.Value, EndYear = endYear.Value, Data = groupedResult });
        }
        else
        {
            return BadRequest("Period must be 'monthly' or 'yearly'.");
        }
    }

    [RequirePermission("finance:view_reports")]
    [HttpGet("expiry-alerts")]
    public async Task<IActionResult> GetExpiryAlerts()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddDays(30);

        var items = await _context.Stocks
            .Where(s => s.ExpireDate <= threshold)
            .ToListAsync();

        var result = new List<ExpiryAlertDto>();

        foreach (var item in items)
        {
            string name = await GetProductName(item.ProductSku);

            result.Add(new ExpiryAlertDto
            {
                ProductName = name,
                LotNumber = item.LotNumber,
                ExpireDate = item.ExpireDate,
                DaysLeft = (item.ExpireDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).Days
            });
        }

        return Ok(result);
    }

    private async Task<string> GetProductName(string productSku)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductSku == productSku);
        if (product == null)
            return "Unknown Product";

        if (product.ProductType == "Medicine" && !string.IsNullOrEmpty(product.MedicineId))
        {
            var medicine = await _context.Medicines.FindAsync(product.MedicineId);
            return medicine?.Name ?? "Unknown Medicine";
        }
        else if (!string.IsNullOrEmpty(product.GlossaryId))
        {
            var glossary = await _context.Glossaries.FindAsync(product.GlossaryId);
            return glossary?.Name ?? "Unknown Product";
        }

        return "Unknown Product";
    }

    [RequirePermission("finance:view_reports")]
    [HttpGet("most-selling-items")]
    public async Task<IActionResult> GetMostSellingItems(string period = "monthly", int? month = null, int? year = null, int? startYear = null, int? endYear = null)
    {
        _logger.LogInformation("Fetching most selling items - Period: {Period}, Month: {Month}, Year: {Year}, StartYear: {StartYear}, EndYear: {EndYear}",
            period, month, year, startYear, endYear);

        period = period?.ToLower() ?? "monthly";

        DateTime start;
        DateTime end;

        if (period == "monthly")
        {
            // Monthly view: Show items for a specific month
            if (!month.HasValue || !year.HasValue)
            {
                var now = DateTime.UtcNow;
                month = now.Month;
                year = now.Year;
            }

            start = new DateTime(year.Value, month.Value, 1);
            end = start.AddMonths(1);
        }
        else if (period == "yearly")
        {
            // Yearly view: Show items aggregated by year
            if (!startYear.HasValue || !endYear.HasValue)
            {
                var currentYear = DateTime.UtcNow.Year;
                startYear = currentYear - 4;
                endYear = currentYear;
            }

            if (startYear.Value > endYear.Value)
            {
                return BadRequest("Start year must be less than or equal to end year.");
            }

            start = new DateTime(startYear.Value, 1, 1);
            end = new DateTime(endYear.Value + 1, 1, 1);
        }
        else
        {
            return BadRequest("Period must be 'monthly' or 'yearly'.");
        }

        // Get sales that have payments in the date range
        var saleIdsWithPayments = await _context.Payments
            .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
            .Select(p => p.SalesId)
            .Distinct()
            .ToListAsync();

        var items = await (
            from si in _context.SalesItems
            join s in _context.Sales on si.SalesId equals s.SalesId
            join prod in _context.Products on si.ProductSku equals prod.ProductSku
            where saleIdsWithPayments.Contains(s.SalesId) &&
                  (s.SaleStatus == "Paid" || s.SaleStatus == "PartiallyPaid")
            group new { si, prod } by new { si.ProductSku } into g
            select new
            {
                ProductSku = g.Key.ProductSku,
                Quantity = g.Sum(x => x.si.Quantity),
                Revenue = g.Sum(x => x.si.SubTotal)
            }
        ).ToListAsync();

        var result = new List<MostSellingItemDto>();

        foreach (var i in items)
        {
            string name = await GetProductName(i.ProductSku);

            result.Add(new MostSellingItemDto
            {
                ProductName = name,
                TotalQuantitySold = i.Quantity,
                TotalRevenue = i.Revenue
            });
        }

        // Sort by revenue descending
        result = result.OrderByDescending(x => x.TotalRevenue).ToList();

        if (period == "monthly")
        {
            return Ok(new { Period = "monthly", Month = month!.Value, Year = year!.Value, Data = result });
        }
        else
        {
            return Ok(new { Period = "yearly", StartYear = startYear!.Value, EndYear = endYear!.Value, Data = result });
        }
    }

    [RequirePermission("finance:view_reports")]
    [HttpGet("summary")]
    public async Task<IActionResult> GetFinanceSummary(string period = "monthly", int? month = null, int? year = null, int? startYear = null, int? endYear = null)
    {
        _logger.LogInformation("Fetching finance summary - Period: {Period}, Month: {Month}, Year: {Year}, StartYear: {StartYear}, EndYear: {EndYear}",
            period, month, year, startYear, endYear);

        period = period?.ToLower() ?? "monthly";

        DateTime start;
        DateTime end;

        if (period == "monthly")
        {
            // Monthly view: Show summary for a specific month
            if (!month.HasValue || !year.HasValue)
            {
                var now = DateTime.UtcNow;
                month = now.Month;
                year = now.Year;
            }

            start = new DateTime(year.Value, month.Value, 1);
            end = start.AddMonths(1);
        }
        else if (period == "yearly")
        {
            // Yearly view: Show summary aggregated by year
            if (!startYear.HasValue || !endYear.HasValue)
            {
                var currentYear = DateTime.UtcNow.Year;
                startYear = currentYear - 4;
                endYear = currentYear;
            }

            if (startYear.Value > endYear.Value)
            {
                return BadRequest("Start year must be less than or equal to end year.");
            }

            start = new DateTime(startYear.Value, 1, 1);
            end = new DateTime(endYear.Value + 1, 1, 1);
        }
        else
        {
            return BadRequest("Period must be 'monthly' or 'yearly'.");
        }

        // Revenue (from Payments table)
        var revenue = await _context.Payments
            .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
            .SumAsync(p => (decimal?)p.PaymentAmount) ?? 0;

        // COGS
        var cogs = await (
            from sm in _context.StockMovements
            join stock in _context.Stocks on sm.Stock_Id equals stock.StockId
            where sm.Reason == "Sale" &&
                  sm.CreatedAt >= start &&
                  sm.CreatedAt < end
            select (decimal?)(sm.QuantityChanged * stock.CostPrice)
        ).SumAsync() ?? 0m;

        // Gross Profit
        decimal profit = revenue - cogs;

        // Pending Payments (always current, not filtered by period)
        int pending = await _context.Sales.CountAsync(s => s.SaleStatus == "Unpaid");

        // Purchase Payment Details (always current, not filtered by period)
        // Pending purchases
        var pendingPurchases = await _context.Purchases
            .Where(p => p.PaymentStatus == "Pending")
            .ToListAsync();

        int pendingPurchaseCount = pendingPurchases.Count;
        decimal pendingPurchaseAmount = pendingPurchases.Sum(p => p.TotalAmount);

        // Overdue purchases
        var overduePurchases = await _context.Purchases
            .Where(p => p.PaymentStatus == "Overdue")
            .ToListAsync();

        int overduePurchaseCount = overduePurchases.Count;
        decimal overduePurchaseAmount = overduePurchases.Sum(p => p.TotalAmount);

        // Total unpaid purchase count and amount
        int totalUnpaidPurchaseCount = pendingPurchaseCount + overduePurchaseCount;
        decimal totalUnpaidPurchaseAmount = pendingPurchaseAmount + overduePurchaseAmount;

        var result = new FinanceSummaryDto
        {
            TotalRevenue = revenue,
            CostOfSold = cogs,
            GrossProfit = profit,
            PendingPayments = pending,
            UnpaidPurchases = new UnpaidPurchasesDto
            {
                Pending = new PurchasePaymentStatusDto
                {
                    Count = pendingPurchaseCount,
                    Amount = pendingPurchaseAmount
                },
                Overdue = new PurchasePaymentStatusDto
                {
                    Count = overduePurchaseCount,
                    Amount = overduePurchaseAmount
                },
                Total = new PurchasePaymentStatusDto
                {
                    Count = totalUnpaidPurchaseCount,
                    Amount = totalUnpaidPurchaseAmount
                }
            }
        };

        if (period == "monthly")
        {
            return Ok(new { Period = "monthly", Month = month!.Value, Year = year!.Value, Summary = result });
        }
        else
        {
            return Ok(new { Period = "yearly", StartYear = startYear!.Value, EndYear = endYear!.Value, Summary = result });
        }
    }

    /*  COMPREHENSIVE FINANCE REPORTS - DAILY, WEEKLY, MONTHLY, YEARLY  */

    [RequirePermission("finance:view_reports")]
    [HttpGet("report/daily")]
    public async Task<IActionResult> GetDailyFinanceReport(DateOnly? date = null)
    {
        if (!date.HasValue)
        {
            date = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var start = date.Value.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);

        _logger.LogInformation("Generating daily finance report for date: {Date}", date.Value);

        var report = await GenerateFinanceReport("daily", start, end);
        return Ok(report);
    }

    [RequirePermission("finance:view_reports")]
    [HttpGet("report/weekly")]
    public async Task<IActionResult> GetWeeklyFinanceReport(DateOnly? startDate = null)
    {
        if (!startDate.HasValue)
        {
            // Default to start of current week (Monday)
            var today = DateTime.UtcNow;
            var daysUntilMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            startDate = DateOnly.FromDateTime(today.AddDays(-daysUntilMonday));
        }

        var start = startDate.Value.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(7);

        _logger.LogInformation("Generating weekly finance report from {StartDate} to {EndDate}", startDate.Value, DateOnly.FromDateTime(end));

        var report = await GenerateFinanceReport("weekly", start, end);
        return Ok(report);
    }

    [RequirePermission("finance:view_reports")]
    [HttpGet("report/monthly")]
    public async Task<IActionResult> GetMonthlyFinanceReport(int? month = null, int? year = null)
    {
        if (!month.HasValue || !year.HasValue)
        {
            var now = DateTime.UtcNow;
            month = now.Month;
            year = now.Year;
        }

        var start = new DateTime(year.Value, month.Value, 1);
        var end = start.AddMonths(1);

        _logger.LogInformation("Generating monthly finance report for {Month}/{Year}", month.Value, year.Value);

        var report = await GenerateFinanceReport("monthly", start, end);
        return Ok(report);
    }

    [RequirePermission("finance:view_reports")]
    [HttpGet("report/yearly")]
    public async Task<IActionResult> GetYearlyFinanceReport(int? year = null)
    {
        if (!year.HasValue)
        {
            year = DateTime.UtcNow.Year;
        }

        var start = new DateTime(year.Value, 1, 1);
        var end = start.AddYears(1);

        _logger.LogInformation("Generating yearly finance report for year {Year}", year.Value);

        var report = await GenerateFinanceReport("yearly", start, end);
        return Ok(report);
    }

    private async Task<FinanceReportDto> GenerateFinanceReport(string reportType, DateTime start, DateTime end)
    {
        var report = new FinanceReportDto
        {
            ReportType = reportType,
            StartDate = start,
            EndDate = end,
            GeneratedAt = DateTime.UtcNow
        };

        // ============================================
        // 1. REVENUE DETAILS
        // ============================================
        // Get sales that have payments in the date range
        var saleIdsWithPayments = await _context.Payments
            .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
            .Select(p => p.SalesId)
            .Distinct()
            .ToListAsync();

        var paidSales = await _context.Sales
            .Where(s => saleIdsWithPayments.Contains(s.SalesId) &&
                        (s.SaleStatus == "Paid" || s.SaleStatus == "PartiallyPaid"))
            .ToListAsync();

        var unpaidSales = await _context.Sales
            .Where(s => s.SaleStatus == "Unpaid" &&
                        s.Date >= DateOnly.FromDateTime(start) &&
                        s.Date < DateOnly.FromDateTime(end))
            .ToListAsync();

        // Calculate revenue from payments (not from sale amounts)
        decimal totalRevenue = await _context.Payments
            .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
            .SumAsync(p => (decimal?)p.PaymentAmount) ?? 0;
        decimal totalDiscounts = paidSales.Sum(s => (s.TotalAmount * (s.CustomerDiscountPercent ?? 0)) / 100m);
        decimal totalRoundingDiscounts = paidSales.Sum(s => s.RoundingDiscount ?? 0);
        decimal netRevenue = totalRevenue;

        report.RevenueDetails = new RevenueDetailsDto
        {
            TotalRevenue = totalRevenue,
            TotalDiscounts = totalDiscounts,
            TotalRoundingDiscounts = totalRoundingDiscounts,
            NetRevenue = netRevenue,
            PaidSalesCount = paidSales.Count,
            UnpaidSalesCount = unpaidSales.Count
        };

        // Daily breakdown for weekly/monthly/yearly reports
        if (reportType != "daily")
        {
            var dailyPayments = await _context.Payments
                .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
                .Join(_context.Sales, p => p.SalesId, s => s.SalesId, (p, s) => new { Payment = p, Sale = s })
                .GroupBy(x => x.Sale.Date)
                .Select(g => new DailyRevenueDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.Payment.PaymentAmount),
                    SalesCount = g.Select(x => x.Sale.SalesId).Distinct().Count()
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            report.RevenueDetails.DailyBreakdown = dailyPayments;
        }

        // ============================================
        // 2. COST OF GOODS SOLD (COGS)
        // ============================================
        var cogs = await (
            from sm in _context.StockMovements
            join stock in _context.Stocks on sm.Stock_Id equals stock.StockId
            where sm.Reason == "Sale" &&
                  sm.CreatedAt >= start &&
                  sm.CreatedAt < end
            select (decimal?)(sm.QuantityChanged * stock.CostPrice)
        ).SumAsync() ?? 0m;

        // ============================================
        // 3. PURCHASE DETAILS
        // ============================================
        var purchases = await _context.Purchases
            .Where(p => p.InvoiceDate >= DateOnly.FromDateTime(start) &&
                        p.InvoiceDate < DateOnly.FromDateTime(end))
            .ToListAsync();

        var paidPurchases = purchases.Where(p => p.PaymentStatus == "Complete").ToList();
        var unpaidPurchasesInPeriod = purchases.Where(p => p.PaymentStatus != "Complete").ToList();

        decimal totalPurchaseAmount = purchases.Sum(p => p.TotalAmount);
        decimal paidPurchasesAmount = paidPurchases.Sum(p => p.TotalAmount);
        decimal unpaidPurchasesAmount = unpaidPurchasesInPeriod.Sum(p => p.TotalAmount);

        report.PurchaseDetails = new PurchaseDetailsDto
        {
            TotalPurchaseAmount = totalPurchaseAmount,
            TotalPurchaseCount = purchases.Count,
            PaidPurchasesAmount = paidPurchasesAmount,
            PaidPurchasesCount = paidPurchases.Count,
            UnpaidPurchasesAmount = unpaidPurchasesAmount,
            UnpaidPurchasesCount = unpaidPurchasesInPeriod.Count
        };

        // Daily breakdown for weekly/monthly/yearly reports
        if (reportType != "daily")
        {
            var dailyPurchases = purchases
                .GroupBy(p => p.InvoiceDate)
                .Select(g => new DailyPurchaseDto
                {
                    Date = g.Key,
                    Amount = g.Sum(p => p.TotalAmount),
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            report.PurchaseDetails.DailyBreakdown = dailyPurchases;
        }

        // ============================================
        // 4. FINANCIAL SUMMARY
        // ============================================
        decimal grossProfit = totalRevenue - cogs;
        decimal grossProfitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100m : 0;
        decimal netProfit = grossProfit - totalPurchaseAmount;

        report.FinancialSummary = new FinancialSummaryDto
        {
            TotalRevenue = totalRevenue,
            TotalCostOfGoodsSold = cogs,
            GrossProfit = grossProfit,
            GrossProfitMargin = Math.Round(grossProfitMargin, 2),
            TotalPurchaseExpenses = totalPurchaseAmount,
            NetProfit = netProfit,
            TotalSalesCount = paidSales.Count,
            TotalPurchaseCount = purchases.Count
        };

        // ============================================
        // 5. TOP SELLING ITEMS
        // ============================================
        // Get sales that have payments in the date range
        var saleIdsForTopItems = await _context.Payments
            .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
            .Select(p => p.SalesId)
            .Distinct()
            .ToListAsync();

        var topSellingItems = await (
            from si in _context.SalesItems
            join s in _context.Sales on si.SalesId equals s.SalesId
            where saleIdsForTopItems.Contains(s.SalesId) &&
                  (s.SaleStatus == "Paid" || s.SaleStatus == "PartiallyPaid")
            group new { si } by new { si.ProductSku } into g
            select new
            {
                ProductSku = g.Key.ProductSku,
                Quantity = g.Sum(x => x.si.Quantity),
                Revenue = g.Sum(x => x.si.SubTotal)
            }
        ).OrderByDescending(x => x.Revenue)
        .Take(20)
        .ToListAsync();

        var topSellingItemsList = new List<MostSellingItemDto>();
        foreach (var item in topSellingItems)
        {
            string name = await GetProductName(item.ProductSku);
            topSellingItemsList.Add(new MostSellingItemDto
            {
                ProductName = name,
                TotalQuantitySold = item.Quantity,
                TotalRevenue = item.Revenue
            });
        }

        report.TopSellingItems = topSellingItemsList;

        // ============================================
        // 6. SUPPLIER EXPENSES
        // ============================================
        var supplierExpenses = await (
            from p in _context.Purchases
            join s in _context.Suppliers on p.SupplierId equals s.SupplierId
            where p.InvoiceDate >= DateOnly.FromDateTime(start) &&
                  p.InvoiceDate < DateOnly.FromDateTime(end)
            group p by new { s.SupplierName } into g
            select new SupplierExpenseDto
            {
                SupplierName = g.Key.SupplierName,
                TotalExpense = g.Sum(x => x.TotalAmount),
                InvoiceCount = g.Count()
            }
        ).OrderByDescending(x => x.TotalExpense)
        .ToListAsync();

        report.SupplierExpenses = supplierExpenses;

        // ============================================
        // 7. PAYMENT METHOD BREAKDOWN (from Payments table)
        // ============================================
        var paymentsInPeriod = await _context.Payments
            .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
            .ToListAsync();

        var paymentBreakdown = new PaymentMethodBreakdownDto();
        var paymentGroups = paymentsInPeriod
            .GroupBy(p => p.PaymentMethod)
            .ToList();

        foreach (var group in paymentGroups)
        {
            var method = group.Key?.ToLower() ?? "";
            decimal amount = group.Sum(p => p.PaymentAmount);
            int count = group.Count();

            switch (method)
            {
                case "cash":
                    paymentBreakdown.CashAmount = amount;
                    paymentBreakdown.CashCount = count;
                    break;
                case "card":
                    paymentBreakdown.CardAmount = amount;
                    paymentBreakdown.CardCount = count;
                    break;
                case "bank":
                    paymentBreakdown.BankAmount = amount;
                    paymentBreakdown.BankCount = count;
                    break;
                case "paylater":
                    paymentBreakdown.PayLaterAmount = amount;
                    paymentBreakdown.PayLaterCount = count;
                    break;
                default:
                    paymentBreakdown.OtherAmount += amount;
                    paymentBreakdown.OtherCount += count;
                    break;
            }
        }

        report.PaymentMethodBreakdown = paymentBreakdown;

        // ============================================
        // 8. UNPAID PURCHASES (Current Status - Not filtered by period)
        // ============================================
        var pendingPurchases = await _context.Purchases
            .Where(p => p.PaymentStatus == "Pending")
            .ToListAsync();

        var overduePurchases = await _context.Purchases
            .Where(p => p.PaymentStatus == "Overdue")
            .ToListAsync();

        int pendingCount = pendingPurchases.Count;
        decimal pendingAmount = pendingPurchases.Sum(p => p.TotalAmount);
        int overdueCount = overduePurchases.Count;
        decimal overdueAmount = overduePurchases.Sum(p => p.TotalAmount);

        report.UnpaidPurchases = new UnpaidPurchasesDto
        {
            Pending = new PurchasePaymentStatusDto
            {
                Count = pendingCount,
                Amount = pendingAmount
            },
            Overdue = new PurchasePaymentStatusDto
            {
                Count = overdueCount,
                Amount = overdueAmount
            },
            Total = new PurchasePaymentStatusDto
            {
                Count = pendingCount + overdueCount,
                Amount = pendingAmount + overdueAmount
            }
        };

        return report;
    }

}