using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class multiusedpromoupdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "code",
                table: "gm_used_promo_codes");

            migrationBuilder.DropColumn(
                name: "usage_type",
                table: "gm_used_promo_codes");

            migrationBuilder.AddColumn<long>(
                name: "user_id",
                table: "gm_used_promo_codes",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_gm_used_promo_codes_user_id",
                table: "gm_used_promo_codes",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_used_promo_codes_gm_user_user_id",
                table: "gm_used_promo_codes",
                column: "user_id",
                principalTable: "gm_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_used_promo_codes_gm_user_user_id",
                table: "gm_used_promo_codes");

            migrationBuilder.DropIndex(
                name: "IX_gm_used_promo_codes_user_id",
                table: "gm_used_promo_codes");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "gm_used_promo_codes");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "gm_used_promo_codes",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "usage_type",
                table: "gm_used_promo_codes",
                nullable: false,
                defaultValue: 0);
        }
    }
}
