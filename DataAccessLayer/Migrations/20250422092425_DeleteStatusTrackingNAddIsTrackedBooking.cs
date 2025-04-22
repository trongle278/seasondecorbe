using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    public partial class DeleteStatusTrackingNAddIsTrackedBooking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Trackings");

            migrationBuilder.AddColumn<bool>(
                name: "IsTracked",
                table: "Bookings",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTracked",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Trackings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
