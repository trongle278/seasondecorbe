using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    public partial class UpdateTblBookingLaborDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ConstructionDetails",
                newName: "LaborDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConstructionDetails",
                table: "LaborDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LaborDetails",
                table: "LaborDetails",
                column: "Id");

            migrationBuilder.DropForeignKey(
                name: "FK_ConstructionDetails_Quotation_QuotationId",
                table: "LaborDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_LaborDetails_Quotation_QuotationId",
                table: "LaborDetails",
                column: "QuotationId",
                principalTable: "Quotation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.RenameIndex(
                name: "IX_ConstructionDetails_QuotationId",
                newName: "IX_LaborDetails_QuotationId",
                table: "LaborDetails");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "LaborDetails");

            migrationBuilder.RenameColumn(
                name: "Width",
                table: "LaborDetails",
                newName: "Area");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Subscriptions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[] { "Id", "AutoRenew", "CommissionDiscount", "Duration", "EndDate", "Name", "Price", "PrioritySupport", "StartDate", "Status", "VoucherCount" },
                values: new object[] { 1, false, 1.5, 30, null, "Silver", 100000m, false, null, 1, 3 });

            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[] { "Id", "AutoRenew", "CommissionDiscount", "Duration", "EndDate", "Name", "Price", "PrioritySupport", "StartDate", "Status", "VoucherCount" },
                values: new object[] { 2, false, 3.25, 30, null, "Gold", 200000m, true, null, 1, 5 });

            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[] { "Id", "AutoRenew", "CommissionDiscount", "Duration", "EndDate", "Name", "Price", "PrioritySupport", "StartDate", "Status", "VoucherCount" },
                values: new object[] { 3, false, 6.5, 30, null, "Platinum", 500000m, true, null, 1, 10 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "Area",
                table: "LaborDetails",
                newName: "Width");

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "Subscriptions",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "LaborDetails",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.RenameIndex(
                name: "IX_LaborDetails_QuotationId",
                newName: "IX_ConstructionDetails_QuotationId",
                table: "LaborDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_LaborDetails_Quotation_QuotationId",
                table: "LaborDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_ConstructionDetails_Quotation_QuotationId",
                table: "LaborDetails",
                column: "QuotationId",
                principalTable: "Quotation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropPrimaryKey(
                name: "PK_LaborDetails",
                table: "LaborDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConstructionDetails",
                table: "LaborDetails",
                column: "Id");

            migrationBuilder.RenameTable(
                name: "LaborDetails",
                newName: "ConstructionDetails");
        }
    }
}
