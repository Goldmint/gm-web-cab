using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class cardverificationcents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verification_code",
                table: "gm_card");

            migrationBuilder.AddColumn<long>(
                name: "verification_amount",
                table: "gm_card",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "verification_attempt",
                table: "gm_card",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "verification_amount",
                table: "gm_card");

            migrationBuilder.DropColumn(
                name: "verification_attempt",
                table: "gm_card");

            migrationBuilder.AddColumn<string>(
                name: "verification_code",
                table: "gm_card",
                maxLength: 8,
                nullable: false,
                defaultValue: "");
        }
    }
}
