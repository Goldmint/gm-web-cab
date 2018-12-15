using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class wallettocabinet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "limit",
                table: "gm_promo_code",
                type: "decimal(38, 18)",
                nullable: false,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<double>(
                name: "discount_value",
                table: "gm_promo_code",
                nullable: false,
                oldClrType: typeof(long));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "limit",
                table: "gm_promo_code",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38, 18)");

            migrationBuilder.AlterColumn<long>(
                name: "discount_value",
                table: "gm_promo_code",
                nullable: false,
                oldClrType: typeof(double));
        }
    }
}
