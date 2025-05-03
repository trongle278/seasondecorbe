using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationCreateAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "CertificateImage");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationCreateAt",
                table: "Accounts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationCreateAt",
                table: "Accounts");

            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "CertificateImage",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
