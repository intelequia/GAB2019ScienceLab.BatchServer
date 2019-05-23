using Microsoft.EntityFrameworkCore.Migrations;

namespace GAB.BatchServer.API.Migrations
{
    public partial class AddedTargetDetailsToOutput : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Result",
                table: "Outputs",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CCD",
                table: "Outputs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Camera",
                table: "Outputs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClientVersion",
                table: "Outputs",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerId",
                table: "Outputs",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Dec",
                table: "Outputs",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RA",
                table: "Outputs",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Sector",
                table: "Outputs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TICId",
                table: "Outputs",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TMag",
                table: "Outputs",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CCD",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "Camera",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "ClientVersion",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "ContainerId",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "Dec",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "RA",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "TICId",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "TMag",
                table: "Outputs");

            migrationBuilder.AlterColumn<string>(
                name: "Result",
                table: "Outputs",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 1024,
                oldNullable: true);
        }
    }
}
