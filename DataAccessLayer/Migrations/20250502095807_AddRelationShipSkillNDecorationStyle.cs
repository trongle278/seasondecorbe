using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationShipSkillNDecorationStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DecorationStyle_DecorationStyleId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Skill_SkillId",
                table: "Accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Skill",
                table: "Skill");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DecorationStyle",
                table: "DecorationStyle");

            migrationBuilder.RenameTable(
                name: "Skill",
                newName: "Skills");

            migrationBuilder.RenameTable(
                name: "DecorationStyle",
                newName: "DecorationStyles");

            migrationBuilder.AddColumn<int>(
                name: "DecorationStyleId1",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Skills",
                table: "Skills",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DecorationStyles",
                table: "DecorationStyles",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_DecorationStyleId1",
                table: "Accounts",
                column: "DecorationStyleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_DecorationStyles_DecorationStyleId",
                table: "Accounts",
                column: "DecorationStyleId",
                principalTable: "DecorationStyles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_DecorationStyles_DecorationStyleId1",
                table: "Accounts",
                column: "DecorationStyleId1",
                principalTable: "DecorationStyles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Skills_SkillId",
                table: "Accounts",
                column: "SkillId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DecorationStyles_DecorationStyleId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DecorationStyles_DecorationStyleId1",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Skills_SkillId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_DecorationStyleId1",
                table: "Accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Skills",
                table: "Skills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DecorationStyles",
                table: "DecorationStyles");

            migrationBuilder.DropColumn(
                name: "DecorationStyleId1",
                table: "Accounts");

            migrationBuilder.RenameTable(
                name: "Skills",
                newName: "Skill");

            migrationBuilder.RenameTable(
                name: "DecorationStyles",
                newName: "DecorationStyle");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Skill",
                table: "Skill",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DecorationStyle",
                table: "DecorationStyle",
                column: "Id");

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
    }
}
