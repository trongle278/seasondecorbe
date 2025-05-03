using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProviderProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecorationStyleId",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PastProjects",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PastWorkPlaces",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SkillId",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationHistories_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CertificateImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateImage_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DecorationStyle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorationStyle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skill",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skill", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_DecorationStyleId",
                table: "Accounts",
                column: "DecorationStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SkillId",
                table: "Accounts",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationHistories_AccountId",
                table: "ApplicationHistories",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateImage_AccountId",
                table: "CertificateImage",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_DecorationStyle_DecorationStyleId",
                table: "Accounts",
                column: "DecorationStyleId",
                principalTable: "DecorationStyle",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Skill_SkillId",
                table: "Accounts",
                column: "SkillId",
                principalTable: "Skill",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DecorationStyle_DecorationStyleId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Skill_SkillId",
                table: "Accounts");

            migrationBuilder.DropTable(
                name: "ApplicationHistories");

            migrationBuilder.DropTable(
                name: "CertificateImage");

            migrationBuilder.DropTable(
                name: "DecorationStyle");

            migrationBuilder.DropTable(
                name: "Skill");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_DecorationStyleId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_SkillId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DecorationStyleId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PastProjects",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PastWorkPlaces",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SkillId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "Accounts");
        }
    }
}
