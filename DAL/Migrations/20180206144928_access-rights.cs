using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class accessrights : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "asp_security_stamp",
                table: "gm_user",
                newName: "security_stamp");

            migrationBuilder.RenameColumn(
                name: "access_stamp",
                table: "gm_user",
                newName: "access_stamp_web");

            migrationBuilder.AddColumn<long>(
                name: "access_rights",
                table: "gm_user",
                nullable: false,
                defaultValue: 0L);

			migrationBuilder.Sql("update gm_user set access_rights=1 where 1=1");
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "access_rights",
                table: "gm_user");

            migrationBuilder.RenameColumn(
                name: "security_stamp",
                table: "gm_user",
                newName: "asp_security_stamp");

            migrationBuilder.RenameColumn(
                name: "access_stamp_web",
                table: "gm_user",
                newName: "access_stamp");
        }
    }
}
