using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    public partial class AddSeedingCancelType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalCost",
                table: "Bookings");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Type",
                value: "I have changed my mind");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Type",
                value: "I found a better option");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Type",
                value: "My schedule conflicted");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Type",
                value: "An unexpected event occurred");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Type",
                value: "The address was incorrect");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Type",
                value: "I want to change my request");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Type",
                value: "Other reason");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalCost",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Type",
                value: "ChangedMind");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Type",
                value: "FoundBetterOption");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Type",
                value: "ScheduleConflict");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 4,
                column: "Type",
                value: "UnexpectedEvent");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 5,
                column: "Type",
                value: "WrongAddress");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 6,
                column: "Type",
                value: "ProviderUnresponsive");

            migrationBuilder.UpdateData(
                table: "CancelTypes",
                keyColumn: "Id",
                keyValue: 7,
                column: "Type",
                value: "Other");
        }
    }
}
