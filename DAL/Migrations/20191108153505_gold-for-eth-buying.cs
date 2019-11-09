using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class goldforethbuying : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_eth_sending_gm_user_finhistory_rel_user_finhistory",
                table: "gm_eth_sending");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_sell_gold_eth_gm_user_finhistory_rel_user_finhistory",
                table: "gm_sell_gold_eth");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_finhistory_gm_user_activity_rel_user_activity",
                table: "gm_user_finhistory");

            migrationBuilder.DropIndex(
                name: "IX_gm_user_finhistory_rel_user_activity",
                table: "gm_user_finhistory");

            migrationBuilder.DropIndex(
                name: "IX_gm_sell_gold_eth_rel_user_finhistory",
                table: "gm_sell_gold_eth");

            migrationBuilder.DropIndex(
                name: "IX_gm_eth_sending_rel_user_finhistory",
                table: "gm_eth_sending");

            migrationBuilder.DropColumn(
                name: "rel_eth_transaction_id",
                table: "gm_user_finhistory");

            migrationBuilder.DropColumn(
                name: "rel_user_activity",
                table: "gm_user_finhistory");

            migrationBuilder.DropColumn(
                name: "time_completed",
                table: "gm_user_finhistory");

            migrationBuilder.DropColumn(
                name: "time_expires",
                table: "gm_user_finhistory");

            migrationBuilder.DropColumn(
                name: "rel_user_finhistory",
                table: "gm_sell_gold_eth");

            migrationBuilder.DropColumn(
                name: "rel_user_finhistory",
                table: "gm_eth_sending");

            migrationBuilder.AddColumn<long>(
                name: "rel_finhistory_id",
                table: "gm_sell_gold_eth",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "rel_finhistory_id",
                table: "gm_eth_sending",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "gm_buy_gold_eth",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<long>(nullable: false),
                    rel_finhistory_id = table.Column<long>(nullable: true),
                    status = table.Column<int>(nullable: false),
                    exchange_currency = table.Column<int>(nullable: false),
                    gold_rate = table.Column<long>(nullable: false),
                    eth_rate = table.Column<long>(nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_buy_gold_eth", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_eth_gm_user_finhistory_rel_finhistory_id",
                        column: x => x.rel_finhistory_id,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_eth_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_withdraw_gold",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<long>(nullable: false),
                    rel_finhistory_id = table.Column<long>(nullable: true),
                    status = table.Column<int>(nullable: false),
                    sum_address = table.Column<string>(maxLength: 128, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(38, 18)", nullable: false),
                    sum_txid = table.Column<string>(maxLength: 128, nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_withdraw_gold", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_withdraw_gold_gm_user_finhistory_rel_finhistory_id",
                        column: x => x.rel_finhistory_id,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_withdraw_gold_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_eth_rel_finhistory_id",
                table: "gm_sell_gold_eth",
                column: "rel_finhistory_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_eth_sending_rel_finhistory_id",
                table: "gm_eth_sending",
                column: "rel_finhistory_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_eth_rel_finhistory_id",
                table: "gm_buy_gold_eth",
                column: "rel_finhistory_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_eth_user_id",
                table: "gm_buy_gold_eth",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_withdraw_gold_rel_finhistory_id",
                table: "gm_withdraw_gold",
                column: "rel_finhistory_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_withdraw_gold_user_id",
                table: "gm_withdraw_gold",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_eth_sending_gm_user_finhistory_rel_finhistory_id",
                table: "gm_eth_sending",
                column: "rel_finhistory_id",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_sell_gold_eth_gm_user_finhistory_rel_finhistory_id",
                table: "gm_sell_gold_eth",
                column: "rel_finhistory_id",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_eth_sending_gm_user_finhistory_rel_finhistory_id",
                table: "gm_eth_sending");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_sell_gold_eth_gm_user_finhistory_rel_finhistory_id",
                table: "gm_sell_gold_eth");

            migrationBuilder.DropTable(
                name: "gm_buy_gold_eth");

            migrationBuilder.DropTable(
                name: "gm_withdraw_gold");

            migrationBuilder.DropIndex(
                name: "IX_gm_sell_gold_eth_rel_finhistory_id",
                table: "gm_sell_gold_eth");

            migrationBuilder.DropIndex(
                name: "IX_gm_eth_sending_rel_finhistory_id",
                table: "gm_eth_sending");

            migrationBuilder.DropColumn(
                name: "rel_finhistory_id",
                table: "gm_sell_gold_eth");

            migrationBuilder.DropColumn(
                name: "rel_finhistory_id",
                table: "gm_eth_sending");

            migrationBuilder.AddColumn<string>(
                name: "rel_eth_transaction_id",
                table: "gm_user_finhistory",
                maxLength: 66,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "rel_user_activity",
                table: "gm_user_finhistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "time_completed",
                table: "gm_user_finhistory",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "time_expires",
                table: "gm_user_finhistory",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "rel_user_finhistory",
                table: "gm_sell_gold_eth",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "rel_user_finhistory",
                table: "gm_eth_sending",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_finhistory_rel_user_activity",
                table: "gm_user_finhistory",
                column: "rel_user_activity");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_eth_rel_user_finhistory",
                table: "gm_sell_gold_eth",
                column: "rel_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_eth_sending_rel_user_finhistory",
                table: "gm_eth_sending",
                column: "rel_user_finhistory");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_eth_sending_gm_user_finhistory_rel_user_finhistory",
                table: "gm_eth_sending",
                column: "rel_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_sell_gold_eth_gm_user_finhistory_rel_user_finhistory",
                table: "gm_sell_gold_eth",
                column: "rel_user_finhistory",
                principalTable: "gm_user_finhistory",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_finhistory_gm_user_activity_rel_user_activity",
                table: "gm_user_finhistory",
                column: "rel_user_activity",
                principalTable: "gm_user_activity",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
