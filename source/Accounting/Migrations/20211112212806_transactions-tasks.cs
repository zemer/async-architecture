using Microsoft.EntityFrameworkCore.Migrations;

namespace Accounting.Migrations
{
    public partial class transactionstasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Task",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TaskId",
                table: "Transactions",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Task",
                table: "Transactions",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Task",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TaskId",
                table: "Transactions");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Task",
                table: "Transactions",
                column: "AccountId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
