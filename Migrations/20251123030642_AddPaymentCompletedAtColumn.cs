using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharmacyPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentCompletedAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Customer___Custo__48CFD27E",
                table: "Customer_Recurrent_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Customer___Produ__49C3F6B7",
                table: "Customer_Recurrent_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Products__Glossa__3E52440B",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK__Products__Medici__3D5E1FD2",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK__Purchase___Produ__5812160E",
                table: "Purchase_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Purchase___Purch__571DF1D5",
                table: "Purchase_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Purchases__Suppl__5441852A",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales__Billed_By__66603565",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales__Customer___6754599E",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales__Issued_By__656C112C",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales_Ite__Produ__6B24EA82",
                table: "Sales_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales_Ite__Sales__6A30C649",
                table: "Sales_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Stock__Product_S__5CD6CB2B",
                table: "Stock");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Supplier__83918D98C2B32CB6",
                table: "Suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Stock__EFA64EB8C9903153",
                table: "Stock");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Sales_It__320F1BA391B1067D",
                table: "Sales_Items");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Sales__32123EFA2BF1222C",
                table: "Sales");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Purchase__543E6DA311D02B7E",
                table: "Purchases");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Purchase__4CEA41E2CD3A3748",
                table: "Purchase_Items");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Products__5C44DD337404FC33",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Medicine__5F01023509791C41",
                table: "Medicines");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Glossari__D5F2E444D696A820",
                table: "Glossaries");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Employee__7811348172D5E011",
                table: "Employees");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Customer__8CB286B94BF2432E",
                table: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Customer__B535C776C8C9BF51",
                table: "Customer_Recurrent_Items");

            migrationBuilder.RenameIndex(
                name: "UQ__Stock__B2FED760161F83A1",
                table: "Stock",
                newName: "UQ__Stock__B2FED760F263F927");

            migrationBuilder.RenameColumn(
                name: "Is_Deleted",
                table: "Products",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "Is_Deleted",
                table: "Medicines",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "Is_Deleted",
                table: "Glossaries",
                newName: "IsDeleted");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Supplier__83918D98532D7B48",
                table: "Suppliers",
                column: "Supplier_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Stock__EFA64EB85A10A757",
                table: "Stock",
                column: "Stock_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Sales_It__320F1BA37575F4B8",
                table: "Sales_Items",
                column: "Sales_Item_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Sales__32123EFAAD35C0BE",
                table: "Sales",
                column: "Sales_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Purchase__543E6DA34F2568D7",
                table: "Purchases",
                column: "Purchase_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Purchase__4CEA41E2080BD38C",
                table: "Purchase_Items",
                column: "Purchase_Item_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Products__5C44DD333C7A5A01",
                table: "Products",
                column: "Product_SKU");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Medicine__5F0102352C5A171D",
                table: "Medicines",
                column: "Medicine_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Glossari__D5F2E44475609879",
                table: "Glossaries",
                column: "Glossary_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Employee__7811348181A106AF",
                table: "Employees",
                column: "Employee_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Customer__8CB286B9E2DDE106",
                table: "Customers",
                column: "Customer_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Customer__B535C77670592A73",
                table: "Customer_Recurrent_Items",
                column: "Recurrent_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__Supplier__C919E828292C941E",
                table: "Suppliers",
                column: "Supplier_Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Medicine__737584F611D8362D",
                table: "Medicines",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Glossari__737584F61C326F38",
                table: "Glossaries",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK__Customer___Custo__4AB81AF0",
                table: "Customer_Recurrent_Items",
                column: "Customer_ID",
                principalTable: "Customers",
                principalColumn: "Customer_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Customer___Produ__4BAC3F29",
                table: "Customer_Recurrent_Items",
                column: "Product_SKU",
                principalTable: "Products",
                principalColumn: "Product_SKU");

            migrationBuilder.AddForeignKey(
                name: "FK__Products__Glossa__4316F928",
                table: "Products",
                column: "Glossary_ID",
                principalTable: "Glossaries",
                principalColumn: "Glossary_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Products__Medici__4222D4EF",
                table: "Products",
                column: "Medicine_ID",
                principalTable: "Medicines",
                principalColumn: "Medicine_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Purchase___Produ__534D60F1",
                table: "Purchase_Items",
                column: "Product_SKU",
                principalTable: "Products",
                principalColumn: "Product_SKU");

            migrationBuilder.AddForeignKey(
                name: "FK__Purchase___Purch__52593CB8",
                table: "Purchase_Items",
                column: "Purchase_ID",
                principalTable: "Purchases",
                principalColumn: "Purchase_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Purchases__Suppl__4F7CD00D",
                table: "Purchases",
                column: "Supplier_ID",
                principalTable: "Suppliers",
                principalColumn: "Supplier_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales__Billed_By__5AEE82B9",
                table: "Sales",
                column: "Billed_By_ID",
                principalTable: "Employees",
                principalColumn: "Employee_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales__Customer___5BE2A6F2",
                table: "Sales",
                column: "Customer_ID",
                principalTable: "Customers",
                principalColumn: "Customer_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales__Issued_By__59FA5E80",
                table: "Sales",
                column: "Issued_By_ID",
                principalTable: "Employees",
                principalColumn: "Employee_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales_Ite__Produ__5FB337D6",
                table: "Sales_Items",
                column: "Product_SKU",
                principalTable: "Products",
                principalColumn: "Product_SKU");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales_Ite__Sales__5EBF139D",
                table: "Sales_Items",
                column: "Sales_ID",
                principalTable: "Sales",
                principalColumn: "Sales_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Stock__Product_S__571DF1D5",
                table: "Stock",
                column: "Product_SKU",
                principalTable: "Products",
                principalColumn: "Product_SKU");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Customer___Custo__4AB81AF0",
                table: "Customer_Recurrent_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Customer___Produ__4BAC3F29",
                table: "Customer_Recurrent_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Products__Glossa__4316F928",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK__Products__Medici__4222D4EF",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK__Purchase___Produ__534D60F1",
                table: "Purchase_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Purchase___Purch__52593CB8",
                table: "Purchase_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Purchases__Suppl__4F7CD00D",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales__Billed_By__5AEE82B9",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales__Customer___5BE2A6F2",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales__Issued_By__59FA5E80",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales_Ite__Produ__5FB337D6",
                table: "Sales_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Sales_Ite__Sales__5EBF139D",
                table: "Sales_Items");

            migrationBuilder.DropForeignKey(
                name: "FK__Stock__Product_S__571DF1D5",
                table: "Stock");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Supplier__83918D98532D7B48",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "UQ__Supplier__C919E828292C941E",
                table: "Suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Stock__EFA64EB85A10A757",
                table: "Stock");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Sales_It__320F1BA37575F4B8",
                table: "Sales_Items");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Sales__32123EFAAD35C0BE",
                table: "Sales");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Purchase__543E6DA34F2568D7",
                table: "Purchases");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Purchase__4CEA41E2080BD38C",
                table: "Purchase_Items");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Products__5C44DD333C7A5A01",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Medicine__5F0102352C5A171D",
                table: "Medicines");

            migrationBuilder.DropIndex(
                name: "UQ__Medicine__737584F611D8362D",
                table: "Medicines");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Glossari__D5F2E44475609879",
                table: "Glossaries");

            migrationBuilder.DropIndex(
                name: "UQ__Glossari__737584F61C326F38",
                table: "Glossaries");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Employee__7811348181A106AF",
                table: "Employees");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Customer__8CB286B9E2DDE106",
                table: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Customer__B535C77670592A73",
                table: "Customer_Recurrent_Items");

            migrationBuilder.RenameIndex(
                name: "UQ__Stock__B2FED760F263F927",
                table: "Stock",
                newName: "UQ__Stock__B2FED760161F83A1");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Products",
                newName: "Is_Deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Medicines",
                newName: "Is_Deleted");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Glossaries",
                newName: "Is_Deleted");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Supplier__83918D98C2B32CB6",
                table: "Suppliers",
                column: "Supplier_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Stock__EFA64EB8C9903153",
                table: "Stock",
                column: "Stock_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Sales_It__320F1BA391B1067D",
                table: "Sales_Items",
                column: "Sales_Item_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Sales__32123EFA2BF1222C",
                table: "Sales",
                column: "Sales_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Purchase__543E6DA311D02B7E",
                table: "Purchases",
                column: "Purchase_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Purchase__4CEA41E2CD3A3748",
                table: "Purchase_Items",
                column: "Purchase_Item_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Products__5C44DD337404FC33",
                table: "Products",
                column: "Product_SKU");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Medicine__5F01023509791C41",
                table: "Medicines",
                column: "Medicine_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Glossari__D5F2E444D696A820",
                table: "Glossaries",
                column: "Glossary_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Employee__7811348172D5E011",
                table: "Employees",
                column: "Employee_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Customer__8CB286B94BF2432E",
                table: "Customers",
                column: "Customer_ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Customer__B535C776C8C9BF51",
                table: "Customer_Recurrent_Items",
                column: "Recurrent_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Customer___Custo__48CFD27E",
                table: "Customer_Recurrent_Items",
                column: "Customer_ID",
                principalTable: "Customers",
                principalColumn: "Customer_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Customer___Produ__49C3F6B7",
                table: "Customer_Recurrent_Items",
                column: "Product_SKU",
                principalTable: "Products",
                principalColumn: "Product_SKU");

            migrationBuilder.AddForeignKey(
                name: "FK__Products__Glossa__3E52440B",
                table: "Products",
                column: "Glossary_ID",
                principalTable: "Glossaries",
                principalColumn: "Glossary_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Products__Medici__3D5E1FD2",
                table: "Products",
                column: "Medicine_ID",
                principalTable: "Medicines",
                principalColumn: "Medicine_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Purchase___Produ__5812160E",
                table: "Purchase_Items",
                column: "Product_SKU",
                principalTable: "Products",
                principalColumn: "Product_SKU");

            migrationBuilder.AddForeignKey(
                name: "FK__Purchase___Purch__571DF1D5",
                table: "Purchase_Items",
                column: "Purchase_ID",
                principalTable: "Purchases",
                principalColumn: "Purchase_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Purchases__Suppl__5441852A",
                table: "Purchases",
                column: "Supplier_ID",
                principalTable: "Suppliers",
                principalColumn: "Supplier_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales__Billed_By__66603565",
                table: "Sales",
                column: "Billed_By_ID",
                principalTable: "Employees",
                principalColumn: "Employee_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales__Customer___6754599E",
                table: "Sales",
                column: "Customer_ID",
                principalTable: "Customers",
                principalColumn: "Customer_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales__Issued_By__656C112C",
                table: "Sales",
                column: "Issued_By_ID",
                principalTable: "Employees",
                principalColumn: "Employee_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales_Ite__Produ__6B24EA82",
                table: "Sales_Items",
                column: "Product_SKU",
                principalTable: "Products",
                principalColumn: "Product_SKU");

            migrationBuilder.AddForeignKey(
                name: "FK__Sales_Ite__Sales__6A30C649",
                table: "Sales_Items",
                column: "Sales_ID",
                principalTable: "Sales",
                principalColumn: "Sales_ID");

            migrationBuilder.AddForeignKey(
                name: "FK__Stock__Product_S__5CD6CB2B",
                table: "Stock",
                column: "Product_SKU",
                principalTable: "Products",
                principalColumn: "Product_SKU");
        }
    }
}
