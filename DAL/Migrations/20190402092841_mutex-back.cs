using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class mutexback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_mutex",
                columns: table => new
                {
                    id = table.Column<string>(maxLength: 64, nullable: false),
                    locker = table.Column<string>(maxLength: 32, nullable: false),
                    expires = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_mutex", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_mutex");
        }
    }
}
