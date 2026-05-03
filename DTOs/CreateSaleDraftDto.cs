using System.ComponentModel.DataAnnotations;

namespace pharmacyPOS.API.DTOs;

public class CreateReceiptItemDto
{
    [Required]
    public string ProductSku { get; set; } = null!;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }

    [Required]
    public decimal SubTotal { get; set; }

    // Optional: cashier-selected stock batch. When provided, deducts from this specific batch.
    // When null, falls back to FEFO auto-deduction.
    public long? StockId { get; set; }
}

public class CreateReceiptWithItemsDto
{
    [Required(ErrorMessage = "At least one item is required.")]
    public List<CreateReceiptItemDto> Items { get; set; } = new();

    [Required]
    public decimal TotalAmount { get; set; }
}
