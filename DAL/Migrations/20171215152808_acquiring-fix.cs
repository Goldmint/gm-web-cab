using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class acquiringfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gw_customer_id",
                table: "gm_card");

            migrationBuilder.DropColumn(
                name: "gw_init_transaction_id",
                table: "gm_card");

            migrationBuilder.AddColumn<string>(
                name: "gw_deposit_card_tid",
                table: "gm_card",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gw_withdraw_card_tid",
                table: "gm_card",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_code",
                table: "gm_card",
                maxLength: 8,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gw_deposit_card_tid",
                table: "gm_card");

            migrationBuilder.DropColumn(
                name: "gw_withdraw_card_tid",
                table: "gm_card");

            migrationBuilder.DropColumn(
                name: "verification_code",
                table: "gm_card");

            migrationBuilder.AddColumn<string>(
                name: "gw_customer_id",
                table: "gm_card",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "gw_init_transaction_id",
                table: "gm_card",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}
