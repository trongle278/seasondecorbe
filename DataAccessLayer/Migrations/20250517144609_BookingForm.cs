using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class BookingForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookingFormId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingForm",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpaceStyle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoomSize = table.Column<double>(type: "float", nullable: true),
                    Style = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThemeColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryUser = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingForm", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BookingFormId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormImage_BookingForm_BookingFormId",
                        column: x => x.BookingFormId,
                        principalTable: "BookingForm",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingFormId",
                table: "Bookings",
                column: "BookingFormId",
                unique: true,
                filter: "[BookingFormId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FormImage_BookingFormId",
                table: "FormImage",
                column: "BookingFormId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BookingForm_BookingFormId",
                table: "Bookings",
                column: "BookingFormId",
                principalTable: "BookingForm",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BookingForm_BookingFormId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "FormImage");

            migrationBuilder.DropTable(
                name: "BookingForm");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingFormId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingFormId",
                table: "Bookings");
        }
    }
}
