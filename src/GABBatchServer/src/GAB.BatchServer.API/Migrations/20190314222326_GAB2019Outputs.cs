using Microsoft.EntityFrameworkCore.Migrations;

namespace GAB.BatchServer.API.Migrations
{
    public partial class GAB2019Outputs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IDX_Email",
                table: "LabUsers");

            migrationBuilder.DropColumn(
                name: "AvgScore",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "TotalItems",
                table: "Outputs");

            migrationBuilder.RenameColumn(
                name: "TotalScore",
                table: "Outputs",
                newName: "Probability");

            migrationBuilder.AddColumn<bool>(
                name: "IsPlanet",
                table: "Outputs",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IDX_Email",
                table: "LabUsers",
                column: "EMail",
                unique: true,
                filter: "[EMail] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IDX_Email",
                table: "LabUsers");

            migrationBuilder.DropColumn(
                name: "IsPlanet",
                table: "Outputs");

            migrationBuilder.RenameColumn(
                name: "Probability",
                table: "Outputs",
                newName: "TotalScore");

            migrationBuilder.AddColumn<double>(
                name: "AvgScore",
                table: "Outputs",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MaxScore",
                table: "Outputs",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TotalItems",
                table: "Outputs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IDX_Email",
                table: "LabUsers",
                column: "EMail",
                unique: true);
        }
    }
}
