using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class buygoldfiat3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "fiat_amount",
                table: "gm_buy_gold_fiat",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fiat_amount",
                table: "gm_buy_gold_fiat");
        }
    }
}
