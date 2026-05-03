using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.Models;
using pharmacyPOS.API.Utilities;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Authorization;

[Route("api/[controller]")]
[ApiController]
public class SalesController : ControllerBase
{
    private readonly SethuwaPharmacyDbContext _context;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        SethuwaPharmacyDbContext context,
        ILogger<SalesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [RequirePermission("sales:create_receipt")]
    [HttpPost("create-receipt-with-items")]

    public async Task<IActionResult> CreateReceiptWithItems(CreateReceiptWithItemsDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var employeeId = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
        if (string.IsNullOrEmpty(employeeId))
            return Unauthorized("Unable to resolve employee identity.");

        var receiptNumber = await ReceiptNumberGenerator.GenerateAsync(_context);
        var now = DateTime.UtcNow;

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1️⃣ Create sale header (not saved yet!)
            var sale = new Sale
            {
                ReceiptNumber = receiptNumber,
                Date = DateOnly.FromDateTime(now),
                Time = TimeOnly.FromDateTime(now),
                SaleStatus = "Draft",
                IssuedById = employeeId,
                TotalAmount = dto.TotalAmount,
                FinalAmountDue = 0
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync(); // get sale.SalesId

            // 2️⃣ Stock Deduction + Create SaleItems (ONE LOOP)
            foreach (var item in dto.Items)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductSku == item.ProductSku && !p.IsDeleted);

                if (product == null)
                    return BadRequest($"Invalid product SKU: {item.ProductSku}");

                long? selectedStockId = null;

                if (item.StockId.HasValue)
                {
                    // Cashier selected a specific batch — deduct from it directly
                    bool ok = await StockDeduction.DeductFromBatch(_context, item.StockId.Value, item.ProductSku, item.Quantity, sale.SalesId);
                    if (!ok)
                        return BadRequest($"Insufficient stock in selected batch for product: {item.ProductSku}");
                    selectedStockId = item.StockId.Value;
                }
                else
                {
                    // No batch selected — fall back to FEFO auto-deduction
                    bool ok = await StockDeduction.DeductUsingFEFO(_context, item.ProductSku, item.Quantity, sale.SalesId);
                    if (!ok)
                        return BadRequest($"Insufficient stock for product: {item.ProductSku}");
                }

                // Create sale item recording which batch was used
                var saleItem = new SalesItem
                {
                    SalesId = sale.SalesId,
                    ProductSku = item.ProductSku,
                    Quantity = item.Quantity,
                    SellingPrice = item.SubTotal / item.Quantity,
                    SubTotal = item.SubTotal,
                    StockId = selectedStockId
                };

                _context.SalesItems.Add(saleItem);
            }

