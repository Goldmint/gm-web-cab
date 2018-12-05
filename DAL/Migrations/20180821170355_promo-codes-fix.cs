using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class promocodesfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "gm_promo_code");

            migrationBuilder.AddColumn<long>(
                name: "promo_code_id",
                table: "gm_buy_gold_request",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_request_promo_code_id",
                table: "gm_buy_gold_request",
                column: "promo_code_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_buy_gold_request_gm_promo_code_promo_code_id",
                table: "gm_buy_gold_request",
                column: "promo_code_id",
                principalTable: "gm_promo_code",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_buy_gold_request_gm_promo_code_promo_code_id",
                table: "gm_buy_gold_request");

            migrationBuilder.DropIndex(
                name: "IX_gm_buy_gold_request_promo_code_id",
                table: "gm_buy_gold_request");

            migrationBuilder.DropColumn(
                name: "promo_code_id",
                table: "gm_buy_gold_request");

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "gm_promo_code",
                nullable: false,
                defaultValue: 0);
        }
    }
}
