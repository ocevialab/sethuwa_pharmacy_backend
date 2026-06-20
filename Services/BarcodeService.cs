using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.Models;

namespace pharmacyPOS.API.Services;

public class BarcodeService
{
    private const string InternalPrefix = "200";
    private readonly SethsuwaPharmacyDbContext _context;
    private readonly ILogger<BarcodeService> _logger;

    public BarcodeService(SethsuwaPharmacyDbContext context, ILogger<BarcodeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateUniqueEan13Async(CancellationToken cancellationToken = default)
    {
        var existing = await _context.Products
            .Where(p => p.Barcode != null && p.Barcode.StartsWith(InternalPrefix) && p.Barcode.Length == 13)
            .Select(p => p.Barcode!)
            .ToListAsync(cancellationToken);

        long maxSequence = 0;
        foreach (var code in existing)
        {
            if (code.Length == 13 && long.TryParse(code.AsSpan(3, 9), out var seq))
                maxSequence = Math.Max(maxSequence, seq);
        }

        for (var attempt = 0; attempt < 100; attempt++)
        {
            var next = maxSequence + 1 + attempt;
            if (next > 999_999_999)
                throw new InvalidOperationException("Internal barcode sequence exhausted.");

            var candidate = BuildEan13(next);
            var taken = await _context.Products
                .AnyAsync(p => p.Barcode == candidate, cancellationToken);
            if (!taken)
            {
                _logger.LogInformation("Generated barcode {Barcode} (sequence {Sequence})", candidate, next);
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique barcode after multiple attempts.");
    }

    public static string BuildEan13(long nineDigitSequence)
    {
        if (nineDigitSequence is < 0 or > 999_999_999)
            throw new ArgumentOutOfRangeException(nameof(nineDigitSequence));

        var twelve = InternalPrefix + nineDigitSequence.ToString("D9");
        var check = ComputeEan13CheckDigit(twelve);
        return twelve + check;
    }

    public static int ComputeEan13CheckDigit(string twelveDigits)
    {
        if (twelveDigits.Length != 12 || !twelveDigits.All(char.IsDigit))
            throw new ArgumentException("EAN-13 requires exactly 12 numeric digits before the check digit.");

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var d = twelveDigits[i] - '0';
            sum += (i % 2 == 0) ? d : d * 3;
        }

        return (10 - (sum % 10)) % 10;
    }

    public async Task EnsureBarcodeAvailableAsync(
        string barcode,
        string productSku,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return;

        barcode = barcode.Trim();
        if (barcode.Length is < 8 or > 50)
            throw new InvalidOperationException("Barcode must be between 8 and 50 characters.");

        var conflict = await _context.Products
            .AnyAsync(p => p.Barcode == barcode && p.ProductSku != productSku, cancellationToken);

        if (conflict)
            throw new InvalidOperationException($"Barcode \"{barcode}\" is already assigned to another product.");
    }

    public async Task<Product> GetMedicineProductAsync(string medicineId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(
                p => p.MedicineId == medicineId && p.ProductType == "Medicine" && !p.IsDeleted,
                cancellationToken);

        if (product == null)
            throw new InvalidOperationException($"No active product found for medicine {medicineId}.");

        return product;
    }

    public async Task<string> GetMedicineDisplayNameAsync(string medicineId, CancellationToken cancellationToken = default)
    {
        var medicine = await _context.Medicines
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MedicineId == medicineId, cancellationToken);

        return medicine?.Name ?? medicineId;
    }
}
