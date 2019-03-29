using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class poolfreezerevents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_es_token_migration");

            migrationBuilder.DropTable(
                name: "gm_se_token_migration");

            migrationBuilder.CreateTable(
                name: "gm_pool_freeze_request",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    status = table.Column<int>(nullable: false),
                    eth_address = table.Column<string>(maxLength: 42, nullable: false),
                    eth_txid = table.Column<string>(maxLength: 66, nullable: false),
                    sum_address = table.Column<string>(maxLength: 128, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(38, 18)", nullable: false),
                    sum_txid = table.Column<string>(maxLength: 128, nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_completed = table.Column<DateTime>(nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_pool_freeze_request", x => x.id);
					table.UniqueConstraint("UX_gm_pool_freeze_request_txid", x => x.eth_txid);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_pool_freeze_request");

            migrationBuilder.CreateTable(
                name: "gm_es_token_migration",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<decimal>(type: "decimal(38, 18)", nullable: true),
                    asset = table.Column<int>(nullable: false),
                    block = table.Column<ulong>(nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    eth_address = table.Column<string>(maxLength: 42, nullable: false),
                    eth_txid = table.Column<string>(maxLength: 66, nullable: true),
                    status = table.Column<int>(nullable: false),
                    sum_address = table.Column<string>(maxLength: 128, nullable: false),
                    sum_txid = table.Column<string>(maxLength: 128, nullable: true),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_emitted = table.Column<DateTime>(nullable: true),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_es_token_migration", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_es_token_migration_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gm_se_token_migration",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    amount = table.Column<decimal>(type: "decimal(38, 18)", nullable: true),
                    asset = table.Column<int>(nullable: false),
                    block = table.Column<ulong>(nullable: true),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    eth_address = table.Column<string>(maxLength: 42, nullable: false),
                    eth_txid = table.Column<string>(maxLength: 66, nullable: true),
                    status = table.Column<int>(nullable: false),
                    sum_address = table.Column<string>(maxLength: 128, nullable: false),
                    sum_txid = table.Column<string>(maxLength: 128, nullable: true),
                    time_completed = table.Column<DateTime>(nullable: true),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_next_check = table.Column<DateTime>(nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_se_token_migration", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_se_token_migration_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_es_token_migration_user_id",
                table: "gm_es_token_migration",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_gm_se_token_migration_user_id",
                table: "gm_se_token_migration",
                column: "user_id");
        }
    }
}
