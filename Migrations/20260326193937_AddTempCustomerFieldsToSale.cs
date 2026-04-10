using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSoftPosNew.Migrations
{
    /// <inheritdoc />
    public partial class AddTempCustomerFieldsToSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerAddress",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerDisplayName",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TerminalPrinterSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PCName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiptPrinterPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminalPrinterSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerminalPrinterSettings");

            migrationBuilder.DropColumn(
                name: "CustomerAddress",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CustomerDisplayName",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "Sales");
        }
    }
}
