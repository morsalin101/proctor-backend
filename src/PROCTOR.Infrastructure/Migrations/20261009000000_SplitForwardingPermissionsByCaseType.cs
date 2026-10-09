using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PROCTOR.Infrastructure.Data;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations;

[DbContext(typeof(ProctorDbContext))]
[Migration("20261009000000_SplitForwardingPermissionsByCaseType")]
public partial class SplitForwardingPermissionsByCaseType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing forwarding rules belong to the formal Type-2 workflow. Type-1 has its
        // own routing page and starts with only the Proctor's assign/close permissions.
        migrationBuilder.AddColumn<string>(
            name: "AppliesToType",
            table: "ForwardingRules",
            type: "text",
            nullable: false,
            defaultValue: "type-2");

        migrationBuilder.Sql("""
            INSERT INTO "ForwardingRules"
                ("Id", "FromRole", "ToRole", "AppliesToType", "ResultStatus", "IsActive", "CreatedAt", "UpdatedAt")
            SELECT gen_random_uuid(), 'proctor', '__assign__', 'type-1', 'assigned', TRUE, NOW(), NOW()
            WHERE NOT EXISTS (
                SELECT 1 FROM "ForwardingRules"
                WHERE "FromRole" = 'proctor' AND "ToRole" = '__assign__' AND "AppliesToType" = 'type-1');

            INSERT INTO "ForwardingRules"
                ("Id", "FromRole", "ToRole", "AppliesToType", "ResultStatus", "IsActive", "CreatedAt", "UpdatedAt")
            SELECT gen_random_uuid(), 'proctor', '__close__', 'type-1', 'closed', TRUE, NOW(), NOW()
            WHERE NOT EXISTS (
                SELECT 1 FROM "ForwardingRules"
                WHERE "FromRole" = 'proctor' AND "ToRole" = '__close__' AND "AppliesToType" = 'type-1');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AppliesToType",
            table: "ForwardingRules");
    }
}
