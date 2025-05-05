using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "ZoomMeetings",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "MeetingId",
                table: "ZoomMeetings",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingId",
                table: "ZoomMeetings");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ZoomMeetings",
                newName: "id");
        }
    }
}
