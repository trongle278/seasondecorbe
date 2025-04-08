using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    public partial class AddContract : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    TermOfUseContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContractFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignatureToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureTokenGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isContractExisted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Quotation_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_QuotationId",
                table: "Contracts",
                column: "QuotationId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contracts");
        }
    }
}
