using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class primaryagreementfix1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "last_agreement_id",
                table: "gm_user_verification",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_verification_last_agreement_id",
                table: "gm_user_verification",
                column: "last_agreement_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_verification_gm_signed_document_last_agreement_id",
                table: "gm_user_verification",
                column: "last_agreement_id",
                principalTable: "gm_signed_document",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_verification_gm_signed_document_last_agreement_id",
                table: "gm_user_verification");

            migrationBuilder.DropIndex(
                name: "IX_gm_user_verification_last_agreement_id",
                table: "gm_user_verification");

            migrationBuilder.DropColumn(
                name: "last_agreement_id",
                table: "gm_user_verification");
        }
    }
}
