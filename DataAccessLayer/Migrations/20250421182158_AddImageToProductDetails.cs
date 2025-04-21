using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    public partial class AddImageToProductDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "ProductDetails",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "ProductDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "ProductDetails");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ProductDetails",
                newName: "id");
        }
    }
}
