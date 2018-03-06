using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class userverificationfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_verification_gm_kyc_shuftipro_ticket_verified_kyc_tic",
                table: "gm_user_verification");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_verification_gm_signed_document_signed_agreement_id",
                table: "gm_user_verification");

            migrationBuilder.DropIndex(
                name: "IX_gm_user_verification_verified_kyc_ticket_id",
                table: "gm_user_verification");

            migrationBuilder.DropIndex(
                name: "IX_gm_user_verification_signed_agreement_id",
                table: "gm_user_verification");

            migrationBuilder.DropColumn(
                name: "verified_kyc_ticket_id",
                table: "gm_user_verification");

            migrationBuilder.DropColumn(
                name: "signed_agreement_id",
                table: "gm_user_verification");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "verified_kyc_ticket_id",
                table: "gm_user_verification",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "signed_agreement_id",
                table: "gm_user_verification",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_verification_verified_kyc_ticket_id",
                table: "gm_user_verification",
                column: "verified_kyc_ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_verification_signed_agreement_id",
                table: "gm_user_verification",
                column: "signed_agreement_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_verification_gm_kyc_shuftipro_ticket_verified_kyc_ticket_id",
                table: "gm_user_verification",
                column: "verified_kyc_ticket_id",
                principalTable: "gm_kyc_shuftipro_ticket",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_verification_gm_signed_document_signed_agreement_id",
                table: "gm_user_verification",
                column: "signed_agreement_id",
                principalTable: "gm_signed_document",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
