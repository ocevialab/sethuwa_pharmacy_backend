using System.ComponentModel.DataAnnotations;

namespace pharmacyPOS.API.DTOs;

public class CustomerUpdateDto
{
    [Required(ErrorMessage = "Customer name is required.")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string CustomerName { get; set; } = null!;

    [Required(ErrorMessage = "Contact number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    [MaxLength(15, ErrorMessage = "Contact number cannot exceed 15 characters.")]
    public string ContactNumber { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string? EmailAddress { get; set; }

    [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public string? Address { get; set; }

    [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
    public decimal? Discount { get; set; }

    [Required(ErrorMessage = "Customer status is required.")]
    [RegularExpression("Active|Inactive", ErrorMessage = "Status must be Active or Inactive.")]
    public string CustomerStatus { get; set; } = "Active";
}
