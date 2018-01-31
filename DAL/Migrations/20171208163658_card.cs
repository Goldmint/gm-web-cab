using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class card : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "card_holder",
                table: "gm_card_payment");

            migrationBuilder.DropColumn(
                name: "card_masked",
                table: "gm_card_payment");

            migrationBuilder.AddColumn<long>(
                name: "card_id",
                table: "gm_card_payment",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "gm_card",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    card_holder = table.Column<string>(maxLength: 64, nullable: true),
                    card_masked = table.Column<string>(maxLength: 64, nullable: true),
                    gw_customer_id = table.Column<string>(maxLength: 64, nullable: false),
                    gw_init_transaction_id = table.Column<string>(maxLength: 64, nullable: false),
                    state = table.Column<int>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_gm_card_payment_card_id",
                table: "gm_card_payment",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_card_user_id",
                table: "gm_card",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_card_payment_gm_card_card_id",
                table: "gm_card_payment",
                column: "card_id",
                principalTable: "gm_card",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_card_payment_gm_card_card_id",
                table: "gm_card_payment");

            migrationBuilder.DropTable(
                name: "gm_card");

            migrationBuilder.DropIndex(
                name: "IX_gm_card_payment_card_id",
                table: "gm_card_payment");

            migrationBuilder.DropColumn(
                name: "card_id",
                table: "gm_card_payment");

            migrationBuilder.AddColumn<string>(
                name: "card_holder",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "card_masked",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: true);
        }
    }
}
