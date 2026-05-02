public class PurchasingSummaryDto
{
    public PendingPaymentsDto PendingPayments { get; set; } = new();
    public MonthlyPurchasesDto MonthlyPurchases { get; set; } = new();
    public ItemsReceivedDto ItemsReceived { get; set; } = new();
    public int ActiveSuppliers { get; set; }
}

public class PendingPaymentsDto
{
    public decimal TotalAmount { get; set; }
    public int InvoiceCount { get; set; }
}

public class MonthlyPurchasesDto
{
    public decimal TotalAmount { get; set; }
    public int InvoiceCount { get; set; }
}

public class ItemsReceivedDto
{
    public int TotalQuantity { get; set; }
    public int Batches { get; set; }
}

public class PaymentSummaryDto
{
    public MonthlyPaymentsDto MonthlyPayments { get; set; } = new();
    public PendingPaymentSummaryDto PendingPayments { get; set; } = new();
    public OverduePaymentSummaryDto OverduePayments { get; set; } = new();
}

public class MonthlyPaymentsDto
{
    public decimal TotalAmount { get; set; }
    public int PaymentCount { get; set; }
}

public class PendingPaymentSummaryDto
{
    public decimal TotalAmount { get; set; }
    public int PaymentCount { get; set; }
}

public class OverduePaymentSummaryDto
{
    public decimal TotalAmount { get; set; }
    public int PaymentCount { get; set; }
}