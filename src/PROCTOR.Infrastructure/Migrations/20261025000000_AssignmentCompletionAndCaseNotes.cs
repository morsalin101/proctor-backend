using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PROCTOR.Infrastructure.Data;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations;

[DbContext(typeof(ProctorDbContext))]
[Migration("20261025000000_AssignmentCompletionAndCaseNotes")]
public partial class AssignmentCompletionAndCaseNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CompletedAt",
            table: "CaseAssignments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CompletionAction",
            table: "CaseAssignments",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "CaseAssignments",
            type: "text",
            nullable: false,
            defaultValue: "Pending");

        migrationBuilder.Sql("""
            UPDATE "CaseAssignments" AS a
            SET "Status" = CASE
                    WHEN NOT a."IsActive" THEN 'Completed'
                    WHEN c."Status" IN ('Resolved', 'Closed', 'Rejected', 'PoliceCase', 'ForwardedToRegistrar') THEN 'Completed'
                    ELSE 'Pending'
                END,
                "IsActive" = CASE
                    WHEN NOT a."IsActive" OR c."Status" IN ('Resolved', 'Closed', 'Rejected', 'PoliceCase', 'ForwardedToRegistrar') THEN FALSE
                    ELSE TRUE
                END,
                "IsPrimary" = CASE
                    WHEN NOT a."IsActive" OR c."Status" IN ('Resolved', 'Closed', 'Rejected', 'PoliceCase', 'ForwardedToRegistrar') THEN FALSE
                    ELSE a."IsPrimary"
                END,
                "CompletedAt" = CASE
                    WHEN NOT a."IsActive" OR c."Status" IN ('Resolved', 'Closed', 'Rejected', 'PoliceCase', 'ForwardedToRegistrar')
                        THEN COALESCE(c."ClosedAt", a."UpdatedAt", a."AssignedAt")
                    ELSE NULL
                END,
                "CompletionAction" = CASE
                    WHEN NOT a."IsActive" THEN 'legacy-completed'
                    WHEN c."Status" IN ('Resolved', 'Closed', 'Rejected', 'PoliceCase', 'ForwardedToRegistrar') THEN 'terminal-case'
                    ELSE NULL
                END
            FROM "Cases" AS c
            WHERE c."Id" = a."CaseId";
            """);

        migrationBuilder.Sql("UPDATE \"Cases\" SET \"AssignedToId\" = NULL WHERE \"Status\" IN ('Resolved', 'Closed', 'Rejected', 'PoliceCase', 'ForwardedToRegistrar');");

        migrationBuilder.Sql("""
            INSERT INTO "Notes" ("Id", "CaseId", "Content", "Author", "CreatedAt", "UpdatedAt")
            SELECT "Id", "CaseId", "Content", "Author", "CreatedAt", "UpdatedAt"
            FROM "CaseAdditionalInfos"
            ON CONFLICT ("Id") DO NOTHING;
            """);

        migrationBuilder.Sql("DELETE FROM \"ForwardingRules\" WHERE \"ToRole\" = '__draft_report__';");
        migrationBuilder.DropTable(name: "CaseAdditionalInfos");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CaseAdditionalInfos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                Author = table.Column<string>(type: "text", nullable: false),
                AuthorRole = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseAdditionalInfos", x => x.Id);
                table.ForeignKey(
                    name: "FK_CaseAdditionalInfos_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CaseAdditionalInfos_CaseId",
            table: "CaseAdditionalInfos",
            column: "CaseId");

        migrationBuilder.DropColumn(name: "CompletedAt", table: "CaseAssignments");
        migrationBuilder.DropColumn(name: "CompletionAction", table: "CaseAssignments");
        migrationBuilder.DropColumn(name: "Status", table: "CaseAssignments");
    }
}
