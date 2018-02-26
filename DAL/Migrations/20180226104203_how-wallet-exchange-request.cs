using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class howwalletexchangerequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "gm_sell_request",
                nullable: false,
                defaultValue: 0);

	        migrationBuilder.Sql("update gm_sell_request set type=2 where 1=1");

			migrationBuilder.AddColumn<int>(
                name: "type",
                table: "gm_buy_request",
                nullable: false,
                defaultValue: 0);

	        migrationBuilder.Sql("update gm_buy_request set type=2 where 1=1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "gm_sell_request");

            migrationBuilder.DropColumn(
                name: "type",
                table: "gm_buy_request");
        }
    }
}
