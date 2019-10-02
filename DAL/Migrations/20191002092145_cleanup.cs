using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class cleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_options_gm_signed_document_dpa_document_id",
                table: "gm_user_options");

            migrationBuilder.DropTable(
                name: "gm_ccard_payment");

            migrationBuilder.DropTable(
                name: "gm_mutex");

            migrationBuilder.DropTable(
                name: "gm_signed_document");

            migrationBuilder.DropTable(
                name: "gm_used_promo_codes");

            migrationBuilder.DropTable(
                name: "gm_user_oplog");

            migrationBuilder.DropTable(
                name: "gm_user_ccard");

            migrationBuilder.DropTable(
                name: "gm_promo_code");

            migrationBuilder.DropIndex(
                name: "IX_gm_user_options_dpa_document_id",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "dpa_document_id",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "oplog_id",
                table: "gm_user_finhistory");

            migrationBuilder.DropColumn(
                name: "access_rights",
                table: "gm_user");

            migrationBuilder.DropColumn(
                name: "jwt_salt_dbr",
                table: "gm_user");

            migrationBuilder.DropColumn(
                name: "oplog_id",
                table: "gm_sell_gold_eth");

            migrationBuilder.DropColumn(
                name: "oplog_id",
                table: "gm_eth_sending");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "dpa_document_id",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "oplog_id",
                table: "gm_user_finhistory",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "access_rights",
                table: "gm_user",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "jwt_salt_dbr",
                table: "gm_user",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "oplog_id",
                table: "gm_sell_gold_eth",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "oplog_id",
                table: "gm_eth_sending",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

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
                name: "gm_promo_code",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(maxLength: 32, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    token_type = table.Column<int>(nullable: false),
                    discount_value = table.Column<double>(nullable: false),
                    limit = table.Column<decimal>(type: "decimal(38, 18)", nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_expires = table.Column<DateTime>(nullable: false),
                    usage_type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_promo_code", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gm_signed_document",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    callback_event_type = table.Column<string>(maxLength: 64, nullable: true),
                    callback_status = table.Column<string>(maxLength: 16, nullable: true),
                    is_signed = table.Column<bool>(nullable: false),
                    reference_id = table.Column<string>(maxLength: 32, nullable: false),
                    secret = table.Column<string>(maxLength: 64, nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_signed_document", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_signed_document_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "gm_user_oplog",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    message = table.Column<string>(maxLength: 512, nullable: false),
                    ref_id = table.Column<long>(nullable: true),
                    status = table.Column<int>(nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_oplog", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_user_oplog_gm_user_oplog_ref_id",
                        column: x => x.ref_id,
                        principalTable: "gm_user_oplog",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_user_oplog_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_used_promo_codes",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    promo_code_id = table.Column<long>(nullable: false),
                    time_used = table.Column<DateTime>(nullable: true),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_used_promo_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_used_promo_codes_gm_promo_code_promo_code_id",
                        column: x => x.promo_code_id,
                        principalTable: "gm_promo_code",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_used_promo_codes_gm_user_user_id",
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
                    rel_request_id = table.Column<long>(nullable: true),
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
                name: "IX_gm_user_options_dpa_document_id",
                table: "gm_user_options",
                column: "dpa_document_id");

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
                name: "IX_gm_signed_document_user_id",
                table: "gm_signed_document",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_used_promo_codes_promo_code_id",
                table: "gm_used_promo_codes",
                column: "promo_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_used_promo_codes_user_id",
                table: "gm_used_promo_codes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_ccard_user_id",
                table: "gm_user_ccard",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_oplog_ref_id",
                table: "gm_user_oplog",
                column: "ref_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_oplog_user_id",
                table: "gm_user_oplog",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_options_gm_signed_document_dpa_document_id",
                table: "gm_user_options",
                column: "dpa_document_id",
                principalTable: "gm_signed_document",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
