using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSoftPosNew.Migrations
{
    /// <inheritdoc />
    public partial class _OrderType_sake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                table: "Sales",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "Sales");
        }
    }
}
