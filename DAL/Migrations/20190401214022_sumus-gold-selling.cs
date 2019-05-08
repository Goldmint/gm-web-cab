using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class sumusgoldselling : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_buy_gold_request");

            migrationBuilder.DropTable(
                name: "gm_eth_operation");

            migrationBuilder.DropTable(
                name: "gm_mutex");

            migrationBuilder.DropTable(
                name: "gm_sell_gold_request");

            migrationBuilder.CreateTable(
                name: "gm_eth_sending",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<long>(nullable: false),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    rel_user_finhistory = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    address = table.Column<string>(maxLength: 128, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(38, 18)", nullable: false),
                    tx = table.Column<string>(maxLength: 66, nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_eth_sending", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_eth_sending_gm_user_finhistory_rel_user_finhistory",
                        column: x => x.rel_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_eth_sending_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_sell_gold_eth",
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
                    eth_amount = table.Column<decimal>(type: "decimal(38, 18)", nullable: false),
                    exchange_currency = table.Column<int>(nullable: false),
                    gold_rate = table.Column<long>(nullable: false),
                    eth_rate = table.Column<long>(nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_sell_gold_eth", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_sell_gold_eth_gm_user_finhistory_rel_user_finhistory",
                        column: x => x.rel_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_sell_gold_eth_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_eth_sending_rel_user_finhistory",
                table: "gm_eth_sending",
                column: "rel_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_eth_sending_user_id",
                table: "gm_eth_sending",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_eth_rel_user_finhistory",
                table: "gm_sell_gold_eth",
                column: "rel_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_eth_user_id",
                table: "gm_sell_gold_eth",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_eth_sending");

            migrationBuilder.DropTable(
                name: "gm_sell_gold_eth");

            migrationBuilder.CreateTable(
                name: "gm_buy_gold_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    eth_address = table.Column<string>(maxLength: 128, nullable: false),
                    exchange_currency = table.Column<int>(nullable: false),
                    gold_rate = table.Column<long>(nullable: false),
                    input = table.Column<int>(nullable: false),
                    input_expected = table.Column<string>(maxLength: 64, nullable: false),
                    input_rate = table.Column<long>(nullable: false),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    output = table.Column<int>(nullable: false),
                    promo_code_id = table.Column<long>(nullable: true),
                    rel_input_id = table.Column<long>(nullable: true),
                    rel_user_finhistory = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_expires = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_buy_gold_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_request_gm_promo_code_promo_code_id",
                        column: x => x.promo_code_id,
                        principalTable: "gm_promo_code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_request_gm_user_finhistory_rel_user_finhistory",
                        column: x => x.rel_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_request_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_eth_operation",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    cents_amount = table.Column<long>(nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    address = table.Column<string>(maxLength: 128, nullable: false),
                    discount = table.Column<double>(nullable: false),
                    eth_request_index = table.Column<string>(maxLength: 64, nullable: true),
                    eth_txid = table.Column<string>(maxLength: 66, nullable: true),
                    gold_amount = table.Column<string>(maxLength: 64, nullable: false),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    rate = table.Column<string>(maxLength: 64, nullable: false),
                    rel_user_finhistory = table.Column<long>(nullable: false),
                    rel_request_id = table.Column<long>(nullable: true),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_eth_operation", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_eth_operation_gm_user_finhistory_rel_user_finhistory",
                        column: x => x.rel_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_eth_operation_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_mutex",
                columns: table => new
                {
                    id = table.Column<string>(maxLength: 64, nullable: false),
                    expires = table.Column<DateTime>(nullable: false),
                    locker = table.Column<string>(maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_mutex", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gm_sell_gold_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    eth_address = table.Column<string>(maxLength: 128, nullable: false),
                    exchange_currency = table.Column<int>(nullable: false),
                    gold_rate = table.Column<long>(nullable: false),
                    input = table.Column<int>(nullable: false),
                    input_expected = table.Column<string>(maxLength: 64, nullable: false),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    output = table.Column<int>(nullable: false),
                    output_rate = table.Column<long>(nullable: false),
                    rel_output_id = table.Column<long>(nullable: true),
                    rel_user_finhistory = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_expires = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_sell_gold_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_sell_gold_request_gm_user_finhistory_rel_user_finhistory",
                        column: x => x.rel_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_sell_gold_request_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_request_promo_code_id",
                table: "gm_buy_gold_request",
                column: "promo_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_request_rel_user_finhistory",
                table: "gm_buy_gold_request",
                column: "rel_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_request_user_id",
                table: "gm_buy_gold_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_eth_operation_rel_user_finhistory",
                table: "gm_eth_operation",
                column: "rel_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_eth_operation_user_id",
                table: "gm_eth_operation",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_request_rel_user_finhistory",
                table: "gm_sell_gold_request",
                column: "rel_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_request_user_id",
                table: "gm_sell_gold_request",
                column: "user_id");
        }
    }
}
