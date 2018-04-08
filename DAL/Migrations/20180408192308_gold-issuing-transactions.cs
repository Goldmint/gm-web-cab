using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class goldissuingtransactions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_trans_gold_request");

            migrationBuilder.AlterColumn<string>(
                name: "hash",
                table: "gm_transparency",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "destination_address",
                table: "gm_buy_gold_request",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 256);

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

            migrationBuilder.CreateTable(
                name: "gm_transfer_gold_tx",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    address = table.Column<string>(maxLength: 128, nullable: false),
                    eth_txid = table.Column<string>(maxLength: 66, nullable: true),
                    ref_user_finhistory = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_transfer_gold_tx", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_transfer_gold_tx_gm_user_finhistory_ref_user_finhistory",
                        column: x => x.ref_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_transfer_gold_tx_gm_user_user_id",
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

            migrationBuilder.CreateIndex(
                name: "IX_gm_transfer_gold_tx_ref_user_finhistory",
                table: "gm_transfer_gold_tx",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_transfer_gold_tx_user_id",
                table: "gm_transfer_gold_tx",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_issue_gold_tx");

            migrationBuilder.DropTable(
                name: "gm_transfer_gold_tx");

            migrationBuilder.AlterColumn<string>(
                name: "hash",
                table: "gm_transparency",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "destination_address",
                table: "gm_buy_gold_request",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 128);

            migrationBuilder.CreateTable(
                name: "gm_trans_gold_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount_wei = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    address = table.Column<string>(maxLength: 256, nullable: false),
                    eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
                    ref_user_finhistory = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_trans_gold_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_trans_gold_request_gm_user_finhistory_ref_user_finhistory",
                        column: x => x.ref_user_finhistory,
                        principalTable: "gm_user_finhistory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_trans_gold_request_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_trans_gold_request_ref_user_finhistory",
                table: "gm_trans_gold_request",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_trans_gold_request_user_id",
                table: "gm_trans_gold_request",
                column: "user_id");
        }
    }
}
