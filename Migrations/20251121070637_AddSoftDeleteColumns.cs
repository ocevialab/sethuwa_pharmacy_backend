using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharmacyPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Customer_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Customer_Name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Contact_Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Email_Address = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    Discount = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Customer_Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Customer__8CB286B94BF2432E", x => x.Customer_ID);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Employee_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Employee_Name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Contact_Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Email_Address = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    Employee_Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Password_Hash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Employee__7811348172D5E011", x => x.Employee_ID);
                });

            migrationBuilder.CreateTable(
                name: "Glossaries",
                columns: table => new
                {
                    Glossary_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Brand_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Low_Stock_Threshold = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Glossari__D5F2E444D696A820", x => x.Glossary_ID);
                });

            migrationBuilder.CreateTable(
                name: "Medicines",
                columns: table => new
                {
                    Medicine_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Brand_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Generic_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Manufacture = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Strength = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Required_Prescription = table.Column<bool>(type: "bit", nullable: false),
                    Low_Stock_Threshold = table.Column<int>(type: "int", nullable: false),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Medicine__5F01023509791C41", x => x.Medicine_ID);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Supplier_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Supplier_Name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Contact_Person = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Contact_Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Email_Address = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Bank_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Bank_Account_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Bank_Account_Number = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Bank_Branch_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Supplier__83918D98C2B32CB6", x => x.Supplier_ID);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Sales_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Receipt_Number = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Time = table.Column<TimeOnly>(type: "time", nullable: false),
                    Sale_Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Payment_Method = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Payment_Completed_At = table.Column<DateTime>(type: "datetime", nullable: true),
                    Customer_Discount_Percent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Rounding_Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Total_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Final_Amount_Due = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Issued_By_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Billed_By_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Customer_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Sales__32123EFA2BF1222C", x => x.Sales_ID);
                    table.ForeignKey(
                        name: "FK__Sales__Billed_By__66603565",
                        column: x => x.Billed_By_ID,
                        principalTable: "Employees",
                        principalColumn: "Employee_ID");
                    table.ForeignKey(
                        name: "FK__Sales__Customer___6754599E",
                        column: x => x.Customer_ID,
                        principalTable: "Customers",
                        principalColumn: "Customer_ID");
                    table.ForeignKey(
                        name: "FK__Sales__Issued_By__656C112C",
                        column: x => x.Issued_By_ID,
                        principalTable: "Employees",
                        principalColumn: "Employee_ID");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Product_SKU = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Product_Type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Medicine_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Glossary_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Is_Deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Products__5C44DD337404FC33", x => x.Product_SKU);
                    table.ForeignKey(
                        name: "FK__Products__Glossa__3E52440B",
                        column: x => x.Glossary_ID,
                        principalTable: "Glossaries",
                        principalColumn: "Glossary_ID");
                    table.ForeignKey(
                        name: "FK__Products__Medici__3D5E1FD2",
                        column: x => x.Medicine_ID,
                        principalTable: "Medicines",
                        principalColumn: "Medicine_ID");
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Purchase_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Invoice_Number = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Invoice_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Payment_Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Payment_Due_Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Payment_Method = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Payment_Completed_At = table.Column<DateTime>(type: "datetime", nullable: true),
                    Created_At = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Total_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Supplier_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Purchase__543E6DA311D02B7E", x => x.Purchase_ID);
                    table.ForeignKey(
                        name: "FK__Purchases__Suppl__5441852A",
                        column: x => x.Supplier_ID,
                        principalTable: "Suppliers",
                        principalColumn: "Supplier_ID");
                });

            migrationBuilder.CreateTable(
                name: "Customer_Recurrent_Items",
                columns: table => new
                {
                    Recurrent_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Customer_ID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Product_SKU = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Customer__B535C776C8C9BF51", x => x.Recurrent_ID);
                    table.ForeignKey(
                        name: "FK__Customer___Custo__48CFD27E",
                        column: x => x.Customer_ID,
                        principalTable: "Customers",
                        principalColumn: "Customer_ID");
                    table.ForeignKey(
                        name: "FK__Customer___Produ__49C3F6B7",
                        column: x => x.Product_SKU,
                        principalTable: "Products",
                        principalColumn: "Product_SKU");
                });

            migrationBuilder.CreateTable(
                name: "Sales_Items",
                columns: table => new
                {
                    Sales_Item_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sales_ID = table.Column<long>(type: "bigint", nullable: false),
                    Product_SKU = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Selling_Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Sub_Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Sales_It__320F1BA391B1067D", x => x.Sales_Item_ID);
                    table.ForeignKey(
                        name: "FK__Sales_Ite__Produ__6B24EA82",
                        column: x => x.Product_SKU,
                        principalTable: "Products",
                        principalColumn: "Product_SKU");
                    table.ForeignKey(
                        name: "FK__Sales_Ite__Sales__6A30C649",
                        column: x => x.Sales_ID,
                        principalTable: "Sales",
                        principalColumn: "Sales_ID");
                });

            migrationBuilder.CreateTable(
                name: "Stock",
                columns: table => new
                {
                    Stock_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Lot_Number = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Quantity_on_Hand = table.Column<int>(type: "int", nullable: false),
                    Expire_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Cost_Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Product_SKU = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Stock__EFA64EB8C9903153", x => x.Stock_ID);
                    table.ForeignKey(
                        name: "FK__Stock__Product_S__5CD6CB2B",
                        column: x => x.Product_SKU,
                        principalTable: "Products",
                        principalColumn: "Product_SKU");
                });

            migrationBuilder.CreateTable(
                name: "Purchase_Items",
                columns: table => new
                {
                    Purchase_Item_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cost_Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Selling_Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Expire_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Purchase_ID = table.Column<long>(type: "bigint", nullable: false),
                    Product_SKU = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Purchase__4CEA41E2CD3A3748", x => x.Purchase_Item_ID);
                    table.ForeignKey(
                        name: "FK__Purchase___Produ__5812160E",
                        column: x => x.Product_SKU,
                        principalTable: "Products",
                        principalColumn: "Product_SKU");
                    table.ForeignKey(
                        name: "FK__Purchase___Purch__571DF1D5",
                        column: x => x.Purchase_ID,
                        principalTable: "Purchases",
                        principalColumn: "Purchase_ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Recurrent_Items_Customer_ID",
                table: "Customer_Recurrent_Items",
                column: "Customer_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Recurrent_Items_Product_SKU",
                table: "Customer_Recurrent_Items",
                column: "Product_SKU");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Glossary_ID",
                table: "Products",
                column: "Glossary_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Medicine_ID",
                table: "Products",
                column: "Medicine_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Purchase_Items_Product_SKU",
                table: "Purchase_Items",
                column: "Product_SKU");

            migrationBuilder.CreateIndex(
                name: "IX_Purchase_Items_Purchase_ID",
                table: "Purchase_Items",
                column: "Purchase_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_Supplier_ID",
                table: "Purchases",
                column: "Supplier_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Billed_By_ID",
                table: "Sales",
                column: "Billed_By_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Customer_ID",
                table: "Sales",
                column: "Customer_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Issued_By_ID",
                table: "Sales",
                column: "Issued_By_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Items_Product_SKU",
                table: "Sales_Items",
                column: "Product_SKU");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Items_Sales_ID",
                table: "Sales_Items",
                column: "Sales_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Product_SKU",
                table: "Stock",
                column: "Product_SKU");

            migrationBuilder.CreateIndex(
                name: "UQ__Stock__B2FED760161F83A1",
                table: "Stock",
                column: "Lot_Number",
                unique: true,
                filter: "[Lot_Number] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customer_Recurrent_Items");

            migrationBuilder.DropTable(
                name: "Purchase_Items");

            migrationBuilder.DropTable(
                name: "Sales_Items");

            migrationBuilder.DropTable(
                name: "Stock");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Glossaries");

            migrationBuilder.DropTable(
                name: "Medicines");
        }
    }
}
