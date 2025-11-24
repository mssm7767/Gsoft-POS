using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSoftPosNew.Migrations
{
    /// <inheritdoc />
    public partial class _vanStockSale_customerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "VanStockSales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VanStockSales_CustomerId",
                table: "VanStockSales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VanStockSales_ItemId",
                table: "VanStockSales",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_VanStockSales_Customers_CustomerId",
                table: "VanStockSales",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VanStockSales_Items_ItemId",
                table: "VanStockSales",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VanStockSales_Customers_CustomerId",
                table: "VanStockSales");

            migrationBuilder.DropForeignKey(
                name: "FK_VanStockSales_Items_ItemId",
                table: "VanStockSales");

            migrationBuilder.DropIndex(
                name: "IX_VanStockSales_CustomerId",
                table: "VanStockSales");

            migrationBuilder.DropIndex(
                name: "IX_VanStockSales_ItemId",
                table: "VanStockSales");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "VanStockSales");
        }
    }
}
