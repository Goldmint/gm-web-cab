using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class primaryagreement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "primary_agreement_read",
                table: "gm_user_options");

            migrationBuilder.RenameColumn(
                name: "initial_tfa_quest",
                table: "gm_user_options",
                newName: "init_tfa_quest");

            migrationBuilder.RenameColumn(
                name: "token",
                table: "gm_signed_document",
                newName: "secret");

            migrationBuilder.AddColumn<long>(
                name: "signed_agreement_id",
                table: "gm_user_verification",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "time_completed",
                table: "gm_signed_document",
                nullable: true,
                oldClrType: typeof(DateTime));

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_verification_signed_agreement_id",
                table: "gm_user_verification",
                column: "signed_agreement_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_verification_gm_signed_document_signed_agreement_id",
                table: "gm_user_verification",
                column: "signed_agreement_id",
                principalTable: "gm_signed_document",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_verification_gm_signed_document_signed_agreement_id",
                table: "gm_user_verification");

            migrationBuilder.DropIndex(
                name: "IX_gm_user_verification_signed_agreement_id",
                table: "gm_user_verification");

            migrationBuilder.DropColumn(
                name: "signed_agreement_id",
                table: "gm_user_verification");

            migrationBuilder.RenameColumn(
                name: "init_tfa_quest",
                table: "gm_user_options",
                newName: "initial_tfa_quest");

            migrationBuilder.RenameColumn(
                name: "secret",
                table: "gm_signed_document",
                newName: "token");

            migrationBuilder.AddColumn<bool>(
                name: "primary_agreement_read",
                table: "gm_user_options",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "time_completed",
                table: "gm_signed_document",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);
        }
    }
}
