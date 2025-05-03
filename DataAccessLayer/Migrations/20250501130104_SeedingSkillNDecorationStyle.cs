using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class SeedingSkillNDecorationStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DecorationStyle",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Modern" },
                    { 2, "Traditional" },
                    { 3, "Coastal" },
                    { 4, "Scandinavian" },
                    { 5, "Industrial" }
                });

            migrationBuilder.InsertData(
                table: "Skill",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Lighting Decoration" },
                    { 2, "Space Decoration" },
                    { 3, "Art Decoration" },
                    { 4, "Event Decoration" },
                    { 5, "Eco-friendly Decoration" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DecorationStyle",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "DecorationStyle",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "DecorationStyle",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "DecorationStyle",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "DecorationStyle",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Skill",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Skill",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Skill",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Skill",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Skill",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
