using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class usersumuswallet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hw_buying_stamp",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "hw_selling_stamp",
                table: "gm_user_options");

            migrationBuilder.DropColumn(
                name: "hw_transfer_stamp",
                table: "gm_user_options");

            migrationBuilder.CreateTable(
                name: "gm_user_sumus_wallet",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<long>(nullable: false),
                    public_key = table.Column<string>(maxLength: 128, nullable: false),
                    private_key = table.Column<string>(maxLength: 128, nullable: false),
                    time_created = table.Column<DateTime>(nullable: false),
                    time_checked = table.Column<DateTime>(nullable: false),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_user_sumus_wallet", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_user_sumus_wallet_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
					table.UniqueConstraint("UX_gm_user_sumus_wallet_pvt_key", x => x.private_key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_user_sumus_wallet_user_id",
                table: "gm_user_sumus_wallet",
                column: "user_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_user_sumus_wallet");

            migrationBuilder.AddColumn<DateTime>(
                name: "hw_buying_stamp",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "hw_selling_stamp",
                table: "gm_user_options",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "hw_transfer_stamp",
                table: "gm_user_options",
                nullable: true);
        }
    }
}
