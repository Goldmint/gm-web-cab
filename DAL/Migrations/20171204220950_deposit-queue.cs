using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class depositqueue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "card",
                table: "gm_card_payment",
                newName: "card_masked");

            migrationBuilder.AlterColumn<string>(
                name: "desk_ticket_id",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "gm_deposit",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<double>(nullable: false),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 64, nullable: false),
                    eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
                    finalized = table.Column<bool>(nullable: false),
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
                        name: "FK_gm_deposit_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_deposit_user_id",
                table: "gm_deposit",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_deposit");

            migrationBuilder.RenameColumn(
                name: "card_masked",
                table: "gm_card_payment",
                newName: "card");

            migrationBuilder.AlterColumn<string>(
                name: "desk_ticket_id",
                table: "gm_card_payment",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64);
        }
    }
}
