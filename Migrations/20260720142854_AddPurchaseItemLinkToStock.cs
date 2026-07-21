using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pharmacyPOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseItemLinkToStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Purchase_Item_ID",
                table: "Stock",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stock_Purchase_Item_ID",
                table: "Stock",
                column: "Purchase_Item_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Stock_PurchaseItem",
                table: "Stock",
                column: "Purchase_Item_ID",
                principalTable: "Purchase_Items",
                principalColumn: "Purchase_Item_ID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stock_PurchaseItem",
                table: "Stock");

            migrationBuilder.DropIndex(
                name: "IX_Stock_Purchase_Item_ID",
                table: "Stock");

            migrationBuilder.DropColumn(
                name: "Purchase_Item_ID",
                table: "Stock");
        }
    }
}
