using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class dbsettingsvaluefieldsize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "value",
                table: "gm_settings",
                maxLength: 16384,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 1024,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "value",
                table: "gm_settings",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 16384,
                oldNullable: true);
        }
    }
}
