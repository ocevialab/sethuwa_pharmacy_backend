namespace pharmacyPOS.API.DTOs;

public class MedicineExcelBulkUpdateRowResultDto
{
    public int RowNumber { get; set; }
    public string? MedicineId { get; set; }
    public string Status { get; set; } = ""; // Success, Error, Skipped
    public string Message { get; set; } = "";
}

public class MedicineExcelBulkUpdateSummaryDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
    public List<MedicineExcelBulkUpdateRowResultDto> Rows { get; set; } = new();
}
