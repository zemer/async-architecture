using Microsoft.EntityFrameworkCore.Migrations;

namespace Accounting.Migrations
{
    public partial class bill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Cost",
                table: "Tasks",
                newName: "CompleteCost");

            migrationBuilder.AddColumn<float>(
                name: "AssignCost",
                table: "Tasks",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Bill",
                table: "Accounts",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignCost",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Bill",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "CompleteCost",
                table: "Tasks",
                newName: "Cost");
        }
    }
}
