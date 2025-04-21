using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    public partial class AddIsDepositednIsFinalPaid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isDeposited",
                table: "Contracts",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isFinalPaid",
                table: "Contracts",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isDeposited",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "isFinalPaid",
                table: "Contracts");
        }
    }
}
