using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class tokenmigrationemissiontimebug : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "time_emitted",
                table: "gm_es_token_migration",
                nullable: true,
                oldClrType: typeof(DateTime));


	        migrationBuilder.AddUniqueConstraint(
		        name: "UX_gm_es_token_migration_dupl",
		        table: "gm_es_token_migration",
		        columns: new string[] { "asset", "eth_address", "block" }
	        );
	        migrationBuilder.AddUniqueConstraint(
		        name: "UX_gm_es_token_migration_txid",
		        table: "gm_es_token_migration",
		        columns: new string[] { "eth_txid" }
	        );


	        migrationBuilder.AddUniqueConstraint(
		        name: "UX_gm_se_token_migration_dupl",
		        table: "gm_se_token_migration",
		        columns: new string[] { "asset", "sum_address", "block" }
	        );
	        migrationBuilder.AddUniqueConstraint(
		        name: "UC_gm_se_token_migration_txid",
		        table: "gm_se_token_migration",
		        columns: new string[] { "sum_txid" }
	        );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "time_emitted",
                table: "gm_es_token_migration",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);
        }
    }
}
