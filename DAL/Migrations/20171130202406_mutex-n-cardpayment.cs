using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class mutexncardpayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_card_payment",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<double>(nullable: false),
                    card = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<byte>(nullable: false),
                    finalized = table.Column<bool>(nullable: false),
                    gw_transaction_id = table.Column<string>(maxLength: 64, nullable: false),
                    is_deposit = table.Column<bool>(nullable: false),
                    provider_message = table.Column<string>(maxLength: 512, nullable: true),
                    provider_status = table.Column<string>(maxLength: 64, nullable: true),
                    status = table.Column<byte>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    transaction_id = table.Column<string>(maxLength: 64, nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_card_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_card_payment_gm_user_user_id",
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
                    description = table.Column<string>(maxLength: 64, nullable: false),
                    expires = table.Column<DateTime>(nullable: false),
                    locker = table.Column<string>(maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_mutex", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_card_payment_user_id",
                table: "gm_card_payment",
                column: "user_id");

			migrationBuilder.CreateIndex(
				name: "TransactionIdIndex",
				table: "gm_card_payment",
				column: "transaction_id",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "GWTransactionIdIndex",
				table: "gm_card_payment",
				column: "gw_transaction_id",
				unique: true);
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_card_payment");

            migrationBuilder.DropTable(
                name: "gm_mutex");
        }
    }
}
