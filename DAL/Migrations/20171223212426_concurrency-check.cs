using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class concurrencycheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "gm_user_verification",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "gm_user_options",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "gw_transaction_id",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "gm_card",
                maxLength: 32,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "gm_user_verification");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "gm_card");

            migrationBuilder.AlterColumn<string>(
                name: "gw_transaction_id",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64);
        }
    }
}
