using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSoftPosNew.Migrations
{
    /// <inheritdoc />
    public partial class _setting_ntn_strn_posid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NTN",
                table: "ShopSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "POSId",
                table: "ShopSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "STRN",
                table: "ShopSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NTN",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "POSId",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "STRN",
                table: "ShopSettings");
        }
    }
}
