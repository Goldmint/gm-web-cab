using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class usersumuswalletbalance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "balance_gold",
                table: "gm_user_sumus_wallet",
                type: "decimal(38, 18)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "balance_mnt",
                table: "gm_user_sumus_wallet",
                type: "decimal(38, 18)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "balance_gold",
                table: "gm_user_sumus_wallet");

            migrationBuilder.DropColumn(
                name: "balance_mnt",
                table: "gm_user_sumus_wallet");
        }
    }
}
