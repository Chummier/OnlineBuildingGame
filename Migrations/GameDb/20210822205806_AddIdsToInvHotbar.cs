using Microsoft.EntityFrameworkCore.Migrations;

namespace OnlineBuildingGame.Migrations.GameDb
{
    public partial class AddIdsToInvHotbar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Hotbars",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Hotbars");
        }
    }
}
