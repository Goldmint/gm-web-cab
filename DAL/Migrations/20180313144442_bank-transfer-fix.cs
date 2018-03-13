using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class banktransferfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

	        migrationBuilder.Sql("delete from gm_swift_request where 1=1");

			migrationBuilder.DropColumn(
                name: "ben_addr",
                table: "gm_swift_request");

            migrationBuilder.DropColumn(
                name: "ben_swift",
                table: "gm_swift_request");

            migrationBuilder.RenameColumn(
                name: "ben_name",
                table: "gm_swift_request",
                newName: "iban");

            migrationBuilder.RenameColumn(
                name: "ben_iban",
                table: "gm_swift_request",
                newName: "bic");

            migrationBuilder.RenameColumn(
                name: "ben_bank_name",
                table: "gm_swift_request",
                newName: "holder");

            migrationBuilder.RenameColumn(
                name: "ben_bank_addr",
                table: "gm_swift_request",
                newName: "holder_address");

            migrationBuilder.AddColumn<string>(
                name: "bank",
                table: "gm_swift_request",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "details",
                table: "gm_swift_request",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank",
                table: "gm_swift_request");

            migrationBuilder.DropColumn(
                name: "details",
                table: "gm_swift_request");

            migrationBuilder.RenameColumn(
                name: "iban",
                table: "gm_swift_request",
                newName: "ben_name");

            migrationBuilder.RenameColumn(
                name: "holder_address",
                table: "gm_swift_request",
                newName: "ben_bank_addr");

            migrationBuilder.RenameColumn(
                name: "holder",
                table: "gm_swift_request",
                newName: "ben_bank_name");

            migrationBuilder.RenameColumn(
                name: "bic",
                table: "gm_swift_request",
                newName: "ben_iban");

            migrationBuilder.AddColumn<string>(
                name: "ben_addr",
                table: "gm_swift_request",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ben_swift",
                table: "gm_swift_request",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}
