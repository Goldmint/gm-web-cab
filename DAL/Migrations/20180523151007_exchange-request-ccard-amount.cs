using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class exchangerequestccardamount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "output_address",
                table: "gm_sell_gold_request",
                newName: "eth_address");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "gm_buy_gold_request",
                newName: "eth_address");

            migrationBuilder.AddColumn<string>(
                name: "input_expected",
                table: "gm_sell_gold_request",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "rel_output_id",
                table: "gm_sell_gold_request",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "rel_request_id",
                table: "gm_ccard_payment",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "input_expected",
                table: "gm_buy_gold_request",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "rel_input_id",
                table: "gm_buy_gold_request",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "input_expected",
                table: "gm_sell_gold_request");

            migrationBuilder.DropColumn(
                name: "rel_output_id",
                table: "gm_sell_gold_request");

            migrationBuilder.DropColumn(
                name: "rel_request_id",
                table: "gm_ccard_payment");

            migrationBuilder.DropColumn(
                name: "input_expected",
                table: "gm_buy_gold_request");

            migrationBuilder.DropColumn(
                name: "rel_input_id",
                table: "gm_buy_gold_request");

            migrationBuilder.RenameColumn(
                name: "eth_address",
                table: "gm_sell_gold_request",
                newName: "output_address");

            migrationBuilder.RenameColumn(
                name: "eth_address",
                table: "gm_buy_gold_request",
                newName: "address");
        }
    }
}
