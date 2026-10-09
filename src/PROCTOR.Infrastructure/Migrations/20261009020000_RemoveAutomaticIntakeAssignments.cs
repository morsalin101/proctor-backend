using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PROCTOR.Infrastructure.Data;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations;

[DbContext(typeof(ProctorDbContext))]
[Migration("20261009020000_RemoveAutomaticIntakeAssignments")]
public partial class RemoveAutomaticIntakeAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Earlier intake routing incorrectly made the receiving administrative officer a
        // handler. These rows are distinguishable because the student submitter is recorded
        // as assigning a Coordinator/FemaleCoordinator and the row was never primary.
        migrationBuilder.Sql("""
            UPDATE "Cases" AS c
            SET "AssignedToId" = NULL, "UpdatedAt" = NOW()
            FROM "CaseAssignments" AS a
            INNER JOIN "Users" AS u ON u."Id" = a."UserId"
            WHERE a."CaseId" = c."Id"
              AND c."AssignedToId" = a."UserId"
              AND a."AssignedById" = c."SubmittedByUserId"
              AND a."IsPrimary" = FALSE
              AND u."Role" IN ('Coordinator', 'FemaleCoordinator');

            UPDATE "CaseAssignments" AS a
            SET "IsActive" = FALSE, "UpdatedAt" = NOW()
            FROM "Cases" AS c, "Users" AS u
            WHERE a."CaseId" = c."Id"
              AND a."UserId" = u."Id"
              AND a."AssignedById" = c."SubmittedByUserId"
              AND a."IsPrimary" = FALSE
              AND u."Role" IN ('Coordinator', 'FemaleCoordinator');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data repair is intentionally not reversed: these were routing records, not real
        // user assignments, and restoring them would recreate the incorrect responsibility.
    }
}
