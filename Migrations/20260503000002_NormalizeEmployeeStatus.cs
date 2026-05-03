using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharmacyPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeEmployeeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Employees SET Employee_Status = UPPER(Employee_Status) WHERE Employee_Status <> UPPER(Employee_Status);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Normalization is not reversible without original casing data
        }
    }
}
