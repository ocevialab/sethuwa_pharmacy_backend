using System.ComponentModel.DataAnnotations;

namespace pharmacyPOS.API.DTOs
{
    public class SupplierCreationDto
    {
        [Required(ErrorMessage = "SupplierName is required.")]
        public required string SupplierName { get; set; }

        public string? ContactPerson { get; set; }

        [Required(ErrorMessage = "ContactNumber is required.")]
        [MaxLength(10, ErrorMessage = "ContactNumber cannot exceed 10 characters.")]
        [MinLength(10, ErrorMessage = "ContactNumber must be exactly 10 digits.")]
        //alow only digits
        [RegularExpression(@"^\d{10}$", ErrorMessage = "ContactNumber must be exactly 10 digits.")]
        public required string ContactNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid EmailAddress format.")]
        public string? EmailAddress { get; set; }
        public string? Address { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountName { get; set; }

        [MaxLength(20, ErrorMessage = "BankAccountNumber cannot exceed 20 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9]{0,20}$", ErrorMessage = "BankAccountNumber can only contain alphanumeric characters and cannot exceed 20 characters.")]
        public string? BankAccountNumber { get; set; }
        public string? BankBranchName { get; set; }
    }
}