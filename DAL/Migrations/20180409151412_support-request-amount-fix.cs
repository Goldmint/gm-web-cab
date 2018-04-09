using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class supportrequestamountfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

	        migrationBuilder.Sql("delete from gm_sell_gold_crypto_sup where 1=1");
	        migrationBuilder.Sql("delete from gm_buy_gold_crypto_sup where 1=1");

			migrationBuilder.AddColumn<string>(
                name: "amount",
                table: "gm_sell_gold_crypto_sup",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "amount",
                table: "gm_buy_gold_crypto_sup",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amount",
                table: "gm_sell_gold_crypto_sup");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "gm_buy_gold_crypto_sup");
        }
    }
}
