using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class kycticketfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_verification_gm_kyc_shuftipro_ticket_kyc_shuftipro_ti",
                table: "gm_user_verification");

            migrationBuilder.RenameColumn(
                name: "kyc_shuftipro_ticket_id",
                table: "gm_user_verification",
                newName: "verified_kyc_ticket_id");

            migrationBuilder.RenameIndex(
                name: "IX_gm_user_verification_kyc_shuftipro_ticket_id",
                table: "gm_user_verification",
                newName: "IX_gm_user_verification_verified_kyc_ticket_id");

            migrationBuilder.AddColumn<long>(
                name: "last_kyc_ticket_id",
                table: "gm_user_verification",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "time_responed",
                table: "gm_kyc_shuftipro_ticket",
                nullable: true,
                oldClrType: typeof(DateTime));

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_verification_last_kyc_ticket_id",
                table: "gm_user_verification",
                column: "last_kyc_ticket_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_verification_gm_kyc_shuftipro_ticket_last_kyc_ticket_id",
                table: "gm_user_verification",
                column: "last_kyc_ticket_id",
                principalTable: "gm_kyc_shuftipro_ticket",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_verification_gm_kyc_shuftipro_ticket_verified_kyc_ticket_id",
                table: "gm_user_verification",
                column: "verified_kyc_ticket_id",
                principalTable: "gm_kyc_shuftipro_ticket",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_verification_gm_kyc_shuftipro_ticket_last_kyc_ticket_id",
                table: "gm_user_verification");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_verification_gm_kyc_shuftipro_ticket_verified_kyc_ticket_id",
                table: "gm_user_verification");

            migrationBuilder.DropIndex(
                name: "IX_gm_user_verification_last_kyc_ticket_id",
                table: "gm_user_verification");

            migrationBuilder.DropColumn(
                name: "last_kyc_ticket_id",
                table: "gm_user_verification");

            migrationBuilder.RenameColumn(
                name: "verified_kyc_ticket_id",
                table: "gm_user_verification",
                newName: "kyc_shuftipro_ticket_id");

            migrationBuilder.RenameIndex(
                name: "IX_gm_user_verification_verified_kyc_ticket_id",
                table: "gm_user_verification",
                newName: "IX_gm_user_verification_kyc_shuftipro_ticket_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "time_responed",
                table: "gm_kyc_shuftipro_ticket",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_verification_gm_kyc_shuftipro_ticket_kyc_shuftipro_ticket_id",
                table: "gm_user_verification",
                column: "kyc_shuftipro_ticket_id",
                principalTable: "gm_kyc_shuftipro_ticket",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
