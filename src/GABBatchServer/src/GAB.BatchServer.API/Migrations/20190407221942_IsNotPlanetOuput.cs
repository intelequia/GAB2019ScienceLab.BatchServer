using Microsoft.EntityFrameworkCore.Migrations;

namespace GAB.BatchServer.API.Migrations
{
    public partial class IsNotPlanetOuput : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Probability",
                table: "Outputs",
                newName: "IsNotPlanet");

            migrationBuilder.AlterColumn<double>(
                name: "IsPlanet",
                table: "Outputs",
                nullable: false,
                oldClrType: typeof(bool));

            migrationBuilder.AddColumn<string>(
                name: "Frequencies",
                table: "Outputs",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Frequencies",
                table: "Outputs");

            migrationBuilder.RenameColumn(
                name: "IsNotPlanet",
                table: "Outputs",
                newName: "Probability");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPlanet",
                table: "Outputs",
                nullable: false,
                oldClrType: typeof(double));
        }
    }
}
