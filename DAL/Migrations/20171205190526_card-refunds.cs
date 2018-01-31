using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class cardrefunds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "finalized",
                table: "gm_card_payment");

            migrationBuilder.AlterColumn<string>(
                name: "gw_transaction_id",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64);

            migrationBuilder.AddColumn<long>(
                name: "ref_card_payment",
                table: "gm_card_payment",
                nullable: true);

			migrationBuilder.DropIndex(
				name: "GWTransactionIdIndex",
				table: "gm_card_payment"
				);
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ref_card_payment",
                table: "gm_card_payment");

            migrationBuilder.AlterColumn<string>(
                name: "gw_transaction_id",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "finalized",
                table: "gm_card_payment",
                nullable: false,
                defaultValue: false);

			migrationBuilder.CreateIndex(
				name: "GWTransactionIdIndex",
				table: "gm_card_payment",
				column: "gw_transaction_id",
				unique: true);
		}
    }
}
