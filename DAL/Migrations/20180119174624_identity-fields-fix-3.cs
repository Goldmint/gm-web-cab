using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class identityfieldsfix3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "security_stamp",
                table: "gm_user",
                newName: "asp_security_stamp");

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                table: "gm_user_verification",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldRowVersion: true,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                table: "gm_user_options",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldRowVersion: true,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                table: "gm_user",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 36,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AddColumn<string>(
                name: "access_stamp",
                table: "gm_user",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                table: "gm_role",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                table: "gm_card",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldRowVersion: true,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "access_stamp",
                table: "gm_user");

            migrationBuilder.RenameColumn(
                name: "asp_security_stamp",
                table: "gm_user",
                newName: "security_stamp");

            migrationBuilder.AlterColumn<byte[]>(
                name: "concurrency_stamp",
                table: "gm_user_verification",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "concurrency_stamp",
                table: "gm_user_options",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                table: "gm_user",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                table: "gm_role",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "concurrency_stamp",
                table: "gm_card",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
