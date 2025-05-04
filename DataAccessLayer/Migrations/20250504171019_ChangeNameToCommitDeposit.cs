using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNameToCommitDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TrustDepositAmount",
                table: "Bookings",
                newName: "CommitDepositAmount");

            migrationBuilder.RenameColumn(
                name: "IsTrustDepositPaid",
                table: "Bookings",
                newName: "IsCommitDepositPaid");

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Quotation",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelTypeId",
                table: "Quotation",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotation_CancelTypeId",
                table: "Quotation",
                column: "CancelTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotation_CancelTypes_CancelTypeId",
                table: "Quotation",
                column: "CancelTypeId",
                principalTable: "CancelTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotation_CancelTypes_CancelTypeId",
                table: "Quotation");

            migrationBuilder.DropIndex(
                name: "IX_Quotation_CancelTypeId",
                table: "Quotation");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Quotation");

            migrationBuilder.DropColumn(
                name: "CancelTypeId",
                table: "Quotation");

            migrationBuilder.RenameColumn(
                name: "IsCommitDepositPaid",
                table: "Bookings",
                newName: "IsTrustDepositPaid");

            migrationBuilder.RenameColumn(
                name: "CommitDepositAmount",
                table: "Bookings",
                newName: "TrustDepositAmount");
        }
    }
}
