using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class initialfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ticket_id",
                table: "gm_kyc_shuftipro_ticket",
                newName: "reference_id");

			migrationBuilder.CreateIndex(
				name: "ReferenceIdIndex",
				table: "gm_kyc_shuftipro_ticket",
				column: "reference_id",
				unique: true);
		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropIndex(
				name: "ReferenceIdIndex",
				table: "gm_kyc_shuftipro_ticket"
				);

            migrationBuilder.RenameColumn(
                name: "reference_id",
                table: "gm_kyc_shuftipro_ticket",
                newName: "ticket_id");
		}
    }
}
