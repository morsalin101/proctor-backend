using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PROCTOR.Infrastructure.Data;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations;

[DbContext(typeof(ProctorDbContext))]
[Migration("20260917000000_AddCaseAcademicMetrics")]
public partial class AddCaseAcademicMetrics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "StudentSemester", table: "Cases", type: "integer", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "StudentCgpa", table: "Cases", type: "numeric", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "StudentSemester", table: "Cases");
        migrationBuilder.DropColumn(name: "StudentCgpa", table: "Cases");
    }
}