            // 3️⃣ Commit all changes
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                Message = "Receipt created successfully",
                SalesId = sale.SalesId,
                ReceiptNumber = sale.ReceiptNumber
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ex.Message);
        }
    }


    [RequirePermission("sales:view_receipt")]
    [HttpGet("receipt/{receiptNumber}")]
    public async Task<IActionResult> GetReceiptForCashier(string receiptNumber)
    {
        // Load the sale header
        var sale = await _context.Sales
            .Include(s => s.IssuedBy)
            .Include(s => s.SalesItems)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.ReceiptNumber == receiptNumber);

        if (sale == null)
            return NotFound("Receipt not found.");

        if (sale.SaleStatus != "Draft")
            return BadRequest("This receipt is not available for payment.");

        // Prepare summary DTO
        var summary = new SaleSummaryDto
        {
            SalesId = sale.SalesId,
            ReceiptNumber = sale.ReceiptNumber!,
            IssuedBy = sale.IssuedBy.EmployeeName,
            Date = sale.Date,
            Time = sale.Time,
            Total = sale.TotalAmount,
            FinalAmountDue = sale.FinalAmountDue,
            Payments = sale.Payments.Select(p => new PaymentDto
            {
                PaymentId = p.PaymentId,
                SalesId = p.SalesId,
                PaymentMethod = p.PaymentMethod,
                PaymentAmount = p.PaymentAmount,
                PaymentDate = p.PaymentDate,
                CreatedAt = p.CreatedAt
            }).ToList(),
            TotalPaidAmount = sale.Payments.Sum(p => p.PaymentAmount)
        };

        // Load product names from Product table
        foreach (var item in sale.SalesItems)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductSku == item.ProductSku);

            string productName = "Unknown";

            if (product != null)
            {
                if (product.ProductType == "Medicine" && product.MedicineId != null)
                {
                    var med = await _context.Medicines
                        .FirstOrDefaultAsync(m => m.MedicineId == product.MedicineId);

                    productName = med?.Name ?? "Unknown Medicine";
                }
                else if (product.ProductType == "Glossary" && product.GlossaryId != null)
                {
                    var glo = await _context.Glossaries
                        .FirstOrDefaultAsync(g => g.GlossaryId == product.GlossaryId);

                    productName = glo?.Name ?? "Unknown Glossary Item";
                }
            }

            summary.Items.Add(new SaleItemSummaryDto
            {
                ProductName = productName,
                ProductSku = item.ProductSku,
                Quantity = item.Quantity,
                Price = item.SellingPrice,
                SubTotal = item.SubTotal
            });
        }

        return Ok(summary);
    }


    [RequirePermission("sales:finalize_sale")]
    [HttpPut("finalize/{receiptNumber}")]
    public async Task<IActionResult> FinalizeSale(string receiptNumber, FinalizeSaleDto dto)
    {
        _logger.LogInformation("Finalizing sale with receipt number: {ReceiptNumber}", receiptNumber);

        // Use a transaction to ensure atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Reload the sale within the transaction to get the latest state
            // Use AsNoTracking first to check status without tracking
            var saleCheck = await _context.Sales
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ReceiptNumber == receiptNumber);

            if (saleCheck == null)
            {
                _logger.LogWarning("Receipt not found: {ReceiptNumber}", receiptNumber);
                await transaction.RollbackAsync();
                return NotFound("Receipt not found.");
            }

            // CRITICAL: Check status BEFORE loading with tracking or making any modifications
            // if (saleCheck.SaleStatus != "Draft")
            // {
            //     _logger.LogWarning("Attempt to finalize non-draft receipt: {ReceiptNumber}, Status: {Status}", receiptNumber, saleCheck.SaleStatus);
            //     await transaction.RollbackAsync();
            //     return BadRequest("This receipt is not available for payment. Only draft receipts can be finalized.");
            // }

            // Now load with tracking for modifications
            var sale = await _context.Sales
                .Include(s => s.SalesItems)
                .FirstOrDefaultAsync(s => s.ReceiptNumber == receiptNumber);

            // Double-check status after loading with tracking (defense against race conditions)
            // if (sale == null || sale.SaleStatus != "Draft")
            // {
            //     _logger.LogWarning("Sale status changed or not found after reload: {ReceiptNumber}, Status: {Status}",
            //         receiptNumber, sale?.SaleStatus ?? "null");
            //     await transaction.RollbackAsync();
            //     return BadRequest("This receipt is not available for payment. Only draft receipts can be finalized.");
            // }

            // Ensure sale is not null before proceeding
            if (sale == null)
            {
                _logger.LogWarning("Sale not found after reload: {ReceiptNumber}", receiptNumber);
                await transaction.RollbackAsync();
                return NotFound("Receipt not found.");
            }

            // --------------------------
            // SET CASHIER (BILLED BY) - Logged in user
            // --------------------------
            var cashierId = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
            if (string.IsNullOrEmpty(cashierId))
            {
                _logger.LogWarning("Unable to resolve cashier identity for receipt: {ReceiptNumber}", receiptNumber);
                await transaction.RollbackAsync();
                return Unauthorized("Unable to resolve cashier identity.");
            }
            sale.BilledById = cashierId;

            // --------------------------
            // CUSTOMER VALIDATION RULE
            // --------------------------
            if (dto.PaymentMethod == "PayLater")
            {
                if (string.IsNullOrWhiteSpace(dto.CustomerName) ||
                    string.IsNullOrWhiteSpace(dto.ContactNumber))
                {
                    _logger.LogWarning("PayLater payment missing customer info for receipt: {ReceiptNumber}", receiptNumber);
                    await transaction.RollbackAsync();
                    return BadRequest("Customer name and contact number are required for Pay Later.");
                }
            }

            // Attach customer if selected from system
            sale.CustomerId = dto.CustomerId;

            // --------------------------
            // DISCOUNT + ROUNDING
            // --------------------------
            decimal discountValue = (sale.TotalAmount * dto.CustomerDiscountPercent) / 100m;
            decimal newTotal = sale.TotalAmount - discountValue - dto.RoundingDiscount;

            if (newTotal < 0)
            {
                _logger.LogWarning("Final amount is negative for receipt: {ReceiptNumber}, Amount: {Amount}", receiptNumber, newTotal);
                await transaction.RollbackAsync();
                return BadRequest("Final amount cannot be negative.");
            }

            sale.CustomerDiscountPercent = dto.CustomerDiscountPercent;
            sale.RoundingDiscount = dto.RoundingDiscount;
            sale.FinalAmountDue = newTotal;

            // --------------------------
            // PAYMENT TYPES - Create Payment records
            // --------------------------
            decimal totalPaymentAmount = 0;
            var now = DateTime.UtcNow;

            if (dto.PaymentMethod != "PayLater")
            {
                // Handle payments - support both single and multiple payments
                List<CreatePaymentDto> paymentsToProcess = new List<CreatePaymentDto>();

                if (dto.Payments != null && dto.Payments.Count > 0)
                {
                    // Use multiple payments if provided
                    paymentsToProcess = dto.Payments;
                }
                else
                {
                    // Backward compatibility: single payment
                    paymentsToProcess.Add(new CreatePaymentDto
                    {
                        PaymentMethod = dto.PaymentMethod,
                        PaymentAmount = dto.ReceivedAmount,
                        PaymentDate = now
                    });
                }

                // Create payment records
                foreach (var paymentDto in paymentsToProcess)
                {
                    totalPaymentAmount += paymentDto.PaymentAmount;

                    var payment = new Payment
                    {
                        SalesId = sale.SalesId,
                        PaymentMethod = paymentDto.PaymentMethod,
                        PaymentAmount = paymentDto.PaymentAmount,
                        PaymentDate = paymentDto.PaymentDate ?? now,
                        CreatedAt = now
                    };

                    _context.Payments.Add(payment);
                }

                // Validate total payment amount
                if (totalPaymentAmount < newTotal)
                {
                    _logger.LogWarning("Total payment amount less than amount due for receipt: {ReceiptNumber}, Received: {Received}, Due: {Due}",
                        receiptNumber, totalPaymentAmount, newTotal);
                    await transaction.RollbackAsync();
                    return BadRequest("Total payment amount is less than amount due.");
                }

                // Update sale status and legacy fields (for backward compatibility)
                sale.PaymentMethod = paymentsToProcess.First().PaymentMethod; // Use first payment method
                sale.SaleStatus = "Paid";
                sale.PaymentCompletedAt = now;
            }
            else
            {
                // Unpaid sale (PayLater)
                sale.PaymentMethod = "PayLater";
                sale.SaleStatus = "Unpaid";
                sale.PaymentCompletedAt = null;
            }

            // Final status check before saving (defense against race conditions)
            // Query database directly to check current status without affecting tracked entity
            var currentStatus = await _context.Sales
                .AsNoTracking()
                .Where(s => s.ReceiptNumber == receiptNumber)
                .Select(s => s.SaleStatus)
                .FirstOrDefaultAsync();

            // if (currentStatus != "Draft")
            // {
            //     _logger.LogWarning("Sale status changed during finalization: {ReceiptNumber}, Current Status: {Status}",
            //         receiptNumber, currentStatus);
            //     await transaction.RollbackAsync();
            //     return BadRequest("This receipt is not available for payment. Only draft receipts can be finalized.");
            // }

            // Save changes within the transaction
            await _context.SaveChangesAsync();

            // Commit the transaction
            await transaction.CommitAsync();

            _logger.LogInformation("Sale finalized successfully for receipt: {ReceiptNumber}, Status: {Status}",
                receiptNumber, sale.SaleStatus);

            // Calculate payment summary for response
            decimal remainingAmount = sale.FinalAmountDue - totalPaymentAmount;
            decimal change = totalPaymentAmount > sale.FinalAmountDue ? totalPaymentAmount - sale.FinalAmountDue : 0;

            return Ok(new
            {
                Message = "Sale finalized successfully",
                Receipt = sale.ReceiptNumber,
                Status = sale.SaleStatus,
                FinalAmount = sale.FinalAmountDue,
                TotalPaid = totalPaymentAmount,
                RemainingAmount = remainingAmount,
                Change = change
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while finalizing sale for receipt: {ReceiptNumber}", receiptNumber);
            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Error occurred while rolling back transaction for receipt: {ReceiptNumber}", receiptNumber);
            }
            return StatusCode(500, "An error occurred while finalizing the sale.");
        }
    }

    [RequirePermission("sales:complete_paylater")]
    [HttpPut("paylater/complete/{receiptNumber}")]
    public async Task<IActionResult> CompletePayLaterPayment(string receiptNumber, CompletePayLaterPaymentDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var sale = await _context.Sales
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.ReceiptNumber == receiptNumber);

            if (sale == null)
            {
                await transaction.RollbackAsync();
                return NotFound("Receipt not found.");
            }

            if (sale.SaleStatus != "Unpaid" && sale.SaleStatus != "PartiallyPaid")
            {
                await transaction.RollbackAsync();
                return BadRequest("Only unpaid or partially paid (PayLater) receipts can receive payments.");
            }

            if (dto.PaymentMethod == "PayLater")
            {
                await transaction.RollbackAsync();
                return BadRequest("Cannot use PayLater as completion payment method.");
            }

            // -----------------------
            // APPLY ROUNDING DISCOUNT
            // -----------------------
            decimal newAmountDue = sale.FinalAmountDue - dto.RoundingDiscount;

            if (newAmountDue < 0)
            {
                await transaction.RollbackAsync();
                return BadRequest("Final amount cannot be negative.");
            }

            sale.RoundingDiscount += dto.RoundingDiscount; // Add to previous if needed
            sale.FinalAmountDue = newAmountDue;

            // -----------------------
            // CALCULATE TOTAL ALREADY PAID
            // -----------------------
            decimal totalPaidSoFar = sale.Payments.Sum(p => p.PaymentAmount);
            decimal remainingAmount = newAmountDue - totalPaidSoFar;

            // -----------------------
            // PAYMENT VALIDATION (allow partial payments)
            // -----------------------
            if (dto.ReceivedAmount <= 0)
            {
                await transaction.RollbackAsync();
                return BadRequest("Payment amount must be greater than zero.");
            }

            if (dto.ReceivedAmount > remainingAmount)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Payment amount exceeds remaining amount due. Remaining: {remainingAmount}, Payment: {dto.ReceivedAmount}");
            }

            // -----------------------
            // CREATE PAYMENT RECORD
            // -----------------------
            var now = DateTime.UtcNow;
            var payment = new Payment
            {
                SalesId = sale.SalesId,
                PaymentMethod = dto.PaymentMethod,
                PaymentAmount = dto.ReceivedAmount,
                PaymentDate = now,
                CreatedAt = now
            };

            _context.Payments.Add(payment);

            // -----------------------
            // UPDATE SALE STATUS BASED ON TOTAL PAYMENTS
            // -----------------------
            decimal newTotalPaid = totalPaidSoFar + dto.ReceivedAmount;

            if (newTotalPaid >= newAmountDue)
            {
                sale.SaleStatus = "Paid";
                sale.PaymentMethod = dto.PaymentMethod; // Update to latest payment method
                sale.PaymentCompletedAt = now;
            }
            else
            {
                sale.SaleStatus = "PartiallyPaid";
                sale.PaymentMethod = "PayLater"; // Keep as PayLater until fully paid
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            decimal change = dto.ReceivedAmount > remainingAmount ? dto.ReceivedAmount - remainingAmount : 0;

            return Ok(new
            {
                Message = "Payment recorded successfully",
                Receipt = sale.ReceiptNumber,
                PaymentStatus = sale.SaleStatus,
                AmountPaid = dto.ReceivedAmount,
                TotalPaid = newTotalPaid,
                RemainingAmount = newAmountDue - newTotalPaid,
                RoundingDiscount = dto.RoundingDiscount,
                FinalAmount = newAmountDue,
                Change = change
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing PayLater payment for receipt: {ReceiptNumber}", receiptNumber);
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while completing the payment.");
        }
    }

    [RequirePermission("sales:view_paylater_list")]
    [HttpGet("paylater/list")]
    public async Task<IActionResult> GetAllUnpaidSales()
    {
        var unpaidSales = await _context.Sales
            .Include(s => s.IssuedBy)
            .Include(s => s.Customer)
            .Include(s => s.Payments)
            .Where(s => s.SaleStatus == "Unpaid" || s.SaleStatus == "PartiallyPaid")
            .OrderBy(s => s.Date)
            .ToListAsync();

        var response = unpaidSales.Select(s => new PayLaterListItemDto
        {
            ReceiptNumber = s.ReceiptNumber!,
            CustomerName = s.Customer?.CustomerName ?? "Walk-in Customer",
            ContactNumber = s.Customer?.ContactNumber,
            FinalAmountDue = s.FinalAmountDue,
            Date = s.Date,
            IssuedBy = s.IssuedBy.EmployeeName,
            DaysOutstanding = (DateTime.UtcNow - s.Date.ToDateTime(TimeOnly.MinValue)).Days
        });

        return Ok(response);
    }

    [RequirePermission("sales:view_paylater_receipt")]
    [HttpGet("paylater/{receiptNumber}")]
    public async Task<IActionResult> GetPayLaterReceipt(string receiptNumber)
    {
        var sale = await _context.Sales
            .Include(s => s.SalesItems)
            .Include(s => s.IssuedBy)
            .Include(s => s.Customer)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.ReceiptNumber == receiptNumber);

        if (sale == null)
            return NotFound("Receipt not found.");

        if (sale.SaleStatus != "Unpaid")
            return BadRequest("This receipt is not marked as PayLater.");

        var Summary = new SaleSummaryDto
        {
            SalesId = sale.SalesId,
            ReceiptNumber = sale.ReceiptNumber!,
            IssuedBy = sale.IssuedBy.EmployeeName,
            Date = sale.Date,
            Time = sale.Time,
            Total = sale.TotalAmount,
            FinalAmountDue = sale.FinalAmountDue,
            Payments = sale.Payments.Select(p => new PaymentDto
            {
                PaymentId = p.PaymentId,
                SalesId = p.SalesId,
                PaymentMethod = p.PaymentMethod,
                PaymentAmount = p.PaymentAmount,
                PaymentDate = p.PaymentDate,
                CreatedAt = p.CreatedAt
            }).ToList(),
            TotalPaidAmount = sale.Payments.Sum(p => p.PaymentAmount)
        };

        // Load items (like cashier API)
        foreach (var item in sale.SalesItems)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductSku == item.ProductSku);

            string productName = "Unknown";

            if (product != null)
            {
                if (product.ProductType == "Medicine" && product.MedicineId != null)
                {
                    var med = await _context.Medicines.FirstOrDefaultAsync(m => m.MedicineId == product.MedicineId);
                    productName = med?.Name ?? "Unknown Medicine";
                }
                else if (product.ProductType == "Glossary" && product.GlossaryId != null)
                {
                    var glo = await _context.Glossaries.FirstOrDefaultAsync(g => g.GlossaryId == product.GlossaryId);
                    productName = glo?.Name ?? "Unknown Glossary";
                }
            }

            Summary.Items.Add(new SaleItemSummaryDto
            {
                ProductName = productName,
                ProductSku = item.ProductSku,
                Quantity = item.Quantity,
                Price = item.SellingPrice,
                SubTotal = item.SubTotal
            });
        }

        return Ok(Summary);
    }

    /*  CANCEL DRAFT RECEIPT AND RESTORE STOCK  */

    [RequirePermission("sales:cancel_receipt")]
    [HttpDelete("cancel/{receiptNumber}")]
    public async Task<IActionResult> CancelDraftReceipt(string receiptNumber)
    {
        var sale = await _context.Sales
            .Include(s => s.SalesItems)
            .FirstOrDefaultAsync(s => s.ReceiptNumber == receiptNumber);

        if (sale == null)
            return NotFound("Receipt not found.");

        if (sale.SaleStatus != "Draft")
            return BadRequest("Only draft receipts can be cancelled.");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1️⃣ Restore stock from StockMovement log
            var movements = await _context.StockMovements
                .Where(m => m.SalesId == sale.SalesId && m.Reason == "Sale")
                .ToListAsync();

            foreach (var m in movements)
            {
                var batch = await _context.Stocks.FirstOrDefaultAsync(s => s.StockId == m.Stock_Id);
                if (batch != null)
                {
                    batch.QuantityOnHand += (-m.QuantityChanged);
                }

                // Add reverse entry
                _context.StockMovements.Add(new StockMovement
                {
                    ProductSku = m.ProductSku,
                    Stock_Id = m.Stock_Id,
                    QuantityChanged = -m.QuantityChanged, // reverse
                    Reason = "SaleCancellation",
                    SalesId = sale.SalesId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 2️⃣ Mark sale as cancelled
            sale.SaleStatus = "Cancelled";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Draft receipt cancelled & stock restored successfully." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ex.Message);
        }
    }

    [RequirePermission("sales:view_summary")]
    [HttpGet("summary/today")]
    public async Task<IActionResult> GetTodaySalesSummary()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly yesterday = today.AddDays(-1);

        // -----------------------------
        // 1️⃣ Today's Paid Sales Only
        // -----------------------------
        var todaySales = await _context.Sales
            .Include(s => s.Payments)
            .Where(s => (s.SaleStatus == "Paid" || s.SaleStatus == "PartiallyPaid") && s.Date == today)
            .ToListAsync();

        decimal totalToday = todaySales.Sum(s => s.Payments.Sum(p => p.PaymentAmount));
        int receiptCount = todaySales.Count;

        // -----------------------------
        // 2️⃣ Yesterday's Paid Sales
        // -----------------------------
        var yesterdaySales = await _context.Sales
            .Include(s => s.Payments)
            .Where(s => (s.SaleStatus == "Paid" || s.SaleStatus == "PartiallyPaid") && s.Date == yesterday)
            .ToListAsync();

        decimal yesterdayTotal = yesterdaySales.Sum(s => s.Payments.Sum(p => p.PaymentAmount));

        // -----------------------------
        // 3️⃣ Calculate % Change
        // -----------------------------
        decimal percentageChange = 0;

        if (yesterdayTotal > 0)
        {
            percentageChange = ((totalToday - yesterdayTotal) / yesterdayTotal) * 100m;
        }

        var result = new TodaySalesSummaryDto
        {
            TotalSalesToday = totalToday,
            TotalReceiptsToday = receiptCount,
            PercentageChange = Math.Round(percentageChange, 2)
        };

        return Ok(result);
    }


    /*  GET MONTHLY REVENUE SUMMARY  */
    [RequirePermission("sales:view_summary")]
    [HttpGet("summary/month")]
    public async Task<IActionResult> GetMonthlyRevenueSummary()
    {
        var now = DateTime.UtcNow;

        // -------------------------------
        // Current Month range
        // -------------------------------
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        // -------------------------------
        // Last Month range
        // -------------------------------
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth;

        // -------------------------------
        // 1️⃣ Revenue for this month
        // -------------------------------
        var thisMonthSales = await _context.Sales
            .Where(s =>
                s.SaleStatus == "Paid" &&
                s.PaymentCompletedAt >= startOfMonth &&
                s.PaymentCompletedAt < endOfMonth
            )
            .ToListAsync();

        decimal totalThisMonth = thisMonthSales.Sum(s => s.FinalAmountDue);
        int totalReceipts = thisMonthSales.Count;

        // -------------------------------
        // 2️⃣ Revenue for last month
        // -------------------------------
        var lastMonthSales = await _context.Sales
            .Include(s => s.Payments)
            .Where(s => s.SaleStatus == "Paid" || s.SaleStatus == "PartiallyPaid")
            .ToListAsync();

        decimal lastMonthRevenue = lastMonthSales
            .SelectMany(s => s.Payments.Where(p => p.PaymentDate >= startOfLastMonth && p.PaymentDate < endOfLastMonth))
            .Sum(p => p.PaymentAmount);

        // -------------------------------
        // 3️⃣ Calculate Percentage Growth
        // -------------------------------
        decimal growth = 0;

        if (lastMonthRevenue > 0)
            growth = ((totalThisMonth - lastMonthRevenue) / lastMonthRevenue) * 100m;

        var result = new MonthlyRevenueDto
        {
            TotalRevenue = totalThisMonth,
            TotalPaidReceipts = totalReceipts,
            PercentageGrowth = Math.Round(growth, 2)
        };

        return Ok(result);
    }

    // GET: api/Sales?page=1&pageSize=10&sortBy=date&sortDirection=desc&receiptNumber=&saleStatus=&paymentMethod=&fromDate=&toDate=&customerId=&customerName=&issuedById=&issuedByName=&minAmount=&maxAmount=
    [RequirePermission("sales:view_list")]
    [HttpGet]
    public async Task<IActionResult> GetAllSales(
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        string? sortDirection = null,
        string? receiptNumber = null,
        string? saleStatus = null,
        string? paymentMethod = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? customerId = null,
        string? customerName = null,
        string? issuedById = null,
        string? issuedByName = null,
        decimal? minAmount = null,
        decimal? maxAmount = null)
    {
        // Pagination validation
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        _logger.LogInformation($"Fetching sales with filters - page: {page}, pageSize: {pageSize}, sortBy: {sortBy}, sortDirection: {sortDirection}");

        // Base query with includes
        var query = _context.Sales
            .Include(s => s.IssuedBy)
            .Include(s => s.Customer)
            .Include(s => s.SalesItems)
            .Include(s => s.Payments)
            .AsNoTracking()
            .AsQueryable();

        // ============================================
        // FILTERING
        // ============================================

        // Filter by ReceiptNumber (Contains search)
        if (!string.IsNullOrWhiteSpace(receiptNumber))
        {
            receiptNumber = receiptNumber.Trim();
            query = query.Where(s => s.ReceiptNumber != null && s.ReceiptNumber.Contains(receiptNumber));
        }

        // Filter by SaleStatus
        if (!string.IsNullOrWhiteSpace(saleStatus))
        {
            saleStatus = saleStatus.Trim();
            query = query.Where(s => s.SaleStatus == saleStatus);
        }

        // Filter by PaymentMethod
        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            paymentMethod = paymentMethod.Trim();
            query = query.Where(s => s.PaymentMethod != null && s.PaymentMethod == paymentMethod);
        }

        // Filter by Date Range
        if (fromDate.HasValue)
        {
            query = query.Where(s => s.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.Date <= toDate.Value);
        }

        // Filter by CustomerId
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            customerId = customerId.Trim();
            query = query.Where(s => s.CustomerId != null && s.CustomerId == customerId);
        }

        // Filter by CustomerName (Contains search)
        if (!string.IsNullOrWhiteSpace(customerName))
        {
            customerName = customerName.Trim();
            query = query.Where(s => s.Customer != null && s.Customer.CustomerName.Contains(customerName));
        }

        // Filter by IssuedById
        if (!string.IsNullOrWhiteSpace(issuedById))
        {
            issuedById = issuedById.Trim();
            query = query.Where(s => s.IssuedById == issuedById);
        }

        // Filter by IssuedByName (Contains search)
        if (!string.IsNullOrWhiteSpace(issuedByName))
        {
            issuedByName = issuedByName.Trim();
            query = query.Where(s => s.IssuedBy.EmployeeName.Contains(issuedByName));
        }

        // Filter by Amount Range
        if (minAmount.HasValue)
        {
            query = query.Where(s => s.FinalAmountDue >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            query = query.Where(s => s.FinalAmountDue <= maxAmount.Value);
        }

        // ============================================
        // SORTING
        // ============================================

        // Validate sortDirection
        bool isAscending = !string.IsNullOrWhiteSpace(sortDirection) &&
                          sortDirection.Trim().ToLower() == "asc";

        // Apply sorting based on sortBy parameter
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            sortBy = sortBy.Trim().ToLower();

            query = sortBy switch
            {
                "id" => isAscending
                    ? query.OrderBy(s => s.SalesId)
                    : query.OrderByDescending(s => s.SalesId),

                "date" => isAscending
                    ? query.OrderBy(s => s.Date).ThenBy(s => s.Time)
                    : query.OrderByDescending(s => s.Date).ThenByDescending(s => s.Time),

                "amount" => isAscending
                    ? query.OrderBy(s => s.FinalAmountDue)
                    : query.OrderByDescending(s => s.FinalAmountDue),

                "status" => isAscending
                    ? query.OrderBy(s => s.SaleStatus)
                    : query.OrderByDescending(s => s.SaleStatus),

                "receiptnumber" => isAscending
                    ? query.OrderBy(s => s.ReceiptNumber ?? string.Empty)
                    : query.OrderByDescending(s => s.ReceiptNumber ?? string.Empty),

                _ => query.OrderByDescending(s => s.Date).ThenByDescending(s => s.Time) // Default: date desc
            };
        }
        else
        {
            // Default sorting: date desc, then time desc
            query = query.OrderByDescending(s => s.Date).ThenByDescending(s => s.Time);
        }

        // ============================================
        // COUNT TOTAL ITEMS (before pagination)
        // ============================================
        var totalItems = await query.CountAsync();

        // ============================================
        // PAGINATION
        // ============================================
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var sales = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ============================================
        // LOAD ALL PRODUCTS AND RELATED DATA EFFICIENTLY
        // ============================================
        var allProductSkus = sales
            .SelectMany(s => s.SalesItems)
            .Select(si => si.ProductSku)
            .Distinct()
            .ToList();

        var allProducts = await _context.Products
            .Where(p => allProductSkus.Contains(p.ProductSku))
            .AsNoTracking()
            .ToListAsync();

        // Create lookup dictionaries for fast access
        var productLookup = allProducts.ToDictionary(p => p.ProductSku);

        // Get all medicine IDs and glossary IDs
        var medicineIds = allProducts
            .Where(p => p.MedicineId != null)
            .Select(p => p.MedicineId!)
            .Distinct()
            .ToList();

        var glossaryIds = allProducts
            .Where(p => p.GlossaryId != null)
            .Select(p => p.GlossaryId!)
            .Distinct()
            .ToList();

        // Load medicines and glossaries
        var medicines = await _context.Medicines
            .Where(m => medicineIds.Contains(m.MedicineId))
            .AsNoTracking()
            .ToListAsync();

        var glossaries = await _context.Glossaries
            .Where(g => glossaryIds.Contains(g.GlossaryId))
            .AsNoTracking()
            .ToListAsync();

        var medicineLookup = medicines.ToDictionary(m => m.MedicineId);
        var glossaryLookup = glossaries.ToDictionary(g => g.GlossaryId);

        // ============================================
        // MAPPING TO DTO
        // ============================================
        var saleDTOs = sales.Select(s =>
        {
            var items = s.SalesItems.Select(item =>
            {
                string productName = "Unknown";

                if (productLookup.TryGetValue(item.ProductSku, out var product))
                {
                    if (product.ProductType == "Medicine" && product.MedicineId != null)
                    {
                        if (medicineLookup.TryGetValue(product.MedicineId, out var medicine))
                        {
                            productName = medicine.Name;
                        }
                    }
                    else if (product.ProductType == "Glossary" && product.GlossaryId != null)
                    {
                        if (glossaryLookup.TryGetValue(product.GlossaryId, out var glossary))
                        {
                            productName = glossary.Name;
                        }
                    }
                }

                return new SaleItemSummaryDto
                {
                    ProductName = productName,
                    ProductSku = item.ProductSku,
                    Quantity = item.Quantity,
                    Price = item.SellingPrice,
                    SubTotal = item.SubTotal
                };
            }).ToList();

            return new SaleListItemDto
            {
                SalesId = s.SalesId,
                ReceiptNumber = s.ReceiptNumber ?? string.Empty,
                Date = s.Date,
                Time = s.Time,
                SaleStatus = s.SaleStatus,
                PaymentMethod = s.PaymentMethod, // Deprecated: kept for backward compatibility
                PaymentCompletedAt = s.PaymentCompletedAt, // Deprecated: kept for backward compatibility
                TotalAmount = s.TotalAmount,
                FinalAmountDue = s.FinalAmountDue,
                CustomerDiscountPercent = s.CustomerDiscountPercent,
                RoundingDiscount = s.RoundingDiscount,
                IssuedBy = s.IssuedBy.EmployeeName,
                CustomerName = s.Customer?.CustomerName ?? "Unknown",
                Items = items,
                Payments = s.Payments.Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    SalesId = p.SalesId,
                    PaymentMethod = p.PaymentMethod,
                    PaymentAmount = p.PaymentAmount,
                    PaymentDate = p.PaymentDate,
                    CreatedAt = p.CreatedAt
                }).ToList(),
                TotalPaidAmount = s.Payments.Sum(p => p.PaymentAmount)
            };
        }).ToList();

        // ============================================
        // RETURN PAGINATION ENVELOPE
        // ============================================
        return Ok(new
        {
            currentPage = page,
            pageSize,
            totalItems,
            totalPages,
            data = saleDTOs
        });
    }

    // GET: api/Sales/by-receipt/{receiptNumber}
    [RequirePermission("sales:view_receipt")]
    [HttpGet("by-receipt/{receiptNumber}")]
    public async Task<IActionResult> GetSaleByReceiptNumber(string receiptNumber)
    {
        _logger.LogInformation("Fetching sale with receipt number: {ReceiptNumber}", receiptNumber);

        // Load the sale with all related data
        var sale = await _context.Sales
            .Include(s => s.IssuedBy)
            .Include(s => s.Customer)
            .Include(s => s.SalesItems)
            .Include(s => s.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ReceiptNumber == receiptNumber);

        if (sale == null)
        {
            _logger.LogWarning("Sale with receipt number {ReceiptNumber} not found", receiptNumber);
            return NotFound("Sale not found.");
        }

        // ============================================
        // LOAD ALL PRODUCTS AND RELATED DATA EFFICIENTLY
        // ============================================
        var allProductSkus = sale.SalesItems
            .Select(si => si.ProductSku)
            .Distinct()
            .ToList();

        var allProducts = await _context.Products
            .Where(p => allProductSkus.Contains(p.ProductSku))
            .AsNoTracking()
            .ToListAsync();

        // Get all medicine IDs and glossary IDs
        var medicineIds = allProducts
            .Where(p => p.MedicineId != null)
            .Select(p => p.MedicineId!)
            .Distinct()
            .ToList();

        var glossaryIds = allProducts
            .Where(p => p.GlossaryId != null)
            .Select(p => p.GlossaryId!)
            .Distinct()
            .ToList();

        // Load medicines and glossaries
        var medicines = await _context.Medicines
            .Where(m => medicineIds.Contains(m.MedicineId))
            .AsNoTracking()
            .ToListAsync();

        var glossaries = await _context.Glossaries
            .Where(g => glossaryIds.Contains(g.GlossaryId))
            .AsNoTracking()
            .ToListAsync();

        var productLookup = allProducts.ToDictionary(p => p.ProductSku);
        var medicineLookup = medicines.ToDictionary(m => m.MedicineId);
        var glossaryLookup = glossaries.ToDictionary(g => g.GlossaryId);

        // ============================================
        // MAPPING TO DTO
        // ============================================
        var items = sale.SalesItems.Select(item =>
        {
            string productName = "Unknown";

            if (productLookup.TryGetValue(item.ProductSku, out var product))
            {
                if (product.ProductType == "Medicine" && product.MedicineId != null)
                {
                    if (medicineLookup.TryGetValue(product.MedicineId, out var medicine))
                    {
                        productName = medicine.Name;
                    }
                }
                else if (product.ProductType == "Glossary" && product.GlossaryId != null)
                {
                    if (glossaryLookup.TryGetValue(product.GlossaryId, out var glossary))
                    {
                        productName = glossary.Name;
                    }
                }
            }

            return new SaleItemSummaryDto
            {
                ProductName = productName,
                ProductSku = item.ProductSku,
                Quantity = item.Quantity,
                Price = item.SellingPrice,
                SubTotal = item.SubTotal
            };
        }).ToList();

        var saleDto = new SaleListItemDto
        {
            SalesId = sale.SalesId,
            ReceiptNumber = sale.ReceiptNumber ?? string.Empty,
            Date = sale.Date,
            Time = sale.Time,
            SaleStatus = sale.SaleStatus,
            PaymentMethod = sale.PaymentMethod, // Deprecated: kept for backward compatibility
            PaymentCompletedAt = sale.PaymentCompletedAt, // Deprecated: kept for backward compatibility
            TotalAmount = sale.TotalAmount,
            FinalAmountDue = sale.FinalAmountDue,
            CustomerDiscountPercent = sale.CustomerDiscountPercent,
            RoundingDiscount = sale.RoundingDiscount,
            IssuedBy = sale.IssuedBy.EmployeeName,
            CustomerName = sale.Customer?.CustomerName,
            Items = items,
            Payments = sale.Payments.Select(p => new PaymentDto
            {
                PaymentId = p.PaymentId,
                SalesId = p.SalesId,
                PaymentMethod = p.PaymentMethod,
                PaymentAmount = p.PaymentAmount,
                PaymentDate = p.PaymentDate,
                CreatedAt = p.CreatedAt
            }).ToList(),
            TotalPaidAmount = sale.Payments.Sum(p => p.PaymentAmount)
        };

        _logger.LogInformation("Successfully retrieved sale with ID: {SalesId}", sale.SalesId);

        return Ok(saleDto);
    }

    // GET: api/Sales/summary/by-supplier?fromDate=2026-01-01&toDate=2026-01-31
    [RequirePermission("sales:view_summary")]
    [HttpGet("summary/by-supplier")]
    public async Task<IActionResult> GetSalesBySupplier(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        _logger.LogInformation("Fetching sales summary by supplier, fromDate: {From}, toDate: {To}", fromDate, toDate);

        // Query SalesItems that have a linked StockId → join to Stock → Supplier
        var query = _context.SalesItems
            .Include(si => si.Sales)
            .Include(si => si.Stock)
                .ThenInclude(s => s!.Supplier)
            .Where(si => si.StockId != null &&
                         si.Sales.SaleStatus == "Paid" || si.Sales.SaleStatus == "PartiallyPaid");

        if (fromDate.HasValue)
            query = query.Where(si => si.Sales.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(si => si.Sales.Date <= toDate.Value);

        var items = await query.ToListAsync();

        var grouped = items
            .GroupBy(si => new
            {
                SupplierId = si.Stock?.SupplierId ?? "UNKNOWN",
                SupplierName = si.Stock?.Supplier?.SupplierName ?? "Unknown Supplier"
            })
            .Select(g => new
            {
                g.Key.SupplierId,
                g.Key.SupplierName,
                TotalUnitsSold = g.Sum(si => si.Quantity),
                TotalRevenue = g.Sum(si => si.SubTotal),
                BatchCount = g.Select(si => si.StockId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        return Ok(new
        {
            FromDate = fromDate,
            ToDate = toDate,
            Data = grouped
        });
    }

    //TEST SALE RECEIPT
    //test api
    [RequirePermission("sales:view_receipt")]
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok("Test API is working new test");
    }
}
