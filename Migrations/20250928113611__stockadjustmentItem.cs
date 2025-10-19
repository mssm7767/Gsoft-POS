using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSoftPosNew.Migrations
{
    /// <inheritdoc />
    public partial class _stockadjustmentItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComputerStock",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "Difference",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "ItemCode",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "PhysicalStock",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "StockAdjustments");

            migrationBuilder.DropColumn(
                name: "TotalDifference",
                table: "StockAdjustments");

            migrationBuilder.CreateTable(
                name: "StockAdjustmentItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockAdjustmentId = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ComputerStock = table.Column<int>(type: "int", nullable: true),
                    PhysicalStock = table.Column<int>(type: "int", nullable: true),
                    Difference = table.Column<int>(type: "int", nullable: true),
                    TotalDifference = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAdjustmentItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAdjustmentItem_StockAdjustments_StockAdjustmentId",
                        column: x => x.StockAdjustmentId,
                        principalTable: "StockAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustmentItem_StockAdjustmentId",
                table: "StockAdjustmentItem",
                column: "StockAdjustmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockAdjustmentItem");

            migrationBuilder.AddColumn<int>(
                name: "ComputerStock",
                table: "StockAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Difference",
                table: "StockAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                table: "StockAdjustments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "StockAdjustments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhysicalStock",
                table: "StockAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "StockAdjustments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "StockAdjustments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDifference",
                table: "StockAdjustments",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
