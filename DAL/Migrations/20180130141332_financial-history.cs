using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class financialhistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("delete from gm_deposit where 1=1");
			migrationBuilder.Sql("delete from gm_withdraw where 1=1");

			migrationBuilder.DropColumn(
                name: "ref_entity",
                table: "gm_card_payment");

            migrationBuilder.RenameColumn(
                name: "ref_entity_id",
                table: "gm_card_payment",
                newName: "ref_payment_id");

            migrationBuilder.AddColumn<long>(
                name: "ref_fin_history",
                table: "gm_withdraw",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ref_fin_history",
                table: "gm_deposit",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "gm_financial_history",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(nullable: false),
                    balance = table.Column<long>(nullable: false),
                    comment = table.Column<string>(maxLength: 512, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    fee = table.Column<long>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_gm_withdraw_ref_fin_history",
                table: "gm_withdraw",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_deposit_ref_fin_history",
                table: "gm_deposit",
                column: "ref_fin_history");

            migrationBuilder.CreateIndex(
                name: "IX_gm_card_payment_ref_payment_id",
                table: "gm_card_payment",
                column: "ref_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_financial_history_user_id",
                table: "gm_financial_history",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_card_payment_gm_card_payment_ref_payment_id",
                table: "gm_card_payment",
                column: "ref_payment_id",
                principalTable: "gm_card_payment",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_deposit_gm_financial_history_ref_fin_history",
                table: "gm_deposit",
                column: "ref_fin_history",
                principalTable: "gm_financial_history",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_withdraw_gm_financial_history_ref_fin_history",
                table: "gm_withdraw",
                column: "ref_fin_history",
                principalTable: "gm_financial_history",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_card_payment_gm_card_payment_ref_payment_id",
                table: "gm_card_payment");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_deposit_gm_financial_history_ref_fin_history",
                table: "gm_deposit");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_withdraw_gm_financial_history_ref_fin_history",
                table: "gm_withdraw");

            migrationBuilder.DropTable(
                name: "gm_financial_history");

            migrationBuilder.DropIndex(
                name: "IX_gm_withdraw_ref_fin_history",
                table: "gm_withdraw");

            migrationBuilder.DropIndex(
                name: "IX_gm_deposit_ref_fin_history",
                table: "gm_deposit");

            migrationBuilder.DropIndex(
                name: "IX_gm_card_payment_ref_payment_id",
                table: "gm_card_payment");

            migrationBuilder.DropColumn(
                name: "ref_fin_history",
                table: "gm_withdraw");

            migrationBuilder.DropColumn(
                name: "ref_fin_history",
                table: "gm_deposit");

            migrationBuilder.RenameColumn(
                name: "ref_payment_id",
                table: "gm_card_payment",
                newName: "ref_entity_id");

            migrationBuilder.AddColumn<int>(
                name: "ref_entity",
                table: "gm_card_payment",
                nullable: true);
        }
    }
}
