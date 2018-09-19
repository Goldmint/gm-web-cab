using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class promocodesupdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "value",
                table: "gm_promo_code");

            migrationBuilder.AddColumn<long>(
                name: "discount_value",
                table: "gm_promo_code",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "limit",
                table: "gm_promo_code",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "token_type",
                table: "gm_promo_code",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "discount_value",
                table: "gm_promo_code");

            migrationBuilder.DropColumn(
                name: "limit",
                table: "gm_promo_code");

            migrationBuilder.DropColumn(
                name: "token_type",
                table: "gm_promo_code");

            migrationBuilder.AddColumn<decimal>(
                name: "value",
                table: "gm_promo_code",
                type: "decimal(38, 18)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
