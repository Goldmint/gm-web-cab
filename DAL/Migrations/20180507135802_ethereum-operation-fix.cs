using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class ethereumoperationfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.Sql("delete from gm_eth_operation where 1=1");

			migrationBuilder.AddColumn<string>(
                name: "gold_amount",
                table: "gm_eth_operation",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "rel_request_id",
                table: "gm_eth_operation",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gold_amount",
                table: "gm_eth_operation");

            migrationBuilder.DropColumn(
                name: "rel_request_id",
                table: "gm_eth_operation");
        }
    }
}
