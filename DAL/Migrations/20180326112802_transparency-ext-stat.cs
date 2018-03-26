using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class transparencyextstat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_transparency_stat",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    assets = table.Column<string>(maxLength: 2048, nullable: false),
                    audit_timestamp = table.Column<DateTime>(nullable: false),
                    bonds = table.Column<string>(maxLength: 2048, nullable: false),
                    data_timestamp = table.Column<DateTime>(nullable: false),
                    fiat = table.Column<string>(maxLength: 2048, nullable: false),
                    gold = table.Column<string>(maxLength: 2048, nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_transparency_stat", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_transparency_stat_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_transparency_stat_user_id",
                table: "gm_transparency_stat",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_transparency_stat");
        }
    }
}
