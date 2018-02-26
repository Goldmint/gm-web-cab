using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class hotwallettransferrequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_transfer_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    address = table.Column<string>(maxLength: 64, nullable: false),
                    amount_wei = table.Column<string>(maxLength: 64, nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    desk_ticket_id = table.Column<string>(maxLength: 32, nullable: false),
                    eth_transaction_id = table.Column<string>(maxLength: 66, nullable: true),
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
                        name: "FK_gm_transfer_request_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_transfer_request_user_id",
                table: "gm_transfer_request",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_transfer_request");
        }
    }
}
