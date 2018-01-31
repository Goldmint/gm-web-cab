using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gm_role",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(nullable: true),
                    name = table.Column<string>(maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gm_user",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    access_failed_count = table.Column<int>(nullable: false),
                    concurrency_stamp = table.Column<string>(nullable: true),
                    email = table.Column<string>(maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(nullable: false),
                    lockout_enabled = table.Column<bool>(nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(nullable: true),
                    normalized_email = table.Column<string>(maxLength: 256, nullable: true),
                    normalized_username = table.Column<string>(maxLength: 256, nullable: true),
                    password_hash = table.Column<string>(nullable: true),
                    phone_number = table.Column<string>(nullable: true),
                    phone_number_confirmed = table.Column<bool>(nullable: false),
                    security_stamp = table.Column<string>(nullable: true),
                    tfa_secret = table.Column<string>(maxLength: 32, nullable: false),
                    time_registered = table.Column<DateTime>(nullable: false),
                    tfa_enabled = table.Column<bool>(nullable: false),
                    username = table.Column<string>(maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gm_role_claim",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    claim_type = table.Column<string>(nullable: true),
                    claim_value = table.Column<string>(nullable: true),
                    role_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_role_claim", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_role_claim_gm_role_role_id",
                        column: x => x.role_id,
                        principalTable: "gm_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_kyc_shuftipro_ticket",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    callback_message = table.Column<string>(maxLength: 128, nullable: true),
                    callback_status_code = table.Column<string>(maxLength: 32, nullable: true),
                    country_code = table.Column<string>(maxLength: 2, nullable: false),
                    dob = table.Column<DateTime>(nullable: false),
                    first_name = table.Column<string>(maxLength: 64, nullable: false),
                    is_verified = table.Column<bool>(nullable: false),
                    last_name = table.Column<string>(maxLength: 64, nullable: false),
                    method = table.Column<string>(maxLength: 32, nullable: false),
                    phone_number = table.Column<string>(maxLength: 32, nullable: false),
                    ticket_id = table.Column<string>(maxLength: 64, nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_responed = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_kyc_shuftipro_ticket", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_kyc_shuftipro_ticket_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_user_claim",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    claim_type = table.Column<string>(nullable: true),
                    claim_value = table.Column<string>(nullable: true),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_claim", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_user_claim_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_user_login",
                columns: table => new
                {
                    login_provider = table.Column<string>(nullable: false),
                    provider_key = table.Column<string>(nullable: false),
                    provider_display_name = table.Column<string>(nullable: true),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_login", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "FK_gm_user_login_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_user_options",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    initial_tfa_quest = table.Column<bool>(nullable: false),
                    primary_agreement_read = table.Column<bool>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_user_options_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_user_role",
                columns: table => new
                {
                    user_id = table.Column<long>(nullable: false),
                    role_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_role", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_gm_user_role_gm_role_role_id",
                        column: x => x.role_id,
                        principalTable: "gm_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gm_user_role_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_user_token",
                columns: table => new
                {
                    user_id = table.Column<long>(nullable: false),
                    login_provider = table.Column<string>(nullable: false),
                    name = table.Column<string>(nullable: false),
                    value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_token", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "FK_gm_user_token_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_user_verification",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    apartment = table.Column<string>(maxLength: 128, nullable: true),
                    city = table.Column<string>(maxLength: 256, nullable: true),
                    country_code = table.Column<string>(maxLength: 2, nullable: true),
                    dob = table.Column<DateTime>(nullable: true),
                    first_name = table.Column<string>(maxLength: 64, nullable: true),
                    kyc_shuftipro_ticket_id = table.Column<long>(nullable: true),
                    last_name = table.Column<string>(maxLength: 64, nullable: true),
                    middle_name = table.Column<string>(maxLength: 64, nullable: true),
                    phone_number = table.Column<string>(maxLength: 32, nullable: true),
                    postal_code = table.Column<string>(maxLength: 16, nullable: true),
                    state = table.Column<string>(maxLength: 256, nullable: true),
                    street = table.Column<string>(maxLength: 256, nullable: true),
                    time_user_changed = table.Column<DateTime>(nullable: true),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_verification", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_user_verification_gm_kyc_shuftipro_ticket_kyc_shuftipro_ticket_id",
                        column: x => x.kyc_shuftipro_ticket_id,
                        principalTable: "gm_kyc_shuftipro_ticket",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gm_user_verification_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_kyc_shuftipro_ticket_user_id",
                table: "gm_kyc_shuftipro_ticket",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "gm_role",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_role_claim_role_id",
                table: "gm_role_claim",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "gm_user",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "gm_user",
                column: "normalized_username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_claim_user_id",
                table: "gm_user_claim",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_login_user_id",
                table: "gm_user_login",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_options_user_id",
                table: "gm_user_options",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_role_role_id",
                table: "gm_user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_verification_kyc_shuftipro_ticket_id",
                table: "gm_user_verification",
                column: "kyc_shuftipro_ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_verification_user_id",
                table: "gm_user_verification",
                column: "user_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_role_claim");

            migrationBuilder.DropTable(
                name: "gm_user_claim");

            migrationBuilder.DropTable(
                name: "gm_user_login");

            migrationBuilder.DropTable(
                name: "gm_user_options");

            migrationBuilder.DropTable(
                name: "gm_user_role");

            migrationBuilder.DropTable(
                name: "gm_user_token");

            migrationBuilder.DropTable(
                name: "gm_user_verification");

            migrationBuilder.DropTable(
                name: "gm_role");

            migrationBuilder.DropTable(
                name: "gm_kyc_shuftipro_ticket");

            migrationBuilder.DropTable(
                name: "gm_user");
        }
    }
}
