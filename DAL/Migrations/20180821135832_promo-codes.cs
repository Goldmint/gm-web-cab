using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class promocodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "gm_user_limits");

            migrationBuilder.CreateTable(
                name: "gm_promo_code",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(maxLength: 32, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_expires = table.Column<DateTime>(nullable: false),
                    time_used = table.Column<DateTime>(nullable: true),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: true),
                    value = table.Column<decimal>(type: "decimal(38, 18)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_promo_code", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_promo_code_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
	                table.UniqueConstraint("UK_gm_promo_code_code", x => x.code);
				});

            migrationBuilder.CreateIndex(
                name: "IX_gm_promo_code_user_id",
                table: "gm_promo_code",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_promo_code");

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "gm_user_limits",
                maxLength: 64,
                nullable: true);
        }
    }
}
