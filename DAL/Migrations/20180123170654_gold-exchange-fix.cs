using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class goldexchangefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "time_requested",
                table: "gm_sell_request",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "time_requested",
                table: "gm_buy_request",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "time_requested",
                table: "gm_sell_request");

            migrationBuilder.DropColumn(
                name: "time_requested",
                table: "gm_buy_request");
        }
    }
}
