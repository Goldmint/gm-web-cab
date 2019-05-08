using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class usersumuswallettracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "tracking",
                table: "gm_user_sumus_wallet",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tracking",
                table: "gm_user_sumus_wallet");
        }
    }
}
