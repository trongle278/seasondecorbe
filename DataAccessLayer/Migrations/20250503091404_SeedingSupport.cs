using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class SeedingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TicketTypes",
                columns: new[] { "Id", "Type" },
                values: new object[,]
                {
                    { 1, "Service delay" },
                    { 2, "Incorrect service implementation" },
                    { 3, "Unprofessional behavior" },
                    { 4, "Damage or loss report" },
                    { 5, "Poor material quality" },
                    { 6, "Other" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "TicketTypes",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
