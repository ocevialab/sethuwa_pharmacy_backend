using Microsoft.EntityFrameworkCore;
using pharmacyPOS.API.Models;

namespace pharmacyPOS.API.Utilities
{
    public static class ReceiptNumberGenerator
    {
        public static async Task<string> GenerateAsync(SethsuwaPharmacyDbContext context)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Get the most recent receipt for today
            var lastReceipt = await context.Sales
                .Where(s => s.Date == today)
                .OrderByDescending(s => s.SalesId)
                .Select(s => s.ReceiptNumber)
                .FirstOrDefaultAsync();

            int counter = 0;

            // Parse last receipt if exists
            if (!string.IsNullOrEmpty(lastReceipt))
            {
                // Expected format: YYYYMMDD-XYZ
                var parts = lastReceipt.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int lastCounter))
                {
                    counter = lastCounter + 1;
                }
            }

            // 3-digit minimum formatting → 000, 001, 002, ... 999, 1000
            string counterStr = counter.ToString("D3");

            // Format full receipt number
            string todayStr = DateTime.UtcNow.ToString("yyyyMMdd");

            return $"{todayStr}-{counterStr}";
        }
    }
}
