using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class swiftpayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_swift_payment",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<long>(nullable: false),
                    ben_addr = table.Column<string>(maxLength: 512, nullable: false),
                    ben_bank_addr = table.Column<string>(maxLength: 512, nullable: false),
                    ben_bank_name = table.Column<string>(maxLength: 256, nullable: false),
                    ben_iban = table.Column<string>(maxLength: 128, nullable: false),
                    ben_name = table.Column<string>(maxLength: 256, nullable: false),
                    ben_swift = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    currency = table.Column<int>(nullable: false),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    payment_ref = table.Column<string>(maxLength: 128, nullable: false),
                    status = table.Column<int>(nullable: false),
                    support_comment = table.Column<string>(maxLength: 512, nullable: true),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_swift_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_swift_payment_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_swift_payment_user_id",
                table: "gm_swift_payment",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_swift_payment");
        }
    }
}
