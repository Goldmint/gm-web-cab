using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class buygoldfiat2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_buy_gold_fiat",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<long>(nullable: false),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    rel_user_finhistory = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    gold_amount = table.Column<decimal>(type: "decimal(38, 18)", nullable: false),
                    destination = table.Column<string>(maxLength: 128, nullable: false),
                    exchange_currency = table.Column<int>(nullable: false),
                    gold_rate = table.Column<long>(nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_buy_gold_fiat", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_fiat_gm_user_finhistory_rel_user_finhistory",
                        column: x => x.rel_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_fiat_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_fiat_rel_user_finhistory",
                table: "gm_buy_gold_fiat",
                column: "rel_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_fiat_user_id",
                table: "gm_buy_gold_fiat",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_buy_gold_fiat");
        }
    }
}
