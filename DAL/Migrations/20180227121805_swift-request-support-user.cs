using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class swiftrequestsupportuser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "support_user_id",
                table: "gm_swift_payment",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_swift_payment_support_user_id",
                table: "gm_swift_payment",
                column: "support_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_swift_payment_gm_user_support_user_id",
                table: "gm_swift_payment",
                column: "support_user_id",
                principalTable: "gm_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_swift_payment_gm_user_support_user_id",
                table: "gm_swift_payment");

            migrationBuilder.DropIndex(
                name: "IX_gm_swift_payment_support_user_id",
                table: "gm_swift_payment");

            migrationBuilder.DropColumn(
                name: "support_user_id",
                table: "gm_swift_payment");
        }
    }
}
