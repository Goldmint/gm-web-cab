using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class finstatethtxidfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "rel_eth_transaction_id",
                table: "gm_financial_history",
                maxLength: 66,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "rel_eth_transaction_id",
                table: "gm_financial_history",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 66,
                oldNullable: true);
        }
    }
}
