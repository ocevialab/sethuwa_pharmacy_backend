namespace pharmacyPOS.API.DTOs
{
    public class SupplierDto
    {
        public required string SupplierName { get; set; }
        public string? ContactPerson { get; set; }
        public required string ContactNumber { get; set; }
        public string? EmailAddress { get; set; }
        public string? Address { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankBranchName { get; set; }
    }
}

