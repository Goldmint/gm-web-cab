using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class useractivitylocale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropForeignKey(
				name: "FK_gm_buy_gold_crypto_sup_gm_user_finhistory_ref_user_finhistory",
				table: "gm_buy_gold_crypto_sup");

			migrationBuilder.DropForeignKey(
				name: "FK_gm_buy_gold_request_gm_user_finhistory_ref_user_finhistory",
				table: "gm_buy_gold_request");

			migrationBuilder.DropForeignKey(
				name: "FK_gm_eth_operation_gm_user_finhistory_ref_user_finhistory",
				table: "gm_eth_operation");

			migrationBuilder.DropForeignKey(
                name: "FK_gm_sell_gold_crypto_sup_gm_user_finhistory_ref_user_finhistor",
                table: "gm_sell_gold_crypto_sup");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_sell_gold_request_gm_user_finhistory_ref_user_finhistory",
                table: "gm_sell_gold_request");

            migrationBuilder.RenameColumn(
                name: "ref_user_finhistory",
                table: "gm_sell_gold_request",
                newName: "rel_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_sell_gold_request_ref_user_finhistory",
                table: "gm_sell_gold_request",
                newName: "IX_gm_sell_gold_request_rel_user_finhistory");

            migrationBuilder.RenameColumn(
                name: "ref_user_finhistory",
                table: "gm_sell_gold_crypto_sup",
                newName: "rel_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_sell_gold_crypto_sup_ref_user_finhistory",
                table: "gm_sell_gold_crypto_sup",
                newName: "IX_gm_sell_gold_crypto_sup_rel_user_finhistory");

            migrationBuilder.RenameColumn(
                name: "ref_user_finhistory",
                table: "gm_eth_operation",
                newName: "rel_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_eth_operation_ref_user_finhistory",
                table: "gm_eth_operation",
                newName: "IX_gm_eth_operation_rel_user_finhistory");

            migrationBuilder.RenameColumn(
                name: "ref_user_finhistory",
                table: "gm_buy_gold_request",
                newName: "rel_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_buy_gold_request_ref_user_finhistory",
                table: "gm_buy_gold_request",
                newName: "IX_gm_buy_gold_request_rel_user_finhistory");

            migrationBuilder.RenameColumn(
                name: "ref_user_finhistory",
                table: "gm_buy_gold_crypto_sup",
                newName: "rel_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_buy_gold_crypto_sup_ref_user_finhistory",
                table: "gm_buy_gold_crypto_sup",
                newName: "IX_gm_buy_gold_crypto_sup_rel_user_finhistory");

            migrationBuilder.AddColumn<long>(
                name: "rel_user_activity",
                table: "gm_user_finhistory",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "locale",
                table: "gm_user_activity",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_finhistory_rel_user_activity",
                table: "gm_user_finhistory",
                column: "rel_user_activity");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_buy_gold_crypto_sup_gm_user_finhistory_rel_user_finhistory",
                table: "gm_buy_gold_crypto_sup",
                column: "rel_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_buy_gold_request_gm_user_finhistory_rel_user_finhistory",
                table: "gm_buy_gold_request",
                column: "rel_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_eth_operation_gm_user_finhistory_rel_user_finhistory",
                table: "gm_eth_operation",
                column: "rel_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_sell_gold_crypto_sup_gm_user_finhistory_rel_user_finhistory",
                table: "gm_sell_gold_crypto_sup",
                column: "rel_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_sell_gold_request_gm_user_finhistory_rel_user_finhistory",
                table: "gm_sell_gold_request",
                column: "rel_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_finhistory_gm_user_activity_rel_user_activity",
                table: "gm_user_finhistory",
                column: "rel_user_activity",
                principalTable: "gm_user_activity",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_buy_gold_crypto_sup_gm_user_finhistory_rel_user_finhistory",
                table: "gm_buy_gold_crypto_sup");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_buy_gold_request_gm_user_finhistory_rel_user_finhistory",
                table: "gm_buy_gold_request");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_eth_operation_gm_user_finhistory_rel_user_finhistory",
                table: "gm_eth_operation");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_sell_gold_crypto_sup_gm_user_finhistory_rel_user_finhistory",
                table: "gm_sell_gold_crypto_sup");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_sell_gold_request_gm_user_finhistory_rel_user_finhistory",
                table: "gm_sell_gold_request");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_finhistory_gm_user_activity_rel_user_activity",
                table: "gm_user_finhistory");

            migrationBuilder.DropIndex(
                name: "IX_gm_user_finhistory_rel_user_activity",
                table: "gm_user_finhistory");

            migrationBuilder.DropColumn(
                name: "rel_user_activity",
                table: "gm_user_finhistory");

            migrationBuilder.DropColumn(
                name: "locale",
                table: "gm_user_activity");

            migrationBuilder.RenameColumn(
                name: "rel_user_finhistory",
                table: "gm_sell_gold_request",
                newName: "ref_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_sell_gold_request_rel_user_finhistory",
                table: "gm_sell_gold_request",
                newName: "IX_gm_sell_gold_request_ref_user_finhistory");

            migrationBuilder.RenameColumn(
                name: "rel_user_finhistory",
                table: "gm_sell_gold_crypto_sup",
                newName: "ref_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_sell_gold_crypto_sup_rel_user_finhistory",
                table: "gm_sell_gold_crypto_sup",
                newName: "IX_gm_sell_gold_crypto_sup_ref_user_finhistory");

            migrationBuilder.RenameColumn(
                name: "rel_user_finhistory",
                table: "gm_eth_operation",
                newName: "ref_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_eth_operation_rel_user_finhistory",
                table: "gm_eth_operation",
                newName: "IX_gm_eth_operation_ref_user_finhistory");

            migrationBuilder.RenameColumn(
                name: "rel_user_finhistory",
                table: "gm_buy_gold_request",
                newName: "ref_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_buy_gold_request_rel_user_finhistory",
                table: "gm_buy_gold_request",
                newName: "IX_gm_buy_gold_request_ref_user_finhistory");

            migrationBuilder.RenameColumn(
                name: "rel_user_finhistory",
                table: "gm_buy_gold_crypto_sup",
                newName: "ref_user_finhistory");

            migrationBuilder.RenameIndex(
                name: "IX_gm_buy_gold_crypto_sup_rel_user_finhistory",
                table: "gm_buy_gold_crypto_sup",
                newName: "IX_gm_buy_gold_crypto_sup_ref_user_finhistory");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_buy_gold_crypto_sup_gm_user_finhistory_ref_user_finhistory",
                table: "gm_buy_gold_crypto_sup",
                column: "ref_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_buy_gold_request_gm_user_finhistory_ref_user_finhistory",
                table: "gm_buy_gold_request",
                column: "ref_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_eth_operation_gm_user_finhistory_ref_user_finhistory",
                table: "gm_eth_operation",
                column: "ref_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_sell_gold_crypto_sup_gm_user_finhistory_ref_user_finhistory",
                table: "gm_sell_gold_crypto_sup",
                column: "ref_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_sell_gold_request_gm_user_finhistory_ref_user_finhistory",
                table: "gm_sell_gold_request",
                column: "ref_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
