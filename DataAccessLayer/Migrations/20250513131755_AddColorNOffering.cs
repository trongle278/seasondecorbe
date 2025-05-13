using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddColorNOffering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DecorationStyles_DecorationStyleId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DecorationStyles_DecorationStyleId1",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_DecorationStyleId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_DecorationStyleId1",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DecorationStyleId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DecorationStyleId1",
                table: "Accounts");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecorationStyleId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DecorServiceStyle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DecorServiceId = table.Column<int>(type: "int", nullable: false),
                    DecorationStyleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorServiceStyle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecorServiceStyle_DecorServices_DecorServiceId",
                        column: x => x.DecorServiceId,
                        principalTable: "DecorServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecorServiceStyle_DecorationStyles_DecorationStyleId",
                        column: x => x.DecorationStyleId,
                        principalTable: "DecorationStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Offering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offering", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThemeColor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColorCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemeColor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecorServiceOffering",
                columns: table => new
                {
                    DecorServiceId = table.Column<int>(type: "int", nullable: false),
                    OfferingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorServiceOffering", x => new { x.DecorServiceId, x.OfferingId });
                    table.ForeignKey(
                        name: "FK_DecorServiceOffering_DecorServices_DecorServiceId",
                        column: x => x.DecorServiceId,
                        principalTable: "DecorServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecorServiceOffering_Offering_OfferingId",
                        column: x => x.OfferingId,
                        principalTable: "Offering",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingThemeColor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    ThemeColorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingThemeColor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingThemeColor_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingThemeColor_ThemeColor_ThemeColorId",
                        column: x => x.ThemeColorId,
                        principalTable: "ThemeColor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DecorServiceThemeColor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DecorServiceId = table.Column<int>(type: "int", nullable: false),
                    ThemeColorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorServiceThemeColor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecorServiceThemeColor_DecorServices_DecorServiceId",
                        column: x => x.DecorServiceId,
                        principalTable: "DecorServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecorServiceThemeColor_ThemeColor_ThemeColorId",
                        column: x => x.ThemeColorId,
                        principalTable: "ThemeColor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Offering",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Materials and methods that are environmentally friendly", "Eco-friendly Package" },
                    { 2, "Furniture and items designed to match the selected theme", "Theme Furniture" },
                    { 3, "One-on-one consultation support with experts", "Consultation Support" },
                    { 4, "Harmonized color scheme across decor elements", "Color Palette Matching" },
                    { 5, "Design on request", "Custom Design" },
                    { 6, "Create a highlight for the space", "Visual Focal Point" },
                    { 7, "Support in selecting and arranging furniture related to the service", "Artwork & Decor Placement" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_DecorationStyleId",
                table: "Bookings",
                column: "DecorationStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingThemeColor_BookingId",
                table: "BookingThemeColor",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingThemeColor_ThemeColorId",
                table: "BookingThemeColor",
                column: "ThemeColorId");

            migrationBuilder.CreateIndex(
                name: "IX_DecorServiceOffering_OfferingId",
                table: "DecorServiceOffering",
                column: "OfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_DecorServiceStyle_DecorationStyleId",
                table: "DecorServiceStyle",
                column: "DecorationStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_DecorServiceStyle_DecorServiceId",
                table: "DecorServiceStyle",
                column: "DecorServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_DecorServiceThemeColor_DecorServiceId",
                table: "DecorServiceThemeColor",
                column: "DecorServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_DecorServiceThemeColor_ThemeColorId",
                table: "DecorServiceThemeColor",
                column: "ThemeColorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_DecorationStyles_DecorationStyleId",
                table: "Bookings",
                column: "DecorationStyleId",
                principalTable: "DecorationStyles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_DecorationStyles_DecorationStyleId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "BookingThemeColor");

            migrationBuilder.DropTable(
                name: "DecorServiceOffering");

            migrationBuilder.DropTable(
                name: "DecorServiceStyle");

            migrationBuilder.DropTable(
                name: "DecorServiceThemeColor");

            migrationBuilder.DropTable(
                name: "Offering");

            migrationBuilder.DropTable(
                name: "ThemeColor");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_DecorationStyleId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "DecorationStyleId",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "DecorationStyleId",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecorationStyleId1",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_DecorationStyleId",
                table: "Accounts",
                column: "DecorationStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_DecorationStyleId1",
                table: "Accounts",
                column: "DecorationStyleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_DecorationStyles_DecorationStyleId",
                table: "Accounts",
                column: "DecorationStyleId",
                principalTable: "DecorationStyles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_DecorationStyles_DecorationStyleId1",
                table: "Accounts",
                column: "DecorationStyleId1",
                principalTable: "DecorationStyles",
                principalColumn: "Id");
        }
    }
}
