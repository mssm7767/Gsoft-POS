using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSoftPosNew.Migrations
{
    /// <inheritdoc />
    public partial class _itemid_saleitem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Products_ItemId",
                table: "SaleItems");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Items_ItemId",
                table: "SaleItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Items_ItemId",
                table: "SaleItems");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Products_ItemId",
                table: "SaleItems",
                column: "ItemId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
