using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class financialhistory2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

			migrationBuilder.Sql("delete from gm_buy_request where 1=1");
			migrationBuilder.Sql("delete from gm_sell_request where 1=1");

			migrationBuilder.AddColumn<long>(
                name: "ref_fin_history",
                table: "gm_sell_request",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ref_fin_history",
                table: "gm_buy_request",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_request_ref_fin_history",
                table: "gm_sell_request",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_request_ref_fin_history",
                table: "gm_buy_request",
                column: "ref_fin_history");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_buy_request_gm_financial_history_ref_fin_history",
                table: "gm_buy_request",
                column: "ref_fin_history",
                principalTable: "gm_financial_history",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_sell_request_gm_financial_history_ref_fin_history",
                table: "gm_sell_request",
                column: "ref_fin_history",
                principalTable: "gm_financial_history",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_buy_request_gm_financial_history_ref_fin_history",
                table: "gm_buy_request");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_sell_request_gm_financial_history_ref_fin_history",
                table: "gm_sell_request");

            migrationBuilder.DropIndex(
                name: "IX_gm_sell_request_ref_fin_history",
                table: "gm_sell_request");

            migrationBuilder.DropIndex(
                name: "IX_gm_buy_request_ref_fin_history",
                table: "gm_buy_request");

            migrationBuilder.DropColumn(
                name: "ref_fin_history",
                table: "gm_sell_request");

            migrationBuilder.DropColumn(
                name: "ref_fin_history",
                table: "gm_buy_request");
        }
    }
}
