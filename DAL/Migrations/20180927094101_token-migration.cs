using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class tokenmigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    time_next_check = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_es_token_migration", x => x.id);
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
                    time_next_check = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_se_token_migration", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_es_token_migration");

            migrationBuilder.DropTable(
                name: "gm_se_token_migration");
        }
    }
}
