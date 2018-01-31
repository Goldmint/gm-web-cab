using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class cardpaymentfix2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "dest_id",
                table: "gm_withdraw",
                newName: "destination_id");

            migrationBuilder.RenameColumn(
                name: "dest",
                table: "gm_withdraw",
                newName: "destination");

            migrationBuilder.AddColumn<int>(
                name: "ref_entity",
                table: "gm_card_payment",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ref_entity",
                table: "gm_card_payment");

            migrationBuilder.RenameColumn(
                name: "destination_id",
                table: "gm_withdraw",
                newName: "dest_id");

            migrationBuilder.RenameColumn(
                name: "destination",
                table: "gm_withdraw",
                newName: "dest");
        }
    }
}
