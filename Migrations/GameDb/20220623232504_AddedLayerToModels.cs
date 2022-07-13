using Microsoft.EntityFrameworkCore.Migrations;

namespace OnlineBuildingGame.Migrations.GameDb
{
    public partial class AddedLayerToModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Facing",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PosX",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PosY",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "Health",
                table: "Players",
                newName: "MapId");

            migrationBuilder.AddColumn<int>(
                name: "Layer",
                table: "ProtectedTiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DimZ",
                table: "Maps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpawnZ",
                table: "Maps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Layer",
                table: "Entities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MapId",
                table: "Entities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlayerLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Player = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MapId = table.Column<int>(type: "int", nullable: false),
                    PosX = table.Column<double>(type: "float", nullable: false),
                    PosY = table.Column<double>(type: "float", nullable: false),
                    Layer = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerLocations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerLocations");

            migrationBuilder.DropColumn(
                name: "Layer",
                table: "ProtectedTiles");

            migrationBuilder.DropColumn(
                name: "DimZ",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "SpawnZ",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "Layer",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "MapId",
                table: "Entities");

            migrationBuilder.RenameColumn(
                name: "MapId",
                table: "Players",
                newName: "Health");

            migrationBuilder.AddColumn<int>(
                name: "Facing",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "PosX",
                table: "Players",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PosY",
                table: "Players",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
