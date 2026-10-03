using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCaseSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseCategories_CaseSubjects_SubjectId",
                table: "CaseCategories");

            migrationBuilder.DropTable(
                name: "CaseSubjects");

            migrationBuilder.DropIndex(
                name: "IX_CaseCategories_SubjectId",
                table: "CaseCategories");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "CaseCategories");

            migrationBuilder.RenameColumn(
                name: "Subject",
                table: "Cases",
                newName: "AcademicSemester");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AcademicSemester",
                table: "Cases",
                newName: "Subject");

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                table: "CaseCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseSubjects", x => x.Id);
                });

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
    }
}
