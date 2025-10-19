using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSoftPosNew.Migrations
{
    /// <inheritdoc />
    public partial class _flavour_in_item : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Flavour",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Flavour",
                table: "Items");
        }
    }
}
