﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSoftPosNew.Migrations
{
    /// <inheritdoc />
    public partial class _item_unitprice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "Items",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "Items");
        }
    }
}
