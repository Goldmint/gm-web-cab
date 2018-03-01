using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class hwtransferfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency",
                table: "gm_financial_history");

            migrationBuilder.AddColumn<long>(
                name: "ref_fin_history",
                table: "gm_transfer_request",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_gm_transfer_request_ref_fin_history",
                table: "gm_transfer_request",
                column: "ref_fin_history");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_transfer_request_gm_financial_history_ref_fin_history",
                table: "gm_transfer_request",
                column: "ref_fin_history",
                principalTable: "gm_financial_history",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_transfer_request_gm_financial_history_ref_fin_history",
                table: "gm_transfer_request");

            migrationBuilder.DropIndex(
                name: "IX_gm_transfer_request_ref_fin_history",
                table: "gm_transfer_request");

            migrationBuilder.DropColumn(
                name: "ref_fin_history",
                table: "gm_transfer_request");

            migrationBuilder.AddColumn<int>(
                name: "currency",
                table: "gm_financial_history",
                nullable: false,
                defaultValue: 0);
        }
    }
}
