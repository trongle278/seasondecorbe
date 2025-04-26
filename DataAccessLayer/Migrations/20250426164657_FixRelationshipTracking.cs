using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    public partial class FixRelationshipTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trackings_BookingId",
                table: "Trackings");

            migrationBuilder.CreateIndex(
                name: "IX_Trackings_BookingId",
                table: "Trackings",
                column: "BookingId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trackings_BookingId",
                table: "Trackings");

            migrationBuilder.CreateIndex(
                name: "IX_Trackings_BookingId",
                table: "Trackings",
                column: "BookingId",
                unique: true);
        }
    }
}
