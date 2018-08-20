using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class userlimits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_user_limits",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    eth_deposited = table.Column<decimal>(type: "decimal(38, 18)", nullable: false),
                    eth_withdrawn = table.Column<decimal>(type: "decimal(38, 18)", nullable: false),
                    fiat_deposited = table.Column<long>(nullable: false),
                    fiat_withdrawn = table.Column<long>(nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_limits", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_user_limits_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
	                table.UniqueConstraint("UK_gm_user_limits_date", x => new { x.user_id, x.time_created });
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_limits_user_id",
                table: "gm_user_limits",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_user_limits");
        }
    }
}
