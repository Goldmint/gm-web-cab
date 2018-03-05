using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class dpa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "dpa_document_id",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_options_dpa_document_id",
                table: "gm_user_options",
                column: "dpa_document_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_user_options_gm_signed_document_dpa_document_id",
                table: "gm_user_options",
                column: "dpa_document_id",
                principalTable: "gm_signed_document",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_user_options_gm_signed_document_dpa_document_id",
                table: "gm_user_options");

            migrationBuilder.DropIndex(
                name: "IX_gm_user_options_dpa_document_id",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "dpa_document_id",
                table: "gm_user_options");
        }
    }
}
