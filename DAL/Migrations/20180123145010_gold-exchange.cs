using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class goldexchange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "amount",
                table: "gm_withdraw",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "gm_withdraw",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "amount",
                table: "gm_deposit",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "gm_deposit",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "amount",
                table: "gm_card_payment",
                nullable: false,
                oldClrType: typeof(double));

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: true);

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
                    request_index = table.Column<string>(maxLength: 64, nullable: true),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_buy_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_buy_request_gm_user_user_id",
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
                    request_index = table.Column<string>(maxLength: 64, nullable: true),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_sell_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_sell_request_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_settings",
                columns: table => new
                {
                    key = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    value = table.Column<string>(maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_settings", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_buy_request_user_id",
                table: "gm_buy_request",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_sell_request_user_id",
                table: "gm_sell_request",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_buy_request");

            migrationBuilder.DropTable(
                name: "gm_sell_request");

            migrationBuilder.DropTable(
                name: "gm_settings");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "gm_withdraw");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "gm_deposit");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "gm_card_payment");

            migrationBuilder.AlterColumn<double>(
                name: "amount",
                table: "gm_withdraw",
                nullable: false,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<double>(
                name: "amount",
                table: "gm_deposit",
                nullable: false,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<double>(
                name: "amount",
                table: "gm_card_payment",
                nullable: false,
                oldClrType: typeof(long));
        }
    }
}
