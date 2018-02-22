using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class jwtfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "access_stamp_web",
                table: "gm_user");

            migrationBuilder.AddColumn<string>(
                name: "jwt_salt",
                table: "gm_user",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

	        migrationBuilder.Sql("update gm_user set jwt_salt=1 where 1=1");
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "jwt_salt",
                table: "gm_user");

            migrationBuilder.AddColumn<string>(
                name: "access_stamp_web",
                table: "gm_user",
                maxLength: 64,
                nullable: true);
        }
    }
}
