using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class swifttemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_swift_template",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    bank = table.Column<string>(maxLength: 256, nullable: false),
                    bic = table.Column<string>(maxLength: 128, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    details = table.Column<string>(maxLength: 1024, nullable: false),
                    holder = table.Column<string>(maxLength: 256, nullable: false),
                    iban = table.Column<string>(maxLength: 256, nullable: false),
                    name = table.Column<string>(maxLength: 64, nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_swift_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_swift_template_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_swift_template_user_id",
                table: "gm_swift_template",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_swift_template");
        }
    }
}
