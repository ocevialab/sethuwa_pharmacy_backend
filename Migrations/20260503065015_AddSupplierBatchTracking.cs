using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharmacyPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierBatchTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Supplier_ID",
                table: "Stock",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Stock_ID",
                table: "Sales_Items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Strength",
                table: "Medicines",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "Required_Prescription",
                table: "Medicines",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "Low_Stock_Threshold",
                table: "Medicines",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Generic_Name",
                table: "Medicines",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Medicines",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Brand_Name",
                table: "Medicines",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Supplier_ID",
                table: "Stock",
                column: "Supplier_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Items_Stock_ID",
                table: "Sales_Items",
                column: "Stock_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesItems_Stock",
                table: "Sales_Items",
                column: "Stock_ID",
                principalTable: "Stock",
                principalColumn: "Stock_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_Suppliers",
                table: "Stock",
                column: "Supplier_ID",
                principalTable: "Suppliers",
                principalColumn: "Supplier_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesItems_Stock",
                table: "Sales_Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Stock_Suppliers",
                table: "Stock");

            migrationBuilder.DropIndex(
                name: "IX_Stock_Supplier_ID",
                table: "Stock");

            migrationBuilder.DropIndex(
                name: "IX_Sales_Items_Stock_ID",
                table: "Sales_Items");

            migrationBuilder.DropColumn(
                name: "Supplier_ID",
                table: "Stock");

            migrationBuilder.DropColumn(
                name: "Stock_ID",
                table: "Sales_Items");

            migrationBuilder.AlterColumn<string>(
                name: "Strength",
                table: "Medicines",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Required_Prescription",
                table: "Medicines",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Low_Stock_Threshold",
                table: "Medicines",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Generic_Name",
                table: "Medicines",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Medicines",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Brand_Name",
                table: "Medicines",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
