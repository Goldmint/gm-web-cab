using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class addmultiusedpromo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_promo_code_gm_user_user_id",
                table: "gm_promo_code");

            migrationBuilder.DropIndex(
                name: "IX_gm_promo_code_user_id",
                table: "gm_promo_code");

            migrationBuilder.DropColumn(
                name: "time_used",
                table: "gm_promo_code");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "gm_promo_code");

            migrationBuilder.CreateTable(
                name: "gm_used_promo_codes",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(maxLength: 32, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    promo_code_id = table.Column<long>(nullable: false),
                    time_used = table.Column<DateTime>(nullable: true),
                    usage_type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_used_promo_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_used_promo_codes_gm_promo_code_promo_code_id",
                        column: x => x.promo_code_id,
                        principalTable: "gm_promo_code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_used_promo_codes_promo_code_id",
                table: "gm_used_promo_codes",
                column: "promo_code_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_used_promo_codes");

            migrationBuilder.AddColumn<DateTime>(
                name: "time_used",
                table: "gm_promo_code",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "user_id",
                table: "gm_promo_code",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_promo_code_user_id",
                table: "gm_promo_code",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_promo_code_gm_user_user_id",
                table: "gm_promo_code",
                column: "user_id",
                principalTable: "gm_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
