using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class cardpaymenttype : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deposit",
                table: "gm_card_payment");

            migrationBuilder.AddColumn<string>(
                name: "desk_ticket_id",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "time_next_check",
                table: "gm_card_payment",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte>(
                name: "type",
                table: "gm_card_payment",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "desk_ticket_id",
                table: "gm_card_payment");

            migrationBuilder.DropColumn(
                name: "time_next_check",
                table: "gm_card_payment");

            migrationBuilder.DropColumn(
                name: "type",
                table: "gm_card_payment");

            migrationBuilder.AddColumn<bool>(
                name: "is_deposit",
                table: "gm_card_payment",
                nullable: false,
                defaultValue: false);
        }
    }
}
