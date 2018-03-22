using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class cryptodepositandfixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

	        migrationBuilder.Sql("delete from gm_swift_request where 1=1");

	        migrationBuilder.Sql("update gm_financial_history set status=5 where status=3");
	        migrationBuilder.Sql("update gm_financial_history set status=4 where status=2");
	        migrationBuilder.Sql("update gm_financial_history set status=3 where status=1");

	        migrationBuilder.Sql("update gm_buy_request set status=status+1 where status>1");
	        migrationBuilder.Sql("update gm_sell_request set status=status+1 where status>1");
	        migrationBuilder.Sql("update gm_transfer_request set status=status+1 where status>1");

			migrationBuilder.AddColumn<DateTime>(
                name: "crypto_deposit_stamp",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "crypto_withdraw_stamp",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ref_fin_history",
                table: "gm_swift_request",
                nullable: false,
                defaultValue: 0L);

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

            migrationBuilder.CreateIndex(
                name: "IX_gm_swift_request_ref_fin_history",
                table: "gm_swift_request",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_crypto_deposit_ref_fin_history",
                table: "gm_crypto_deposit",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_crypto_deposit_user_id",
                table: "gm_crypto_deposit",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_swift_request_gm_financial_history_ref_fin_history",
                table: "gm_swift_request",
                column: "ref_fin_history",
                principalTable: "gm_financial_history",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_swift_request_gm_financial_history_ref_fin_history",
                table: "gm_swift_request");

            migrationBuilder.DropTable(
                name: "gm_crypto_deposit");

            migrationBuilder.DropIndex(
                name: "IX_gm_swift_request_ref_fin_history",
                table: "gm_swift_request");

            migrationBuilder.DropColumn(
                name: "crypto_deposit_stamp",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "crypto_withdraw_stamp",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "ref_fin_history",
                table: "gm_swift_request");
        }
    }
}
