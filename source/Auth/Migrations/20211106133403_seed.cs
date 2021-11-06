using Microsoft.EntityFrameworkCore.Migrations;

namespace Auth.Migrations
{
    public partial class seed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "16c1646a-2cbb-4bb9-a597-fd669f80e9b7", "b0f058ad-861f-4a29-919e-383eae9e5df6", "Worker", null });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "c733a386-0b64-4150-a213-5bb22c8fe202", "a8ba923e-5a9a-4f23-b0d2-8f93774d152f", "Manager", null });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "bcc8f29d-6438-4989-82ef-b5639b850bea", "d039ae23-2a69-4be2-bb22-9490e13e2f21", "Administrator", null });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "16c1646a-2cbb-4bb9-a597-fd669f80e9b7");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "bcc8f29d-6438-4989-82ef-b5639b850bea");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c733a386-0b64-4150-a213-5bb22c8fe202");
        }
    }
}
