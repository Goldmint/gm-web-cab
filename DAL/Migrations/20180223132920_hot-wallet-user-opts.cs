using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class hotwalletuseropts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "hw_buying_stamp",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "hw_selling_stamp",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "hw_transfer_stamp",
                table: "gm_user_options",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hw_buying_stamp",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "hw_selling_stamp",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "hw_transfer_stamp",
                table: "gm_user_options");
        }
    }
}
