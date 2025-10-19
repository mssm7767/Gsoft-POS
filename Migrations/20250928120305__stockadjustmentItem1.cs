using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSoftPosNew.Migrations
{
    /// <inheritdoc />
    public partial class _stockadjustmentItem1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentItem_StockAdjustments_StockAdjustmentId",
                table: "StockAdjustmentItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockAdjustmentItem",
                table: "StockAdjustmentItem");

            migrationBuilder.RenameTable(
                name: "StockAdjustmentItem",
                newName: "StockAdjustmentItems");

            migrationBuilder.RenameIndex(
                name: "IX_StockAdjustmentItem_StockAdjustmentId",
                table: "StockAdjustmentItems",
                newName: "IX_StockAdjustmentItems_StockAdjustmentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockAdjustmentItems",
                table: "StockAdjustmentItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentItems_StockAdjustments_StockAdjustmentId",
                table: "StockAdjustmentItems",
                column: "StockAdjustmentId",
                principalTable: "StockAdjustments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustmentItems_StockAdjustments_StockAdjustmentId",
                table: "StockAdjustmentItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockAdjustmentItems",
                table: "StockAdjustmentItems");

            migrationBuilder.RenameTable(
                name: "StockAdjustmentItems",
                newName: "StockAdjustmentItem");

            migrationBuilder.RenameIndex(
                name: "IX_StockAdjustmentItems_StockAdjustmentId",
                table: "StockAdjustmentItem",
                newName: "IX_StockAdjustmentItem_StockAdjustmentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockAdjustmentItem",
                table: "StockAdjustmentItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustmentItem_StockAdjustments_StockAdjustmentId",
                table: "StockAdjustmentItem",
                column: "StockAdjustmentId",
                principalTable: "StockAdjustments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
