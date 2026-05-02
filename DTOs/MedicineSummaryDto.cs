namespace pharmacyPOS.API.DTOs;

public class MedicineSummaryDto
{
    public int TotalMedicines { get; set; }
    public int ActiveMedicines { get; set; }
    public int DeletedMedicines { get; set; }
    public int PrescriptionRequired { get; set; }
    public int NonPrescription { get; set; }
}









