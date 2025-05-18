using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddScopeOfWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "BookingForm",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScopeOfWorkId",
                table: "BookingForm",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScopeOfWork",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeOfWork", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ScopeOfWork",
                columns: new[] { "Id", "WorkType" },
                values: new object[,]
                {
                    { 1, "Full Decoration & Styling" },
                    { 2, "Furniture Selection" },
                    { 3, "Lighting Setup" },
                    { 4, "Wall Paint / Wallpaper" },
                    { 5, "Curtains / Blinds" },
                    { 6, "Rugs / Carpets" },
                    { 7, "Wall Art & Decor Items" },
                    { 8, "Custom Built-ins or Shelving" },
                    { 9, "Indoor Plants" },
                    { 10, "Space Optimization" },
                    { 11, "Decluttering" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingForm_AccountId",
                table: "BookingForm",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingForm_ScopeOfWorkId",
                table: "BookingForm",
                column: "ScopeOfWorkId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingForm_Accounts_AccountId",
                table: "BookingForm",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingForm_ScopeOfWork_ScopeOfWorkId",
                table: "BookingForm",
                column: "ScopeOfWorkId",
                principalTable: "ScopeOfWork",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingForm_Accounts_AccountId",
                table: "BookingForm");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingForm_ScopeOfWork_ScopeOfWorkId",
                table: "BookingForm");

            migrationBuilder.DropTable(
                name: "ScopeOfWork");

            migrationBuilder.DropIndex(
                name: "IX_BookingForm_AccountId",
                table: "BookingForm");

            migrationBuilder.DropIndex(
                name: "IX_BookingForm_ScopeOfWorkId",
                table: "BookingForm");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "BookingForm");

            migrationBuilder.DropColumn(
                name: "ScopeOfWorkId",
                table: "BookingForm");
        }
    }
}
