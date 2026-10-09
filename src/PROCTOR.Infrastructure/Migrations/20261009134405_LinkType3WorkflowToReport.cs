using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkType3WorkflowToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReportId",
                table: "Type3Workflows",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Type3Workflows" AS workflow
                SET "ReportId" = (
                    SELECT report."Id"
                    FROM "Reports" AS report
                    WHERE report."CaseId" = workflow."CaseId" AND report."IsFinal" = TRUE
                    ORDER BY report."CreatedAt" DESC
                    LIMIT 1
                );
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportId",
                table: "Type3Workflows",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Type3Workflows_ReportId",
                table: "Type3Workflows",
                column: "ReportId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Type3Workflows_Reports_ReportId",
                table: "Type3Workflows",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Type3Workflows_Reports_ReportId",
                table: "Type3Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Type3Workflows_ReportId",
                table: "Type3Workflows");

            migrationBuilder.DropColumn(
                name: "ReportId",
                table: "Type3Workflows");
        }
    }
}
