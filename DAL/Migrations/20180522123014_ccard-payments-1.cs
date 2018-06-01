using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class ccardpayments1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_user_ccard",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    mask = table.Column<string>(maxLength: 64, nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 32, nullable: true),
                    gw_deposit_card_tid = table.Column<string>(maxLength: 64, nullable: true),
                    gw_withdraw_card_tid = table.Column<string>(maxLength: 64, nullable: true),
                    holder_name = table.Column<string>(maxLength: 128, nullable: true),
                    state = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false),
                    verification_amount = table.Column<long>(nullable: false),
                    verification_attempt = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_ccard", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_user_ccard_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_ccard_payment",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(nullable: false),
                    card_id = table.Column<long>(nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 32, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    gw_transaction_id = table.Column<string>(maxLength: 64, nullable: false),
                    oplog_id = table.Column<string>(maxLength: 32, nullable: false),
                    provider_message = table.Column<string>(maxLength: 512, nullable: true),
                    provider_status = table.Column<string>(maxLength: 64, nullable: true),
                    rel_payment_id = table.Column<long>(nullable: true),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    transaction_id = table.Column<string>(maxLength: 32, nullable: false),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_ccard_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_ccard_payment_gm_user_ccard_card_id",
                        column: x => x.card_id,
                        principalTable: "gm_user_ccard",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_ccard_payment_gm_ccard_payment_rel_payment_id",
                        column: x => x.rel_payment_id,
                        principalTable: "gm_ccard_payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_ccard_payment_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_ccard_payment_card_id",
                table: "gm_ccard_payment",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_ccard_payment_rel_payment_id",
                table: "gm_ccard_payment",
                column: "rel_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_ccard_payment_user_id",
                table: "gm_ccard_payment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_ccard_user_id",
                table: "gm_user_ccard",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_ccard_payment");

            migrationBuilder.DropTable(
                name: "gm_user_ccard");
        }
    }
}
