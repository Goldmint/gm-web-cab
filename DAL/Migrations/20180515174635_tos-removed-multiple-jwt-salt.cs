using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class tosremovedmultiplejwtsalt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameColumn(
                name: "jwt_salt",
                table: "gm_user",
                newName: "jwt_salt_dbr");

            migrationBuilder.AddColumn<bool>(
                name: "tos_agreed",
                table: "gm_user_verification",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "jwt_salt_cab",
                table: "gm_user",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

	        migrationBuilder.Sql("update gm_user set gm_user.jwt_salt_cab=FLOOR(RAND() * 1000000), gm_user.jwt_salt_dbr=FLOOR(RAND() * 1000000) where 1=1");
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tos_agreed",
                table: "gm_user_verification");

            migrationBuilder.DropColumn(
                name: "jwt_salt_cab",
                table: "gm_user");

            migrationBuilder.RenameColumn(
                name: "jwt_salt_dbr",
                table: "gm_user",
                newName: "jwt_salt");

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
    }
}
