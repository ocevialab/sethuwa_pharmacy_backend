using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.Models;

namespace pharmacyPOS.API.Utilities;

public static class StockDeduction
{
    public static async Task<bool> DeductUsingFEFO(
    ThilankaPharmacyDbContext context,
    string productSku,
    int quantity,
    long saleId)
    {
        var batches = await context.Stocks
            .Where(s => s.ProductSku == productSku && s.QuantityOnHand > 0)
            .OrderBy(s => s.ExpireDate)
            .ToListAsync();

        int remaining = quantity;

        foreach (var batch in batches)
        {
            if (remaining <= 0)
                break;

            int deduct = Math.Min(batch.QuantityOnHand, remaining);

            batch.QuantityOnHand -= deduct;
            remaining -= deduct;

            // Record stock movement
            context.StockMovements.Add(new StockMovement
            {
                ProductSku = productSku,
                Stock_Id = batch.StockId,
                QuantityChanged = -deduct,
                Reason = "Sale",
                SalesId = saleId,
                CreatedAt = DateTime.UtcNow
            });
        }

        return remaining == 0;
    }

}
