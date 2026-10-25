using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PROCTOR.Infrastructure.Data;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations;

[DbContext(typeof(ProctorDbContext))]
[Migration("20261026000000_AddAccusedRiskAnalytics")]
public partial class AddAccusedRiskAnalytics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "OutstandingDues", table: "Students", type: "numeric(12,2)", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "CompletedCredits", table: "Students", type: "numeric(6,2)", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "ExpectedCredits", table: "Students", type: "numeric(6,2)", nullable: true);
        migrationBuilder.AddColumn<int>(name: "CurrentSemester", table: "Students", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "RegistrationStatus", table: "Students", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "UnregistrationReason", table: "Students", type: "character varying(300)", maxLength: 300, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "RiskDataAsOf", table: "Students", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "RiskSeverity", table: "CaseCategories", type: "text", nullable: false, defaultValue: "Moderate");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "OutstandingDues", table: "Students");
        migrationBuilder.DropColumn(name: "CompletedCredits", table: "Students");
        migrationBuilder.DropColumn(name: "ExpectedCredits", table: "Students");
        migrationBuilder.DropColumn(name: "CurrentSemester", table: "Students");
        migrationBuilder.DropColumn(name: "RegistrationStatus", table: "Students");
        migrationBuilder.DropColumn(name: "UnregistrationReason", table: "Students");
        migrationBuilder.DropColumn(name: "RiskDataAsOf", table: "Students");
        migrationBuilder.DropColumn(name: "RiskSeverity", table: "CaseCategories");
    }
}
