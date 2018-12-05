using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.DAL.Migrations
{
    public partial class ethoperationdiscountfield : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gm_temporary_wallet");

            migrationBuilder.AddColumn<double>(
                name: "discount",
                table: "gm_eth_operation",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
 
            migrationBuilder.DropColumn(
                name: "discount",
                table: "gm_eth_operation");

            migrationBuilder.CreateTable(
                name: "gm_temporary_wallet",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    concurrency_stamp = table.Column<string>(maxLength: 64, nullable: true),
                    private_key = table.Column<string>(maxLength: 128, nullable: false),
                    public_key = table.Column<string>(maxLength: 128, nullable: false),
                    user_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gm_temporary_wallet", x => x.id);
                    table.ForeignKey(
                        name: "FK_gm_temporary_wallet_gm_user_user_id",
                        column: x => x.user_id,
                        principalTable: "gm_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gm_temporary_wallet_user_id",
                table: "gm_temporary_wallet",
                column: "user_id");
        }
    }
}
