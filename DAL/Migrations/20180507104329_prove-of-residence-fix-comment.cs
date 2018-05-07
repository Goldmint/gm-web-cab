using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Goldmint.DAL.Migrations
{
    public partial class proveofresidencefixcomment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "proved_residence_link",
                table: "gm_user_verification",
                newName: "proved_residence_comment");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "proved_residence_comment",
                table: "gm_user_verification",
                newName: "proved_residence_link");
        }
    }
}
