using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class addusertomigrationtables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "user_id",
                table: "gm_se_token_migration",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "user_id",
                table: "gm_es_token_migration",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_gm_se_token_migration_user_id",
                table: "gm_se_token_migration",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_es_token_migration_user_id",
                table: "gm_es_token_migration",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_gm_es_token_migration_gm_user_user_id",
                table: "gm_es_token_migration",
                column: "user_id",
                principalTable: "gm_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gm_se_token_migration_gm_user_user_id",
                table: "gm_se_token_migration",
                column: "user_id",
                principalTable: "gm_user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gm_es_token_migration_gm_user_user_id",
                table: "gm_es_token_migration");

            migrationBuilder.DropForeignKey(
                name: "FK_gm_se_token_migration_gm_user_user_id",
                table: "gm_se_token_migration");

            migrationBuilder.DropIndex(
                name: "IX_gm_se_token_migration_user_id",
                table: "gm_se_token_migration");

            migrationBuilder.DropIndex(
                name: "IX_gm_es_token_migration_user_id",
                table: "gm_es_token_migration");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "gm_se_token_migration");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "gm_es_token_migration");
        }
    }
}
