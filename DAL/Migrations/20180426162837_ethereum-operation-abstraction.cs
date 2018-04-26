using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class ethereumoperationabstraction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_transfer_gold_tx");

            migrationBuilder.CreateTable(
                name: "gm_eth_operation",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    address = table.Column<string>(maxLength: 128, nullable: false),
                    eth_request_index = table.Column<string>(maxLength: 64, nullable: true),
                    eth_txid = table.Column<string>(maxLength: 66, nullable: true),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    rate = table.Column<string>(maxLength: 64, nullable: false),
                    ref_user_finhistory = table.Column<long>(nullable: false),
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
                        name: "FK_gm_eth_operation_gm_user_finhistory_ref_user_finhistory",
                        column: x => x.ref_user_finhistory,
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

            migrationBuilder.CreateIndex(
                name: "IX_gm_eth_operation_ref_user_finhistory",
                table: "gm_eth_operation",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_eth_operation_user_id",
                table: "gm_eth_operation",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_eth_operation");

            migrationBuilder.CreateTable(
                name: "gm_transfer_gold_tx",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    address = table.Column<string>(maxLength: 128, nullable: false),
                    eth_txid = table.Column<string>(maxLength: 66, nullable: true),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
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
                name: "IX_gm_transfer_gold_tx_ref_user_finhistory",
                table: "gm_transfer_gold_tx",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_transfer_gold_tx_user_id",
                table: "gm_transfer_gold_tx",
                column: "user_id");
        }
    }
}
