using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CategoriesAcknowledgeMultiAssignGender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcknowledgedById",
                table: "Cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgedByName",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgmentComment",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IncidentLatitude",
                table: "Cases",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncidentLocationDescription",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IncidentLongitude",
                table: "Cases",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAcknowledged",
                table: "Cases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CaseAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseAssignments_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsConfidential = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AppliesToType = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SentEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    To = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RelatedCaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentEmails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CategoryId",
                table: "Cases",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseAssignments_CaseId_UserId_IsActive",
                table: "CaseAssignments",
                columns: new[] { "CaseId", "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseAssignments_UserId",
                table: "CaseAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCategories_Name",
                table: "CaseCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SentEmails_RelatedCaseId",
                table: "SentEmails",
                column: "RelatedCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_CaseCategories_CategoryId",
                table: "Cases",
                column: "CategoryId",
                principalTable: "CaseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_CaseCategories_CategoryId",
                table: "Cases");

            migrationBuilder.DropTable(
                name: "CaseAssignments");

            migrationBuilder.DropTable(
                name: "CaseCategories");

            migrationBuilder.DropTable(
                name: "SentEmails");

            migrationBuilder.DropIndex(
                name: "IX_Cases_CategoryId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AcknowledgedById",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AcknowledgedByName",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AcknowledgmentComment",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IncidentLatitude",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IncidentLocationDescription",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IncidentLongitude",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IsAcknowledged",
                table: "Cases");
        }
    }
}
