using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddScopeOfWorkForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingForm_Accounts_AccountId",
                table: "BookingForm");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingForm_ScopeOfWork_ScopeOfWorkId",
                table: "BookingForm");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BookingForm_BookingFormId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FormImage_BookingForm_BookingFormId",
                table: "FormImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScopeOfWork",
                table: "ScopeOfWork");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingForm",
                table: "BookingForm");

            migrationBuilder.DropIndex(
                name: "IX_BookingForm_ScopeOfWorkId",
                table: "BookingForm");

            migrationBuilder.DropColumn(
                name: "ScopeOfWorkId",
                table: "BookingForm");

            migrationBuilder.RenameTable(
                name: "ScopeOfWork",
                newName: "ScopeOfWorks");

            migrationBuilder.RenameTable(
                name: "BookingForm",
                newName: "BookingForms");

            migrationBuilder.RenameIndex(
                name: "IX_BookingForm_AccountId",
                table: "BookingForms",
                newName: "IX_BookingForms_AccountId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScopeOfWorks",
                table: "ScopeOfWorks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingForms",
                table: "BookingForms",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ScopeOfWorkForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingFormId = table.Column<int>(type: "int", nullable: false),
                    ScopeOfWorkId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeOfWorkForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopeOfWorkForms_BookingForms_BookingFormId",
                        column: x => x.BookingFormId,
                        principalTable: "BookingForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScopeOfWorkForms_ScopeOfWorks_ScopeOfWorkId",
                        column: x => x.ScopeOfWorkId,
                        principalTable: "ScopeOfWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScopeOfWorkForms_BookingFormId",
                table: "ScopeOfWorkForms",
                column: "BookingFormId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeOfWorkForms_ScopeOfWorkId",
                table: "ScopeOfWorkForms",
                column: "ScopeOfWorkId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingForms_Accounts_AccountId",
                table: "BookingForms",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BookingForms_BookingFormId",
                table: "Bookings",
                column: "BookingFormId",
                principalTable: "BookingForms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FormImage_BookingForms_BookingFormId",
                table: "FormImage",
                column: "BookingFormId",
                principalTable: "BookingForms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingForms_Accounts_AccountId",
                table: "BookingForms");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BookingForms_BookingFormId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FormImage_BookingForms_BookingFormId",
                table: "FormImage");

            migrationBuilder.DropTable(
                name: "ScopeOfWorkForms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScopeOfWorks",
                table: "ScopeOfWorks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingForms",
                table: "BookingForms");

            migrationBuilder.RenameTable(
                name: "ScopeOfWorks",
                newName: "ScopeOfWork");

            migrationBuilder.RenameTable(
                name: "BookingForms",
                newName: "BookingForm");

            migrationBuilder.RenameIndex(
                name: "IX_BookingForms_AccountId",
                table: "BookingForm",
                newName: "IX_BookingForm_AccountId");

            migrationBuilder.AddColumn<int>(
                name: "ScopeOfWorkId",
                table: "BookingForm",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScopeOfWork",
                table: "ScopeOfWork",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingForm",
                table: "BookingForm",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BookingForm_ScopeOfWorkId",
                table: "BookingForm",
                column: "ScopeOfWorkId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingForm_Accounts_AccountId",
                table: "BookingForm",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingForm_ScopeOfWork_ScopeOfWorkId",
                table: "BookingForm",
                column: "ScopeOfWorkId",
                principalTable: "ScopeOfWork",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BookingForm_BookingFormId",
                table: "Bookings",
                column: "BookingFormId",
                principalTable: "BookingForm",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FormImage_BookingForm_BookingFormId",
                table: "FormImage",
                column: "BookingFormId",
                principalTable: "BookingForm",
                principalColumn: "Id");
        }
    }
}
