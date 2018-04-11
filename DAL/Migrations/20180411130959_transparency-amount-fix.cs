using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class transparencyamountfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "amount",
                table: "gm_transparency",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(long));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "amount",
                table: "gm_transparency",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
