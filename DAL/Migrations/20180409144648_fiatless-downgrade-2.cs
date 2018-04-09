using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class fiatlessdowngrade2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_issue_gold_tx");

            migrationBuilder.DropColumn(
                name: "destination",
                table: "gm_buy_gold_request");

            migrationBuilder.RenameColumn(
                name: "desk_ticket_id",
                table: "gm_user_finhistory",
                newName: "oplog_id");

            migrationBuilder.RenameColumn(
                name: "desk_ticket_id",
                table: "gm_transfer_gold_tx",
                newName: "oplog_id");

            migrationBuilder.RenameColumn(
                name: "destination_address",
                table: "gm_buy_gold_request",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "desk_ticket_id",
                table: "gm_buy_gold_request",
                newName: "oplog_id");

            migrationBuilder.AddColumn<int>(
                name: "output",
                table: "gm_buy_gold_request",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "gm_buy_gold_crypto_sup",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    buy_gold_request_id = table.Column<long>(nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    ref_user_finhistory = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    support_comment = table.Column<string>(maxLength: 512, nullable: true),
                    support_user_id = table.Column<long>(nullable: true),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_buy_gold_crypto_sup", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_crypto_sup_gm_buy_gold_request_buy_gold_request_id",
                        column: x => x.buy_gold_request_id,
                        principalTable: "gm_buy_gold_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_crypto_sup_gm_user_finhistory_ref_user_finhistory",
                        column: x => x.ref_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_crypto_sup_gm_user_support_user_id",
                        column: x => x.support_user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_crypto_sup_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_sell_gold_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    exchange_currency = table.Column<int>(nullable: false),
                    gold_rate = table.Column<long>(nullable: false),
                    input = table.Column<int>(nullable: false),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    output = table.Column<int>(nullable: false),
                    output_address = table.Column<string>(maxLength: 128, nullable: false),
                    output_rate = table.Column<long>(nullable: false),
                    ref_user_finhistory = table.Column<long>(nullable: false),
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
                        name: "FK_gm_sell_gold_request_gm_user_finhistory_ref_user_finhistory",
                        column: x => x.ref_user_finhistory,
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

            migrationBuilder.CreateTable(
                name: "gm_sell_gold_crypto_sup",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    ref_user_finhistory = table.Column<long>(nullable: false),
                    sell_gold_request_id = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    support_comment = table.Column<string>(maxLength: 512, nullable: true),
                    support_user_id = table.Column<long>(nullable: true),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_sell_gold_crypto_sup", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_sell_gold_crypto_sup_gm_user_finhistory_ref_user_finhistory",
                        column: x => x.ref_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_sell_gold_crypto_sup_gm_sell_gold_request_sell_gold_request_id",
                        column: x => x.sell_gold_request_id,
                        principalTable: "gm_sell_gold_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_sell_gold_crypto_sup_gm_user_support_user_id",
                        column: x => x.support_user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_sell_gold_crypto_sup_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_crypto_sup_buy_gold_request_id",
                table: "gm_buy_gold_crypto_sup",
                column: "buy_gold_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_crypto_sup_ref_user_finhistory",
                table: "gm_buy_gold_crypto_sup",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_crypto_sup_support_user_id",
                table: "gm_buy_gold_crypto_sup",
                column: "support_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_crypto_sup_user_id",
                table: "gm_buy_gold_crypto_sup",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_crypto_sup_ref_user_finhistory",
                table: "gm_sell_gold_crypto_sup",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_crypto_sup_sell_gold_request_id",
                table: "gm_sell_gold_crypto_sup",
                column: "sell_gold_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_crypto_sup_support_user_id",
                table: "gm_sell_gold_crypto_sup",
                column: "support_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_crypto_sup_user_id",
                table: "gm_sell_gold_crypto_sup",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_request_ref_user_finhistory",
                table: "gm_sell_gold_request",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_request_user_id",
                table: "gm_sell_gold_request",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_buy_gold_crypto_sup");

            migrationBuilder.DropTable(
                name: "gm_sell_gold_crypto_sup");

            migrationBuilder.DropTable(
                name: "gm_sell_gold_request");

            migrationBuilder.DropColumn(
                name: "output",
                table: "gm_buy_gold_request");

            migrationBuilder.RenameColumn(
                name: "oplog_id",
                table: "gm_user_finhistory",
                newName: "desk_ticket_id");

            migrationBuilder.RenameColumn(
                name: "oplog_id",
                table: "gm_transfer_gold_tx",
                newName: "desk_ticket_id");

            migrationBuilder.RenameColumn(
                name: "oplog_id",
                table: "gm_buy_gold_request",
                newName: "desk_ticket_id");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "gm_buy_gold_request",
                newName: "destination_address");

            migrationBuilder.AddColumn<int>(
                name: "destination",
                table: "gm_buy_gold_request",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "gm_issue_gold_tx",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    address = table.Column<string>(maxLength: 128, nullable: false),
                    eth_txid = table.Column<string>(maxLength: 66, nullable: true),
                    origin = table.Column<int>(nullable: false),
                    origin_id = table.Column<long>(nullable: false),
                    ref_user_finhistory = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_issue_gold_tx", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_issue_gold_tx_gm_user_finhistory_ref_user_finhistory",
                        column: x => x.ref_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_issue_gold_tx_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_issue_gold_tx_ref_user_finhistory",
                table: "gm_issue_gold_tx",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_issue_gold_tx_user_id",
                table: "gm_issue_gold_tx",
                column: "user_id");
        }
    }
}
