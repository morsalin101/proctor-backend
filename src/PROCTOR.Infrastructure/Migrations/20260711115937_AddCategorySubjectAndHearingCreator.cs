using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySubjectAndHearingCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Hearings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Hearings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                table: "CaseCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCategories_SubjectId",
                table: "CaseCategories",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseCategories_CaseSubjects_SubjectId",
                table: "CaseCategories",
                column: "SubjectId",
                principalTable: "CaseSubjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseCategories_CaseSubjects_SubjectId",
                table: "CaseCategories");

            migrationBuilder.DropIndex(
                name: "IX_CaseCategories_SubjectId",
                table: "CaseCategories");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Hearings");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Hearings");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "CaseCategories");
        }
    }
}
