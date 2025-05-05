using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordZoomMeeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingId",
                table: "ZoomMeetings");

            migrationBuilder.AddColumn<string>(
                name: "MeetingNumber",
                table: "ZoomMeetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "ZoomMeetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartUrl",
                table: "ZoomMeetings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingNumber",
                table: "ZoomMeetings");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "ZoomMeetings");

            migrationBuilder.DropColumn(
                name: "StartUrl",
                table: "ZoomMeetings");

            migrationBuilder.AddColumn<string>(
                name: "MeetingId",
                table: "ZoomMeetings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
