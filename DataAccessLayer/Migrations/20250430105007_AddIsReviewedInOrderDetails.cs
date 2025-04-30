using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddIsReviewedInOrderDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "VoucherCount",
                table: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Subscriptions",
                newName: "RequiredSpending");

            migrationBuilder.AddColumn<bool>(
                name: "IsReviewed",
                table: "OrderDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CommissionDiscount", "FreeRequestChange", "RequiredSpending" },
                values: new object[] { 0.0, 0, 0m });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CommissionDiscount", "FreeRequestChange", "Name", "PrioritySupport", "RequiredSpending" },
                values: new object[] { 0.20000000000000001, 1, "Silver", false, 1m });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CommissionDiscount", "FreeRequestChange", "Name", "RequiredSpending" },
                values: new object[] { 0.5, 2, "Gold", 20000000m });

            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[] { "Id", "CommissionDiscount", "FreeRequestChange", "Name", "PrioritySupport", "RequiredSpending" },
                values: new object[] { 4, 1.0, 5, "Platinum", true, 60000000m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "IsReviewed",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "RequiredSpending",
                table: "Subscriptions",
                newName: "Price");

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "Subscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VoucherCount",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AutoRenew", "CommissionDiscount", "Duration", "EndDate", "FreeRequestChange", "Price", "StartDate", "Status", "VoucherCount" },
                values: new object[] { false, 1.5, 30, null, 3, 100000m, null, 1, 10 });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AutoRenew", "CommissionDiscount", "Duration", "EndDate", "FreeRequestChange", "Name", "Price", "PrioritySupport", "StartDate", "Status", "VoucherCount" },
                values: new object[] { false, 3.25, 30, null, 5, "Gold", 200000m, true, null, 1, 20 });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AutoRenew", "CommissionDiscount", "Duration", "EndDate", "FreeRequestChange", "Name", "Price", "StartDate", "Status", "VoucherCount" },
                values: new object[] { false, 6.5, 30, null, 10, "Platinum", 500000m, null, 1, 40 });
        }
    }
}
