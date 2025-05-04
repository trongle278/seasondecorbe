using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddCancelQuotationNTrustedDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTrustDepositPaid",
                table: "Bookings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TrustDepositAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTrustDepositPaid",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TrustDepositAmount",
                table: "Bookings");
        }
    }
}
