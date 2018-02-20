using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class signeddocument : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_gm_settings",
                table: "gm_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_gm_banned_country",
                table: "gm_banned_country");

            migrationBuilder.AddColumn<long>(
                name: "id",
                table: "gm_settings",
                nullable: false,
                defaultValue: 0L)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<long>(
                name: "id",
                table: "gm_banned_country",
                nullable: false,
                defaultValue: 0L)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_gm_settings",
                table: "gm_settings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_gm_banned_country",
                table: "gm_banned_country",
                column: "id");

            migrationBuilder.CreateTable(
                name: "gm_signed_document",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    callback_event_type = table.Column<string>(maxLength: 64, nullable: true),
                    callback_status = table.Column<string>(maxLength: 16, nullable: true),
                    is_signed = table.Column<bool>(nullable: false),
                    reference_id = table.Column<string>(maxLength: 32, nullable: false),
                    time_completed = table.Column<DateTime>(nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    token = table.Column<string>(maxLength: 64, nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_signed_document", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_signed_document_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_signed_document_user_id",
                table: "gm_signed_document",
                column: "user_id");

	        migrationBuilder.AddUniqueConstraint(
				name: "IX_gm_settings_key",
				table: "gm_settings",
				column: "key"
	        );

	        migrationBuilder.AddUniqueConstraint(
		        name: "IX_gm_banned_country_code",
		        table: "gm_banned_country",
		        column: "code"
	        );
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.DropUniqueConstraint(
		        name: "IX_gm_settings_key",
		        table: "gm_settings"
	        );

	        migrationBuilder.DropUniqueConstraint(
		        name: "IX_gm_banned_country_code",
		        table: "gm_banned_country"
	        );
            migrationBuilder.DropTable(
                name: "gm_signed_document");

            migrationBuilder.DropPrimaryKey(
                name: "PK_gm_settings",
                table: "gm_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_gm_banned_country",
                table: "gm_banned_country");

            migrationBuilder.DropColumn(
                name: "id",
                table: "gm_settings");

            migrationBuilder.DropColumn(
                name: "id",
                table: "gm_banned_country");

            migrationBuilder.AddPrimaryKey(
                name: "PK_gm_settings",
                table: "gm_settings",
                column: "key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_gm_banned_country",
                table: "gm_banned_country",
                column: "code");

	        
		}
    }
}
