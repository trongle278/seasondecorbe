using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    public partial class AddTrackingImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Trackings");

            migrationBuilder.CreateTable(
                name: "TrackingImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackingId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingImages_Trackings_TrackingId",
                        column: x => x.TrackingId,
                        principalTable: "Trackings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingImages_TrackingId",
                table: "TrackingImages",
                column: "TrackingId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackingImages");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Trackings",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
