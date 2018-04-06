using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class fiatlessdowngrade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_buy_request");

            migrationBuilder.DropTable(
                name: "gm_card_payment");

            migrationBuilder.DropTable(
                name: "gm_crypto_deposit");

            migrationBuilder.DropTable(
                name: "gm_deposit");

            migrationBuilder.DropTable(
                name: "gm_mutex");

            migrationBuilder.DropTable(
                name: "gm_sell_request");

            migrationBuilder.DropTable(
                name: "gm_swift_request");

            migrationBuilder.DropTable(
                name: "gm_swift_template");

            migrationBuilder.DropTable(
                name: "gm_transfer_request");

            migrationBuilder.DropTable(
                name: "gm_withdraw");

            migrationBuilder.DropTable(
                name: "gm_card");

            migrationBuilder.DropTable(
                name: "gm_financial_history");

            migrationBuilder.DropColumn(
                name: "crypto_deposit_stamp",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "crypto_withdraw_stamp",
                table: "gm_user_options");

            migrationBuilder.AlterColumn<string>(
                name: "hash",
                table: "gm_transparency",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 128);
				
            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "gm_banned_country",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 128);

            migrationBuilder.CreateTable(
                name: "gm_user_finhistory",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    comment = table.Column<string>(maxLength: 512, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    destination = table.Column<string>(maxLength: 128, nullable: true),
                    rel_eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
                    source = table.Column<string>(maxLength: 128, nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_expires = table.Column<DateTime>(nullable: true),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_finhistory", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_user_finhistory_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_buy_gold_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    destination = table.Column<int>(nullable: false),
                    destination_address = table.Column<string>(maxLength: 256, nullable: false),
                    exchange_currency = table.Column<int>(nullable: false),
                    gold_rate = table.Column<long>(nullable: false),
                    input = table.Column<int>(nullable: false),
                    input_rate = table.Column<long>(nullable: false),
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
                    table.PrimaryKey("PK_gm_buy_gold_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_buy_gold_request_gm_user_finhistory_ref_user_finhistory",
                        column: x => x.ref_user_finhistory,
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
                name: "IX_gm_buy_gold_request_ref_user_finhistory",
                table: "gm_buy_gold_request",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_gold_request_user_id",
                table: "gm_buy_gold_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_trans_gold_request_ref_user_finhistory",
                table: "gm_trans_gold_request",
                column: "ref_user_finhistory");

            migrationBuilder.CreateIndex(
                name: "IX_gm_trans_gold_request_user_id",
                table: "gm_trans_gold_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_finhistory_user_id",
                table: "gm_user_finhistory",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_buy_gold_request");

            migrationBuilder.DropTable(
                name: "gm_trans_gold_request");

            migrationBuilder.DropTable(
                name: "gm_user_finhistory");

            migrationBuilder.AddColumn<DateTime>(
                name: "crypto_deposit_stamp",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "crypto_withdraw_stamp",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "hash",
                table: "gm_transparency",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "gm_banned_country",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 512);

            migrationBuilder.CreateTable(
                name: "gm_card",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    card_holder = table.Column<string>(maxLength: 128, nullable: true),
                    card_masked = table.Column<string>(maxLength: 64, nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    gw_deposit_card_tid = table.Column<string>(maxLength: 64, nullable: true),
                    gw_withdraw_card_tid = table.Column<string>(maxLength: 64, nullable: true),
                    state = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false),
                    verification_amount = table.Column<long>(nullable: false),
                    verification_attempt = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_card", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_card_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_financial_history",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(nullable: false),
                    comment = table.Column<string>(maxLength: 512, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    fee = table.Column<long>(nullable: false),
                    rel_eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_expires = table.Column<DateTime>(nullable: true),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_financial_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_financial_history_gm_user_user_id",
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
                name: "gm_swift_template",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    bank = table.Column<string>(maxLength: 256, nullable: false),
                    bic = table.Column<string>(maxLength: 128, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    details = table.Column<string>(maxLength: 1024, nullable: false),
                    holder = table.Column<string>(maxLength: 256, nullable: false),
                    iban = table.Column<string>(maxLength: 256, nullable: false),
                    name = table.Column<string>(maxLength: 64, nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_swift_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_swift_template_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_card_payment",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(nullable: false),
                    card_id = table.Column<long>(nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    gw_transaction_id = table.Column<string>(maxLength: 64, nullable: false),
                    provider_message = table.Column<string>(maxLength: 512, nullable: true),
                    provider_status = table.Column<string>(maxLength: 64, nullable: true),
                    ref_payment_id = table.Column<long>(nullable: true),
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
                    table.PrimaryKey("PK_gm_card_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_card_payment_gm_card_card_id",
                        column: x => x.card_id,
                        principalTable: "gm_card",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_card_payment_gm_card_payment_ref_payment_id",
                        column: x => x.ref_payment_id,
                        principalTable: "gm_card_payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_card_payment_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_buy_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    actual_rate = table.Column<long>(nullable: true),
                    address = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
                    fiat_amount = table.Column<long>(nullable: false),
                    fixed_rate = table.Column<long>(nullable: false),
                    ref_fin_history = table.Column<long>(nullable: false),
                    request_index = table.Column<string>(maxLength: 64, nullable: true),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    time_requested = table.Column<DateTime>(nullable: true),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_buy_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_buy_request_gm_financial_history_ref_fin_history",
                        column: x => x.ref_fin_history,
                        principalTable: "gm_financial_history",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_buy_request_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_crypto_deposit",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    address = table.Column<string>(maxLength: 128, nullable: false),
                    amount = table.Column<string>(maxLength: 128, nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    origin = table.Column<int>(nullable: false),
                    rate = table.Column<long>(nullable: false),
                    ref_fin_history = table.Column<long>(nullable: false),
                    requested_amount = table.Column<string>(maxLength: 64, nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    time_prepared = table.Column<DateTime>(nullable: true),
                    origin_txid = table.Column<string>(maxLength: 256, nullable: true),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_crypto_deposit", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_crypto_deposit_gm_financial_history_ref_fin_history",
                        column: x => x.ref_fin_history,
                        principalTable: "gm_financial_history",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_crypto_deposit_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_deposit",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
                    ref_fin_history = table.Column<long>(nullable: false),
                    source = table.Column<int>(nullable: false),
                    source_id = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_deposit", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_deposit_gm_financial_history_ref_fin_history",
                        column: x => x.ref_fin_history,
                        principalTable: "gm_financial_history",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_deposit_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_sell_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    actual_rate = table.Column<long>(nullable: true),
                    address = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
                    fiat_amount = table.Column<long>(nullable: false),
                    fixed_rate = table.Column<long>(nullable: false),
                    ref_fin_history = table.Column<long>(nullable: false),
                    request_index = table.Column<string>(maxLength: 64, nullable: true),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    time_requested = table.Column<DateTime>(nullable: true),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_sell_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_sell_request_gm_financial_history_ref_fin_history",
                        column: x => x.ref_fin_history,
                        principalTable: "gm_financial_history",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_sell_request_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_swift_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(nullable: false),
                    bank = table.Column<string>(maxLength: 256, nullable: false),
                    bic = table.Column<string>(maxLength: 128, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    details = table.Column<string>(maxLength: 1024, nullable: false),
                    holder = table.Column<string>(maxLength: 256, nullable: false),
                    holder_address = table.Column<string>(maxLength: 512, nullable: false),
                    iban = table.Column<string>(maxLength: 256, nullable: false),
                    payment_ref = table.Column<string>(maxLength: 128, nullable: false),
                    ref_fin_history = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    support_comment = table.Column<string>(maxLength: 512, nullable: true),
                    support_user_id = table.Column<long>(nullable: true),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_swift_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_swift_request_gm_financial_history_ref_fin_history",
                        column: x => x.ref_fin_history,
                        principalTable: "gm_financial_history",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_swift_request_gm_user_support_user_id",
                        column: x => x.support_user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_swift_request_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_transfer_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount_wei = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    address = table.Column<string>(maxLength: 64, nullable: false),
                    eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
                    ref_fin_history = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_transfer_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_transfer_request_gm_financial_history_ref_fin_history",
                        column: x => x.ref_fin_history,
                        principalTable: "gm_financial_history",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_transfer_request_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_withdraw",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    destination = table.Column<int>(nullable: false),
                    destination_id = table.Column<long>(nullable: false),
                    eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
                    ref_fin_history = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_withdraw", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_withdraw_gm_financial_history_ref_fin_history",
                        column: x => x.ref_fin_history,
                        principalTable: "gm_financial_history",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_withdraw_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_request_ref_fin_history",
                table: "gm_buy_request",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_request_user_id",
                table: "gm_buy_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_card_user_id",
                table: "gm_card",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_card_payment_card_id",
                table: "gm_card_payment",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_card_payment_ref_payment_id",
                table: "gm_card_payment",
                column: "ref_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_card_payment_user_id",
                table: "gm_card_payment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_crypto_deposit_ref_fin_history",
                table: "gm_crypto_deposit",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_crypto_deposit_user_id",
                table: "gm_crypto_deposit",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_deposit_ref_fin_history",
                table: "gm_deposit",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_deposit_user_id",
                table: "gm_deposit",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_financial_history_user_id",
                table: "gm_financial_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_request_ref_fin_history",
                table: "gm_sell_request",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_request_user_id",
                table: "gm_sell_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_swift_request_ref_fin_history",
                table: "gm_swift_request",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_swift_request_support_user_id",
                table: "gm_swift_request",
                column: "support_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_swift_request_user_id",
                table: "gm_swift_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_swift_template_user_id",
                table: "gm_swift_template",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_transfer_request_ref_fin_history",
                table: "gm_transfer_request",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_transfer_request_user_id",
                table: "gm_transfer_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_withdraw_ref_fin_history",
                table: "gm_withdraw",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_withdraw_user_id",
                table: "gm_withdraw",
                column: "user_id");
        }
    }
}
