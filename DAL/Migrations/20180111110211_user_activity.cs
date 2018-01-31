using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class user_activity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_user_activity",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    agent = table.Column<string>(maxLength: 128, nullable: false),
                    comment = table.Column<string>(maxLength: 512, nullable: false),
                    ip = table.Column<string>(maxLength: 32, nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    type = table.Column<string>(maxLength: 32, nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_activity", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_user_activity_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_activity_user_id",
                table: "gm_user_activity",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_user_activity");
        }
    }
}
