using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Analytics.Migrations
{
    public partial class completetask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignCost",
                table: "Tasks");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateCompleted",
                table: "Tasks",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCompleted",
                table: "Tasks");

            migrationBuilder.AddColumn<float>(
                name: "AssignCost",
                table: "Tasks",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
