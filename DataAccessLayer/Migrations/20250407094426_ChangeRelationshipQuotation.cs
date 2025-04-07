using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    public partial class ChangeRelationshipQuotation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotation_BookingId",
                table: "Quotation");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_BookingId",
                table: "Quotation",
                column: "BookingId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotation_BookingId",
                table: "Quotation");

            migrationBuilder.AddColumn<int>(
                name: "QuotationId",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_BookingId",
                table: "Quotation",
                column: "BookingId",
                unique: true);
        }
    }
}
