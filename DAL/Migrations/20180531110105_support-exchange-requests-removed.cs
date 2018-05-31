using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class supportexchangerequestsremoved : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_buy_gold_crypto_sup");

            migrationBuilder.DropTable(
                name: "gm_sell_gold_crypto_sup");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_buy_gold_crypto_sup",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<string>(maxLength: 64, nullable: false),
                    buy_gold_request_id = table.Column<long>(nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    rel_user_finhistory = table.Column<long>(nullable: false),
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
                        name: "FK_gm_buy_gold_crypto_sup_gm_user_finhistory_rel_user_finhistory",
                        column: x => x.rel_user_finhistory,
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
                name: "gm_sell_gold_crypto_sup",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    rel_user_finhistory = table.Column<long>(nullable: false),
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
                        name: "FK_gm_sell_gold_crypto_sup_gm_user_finhistory_rel_user_finhistory",
                        column: x => x.rel_user_finhistory,
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
                name: "IX_gm_buy_gold_crypto_sup_rel_user_finhistory",
                table: "gm_buy_gold_crypto_sup",
                column: "rel_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_crypto_sup_support_user_id",
                table: "gm_buy_gold_crypto_sup",
                column: "support_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_crypto_sup_user_id",
                table: "gm_buy_gold_crypto_sup",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_gold_crypto_sup_rel_user_finhistory",
                table: "gm_sell_gold_crypto_sup",
                column: "rel_user_finhistory");

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
        }
    }
}
