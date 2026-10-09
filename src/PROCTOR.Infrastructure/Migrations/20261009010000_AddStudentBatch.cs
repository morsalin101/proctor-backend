using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PROCTOR.Infrastructure.Data;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations;

[DbContext(typeof(ProctorDbContext))]
[Migration("20261009010000_AddStudentBatch")]
public partial class AddStudentBatch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Batch",
            table: "Students",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        // Batch format is DEPARTMENT_NUMBER. The first three digits in the university ID
        // are the batch number; new records can supply an explicit value when an institution
        // uses a different convention.
        migrationBuilder.Sql("""
            UPDATE "Students"
            SET "Batch" =
                LEFT(UPPER(COALESCE(NULLIF(REGEXP_REPLACE("Department", '[^A-Za-z0-9]+', '', 'g'), ''), 'UNKNOWN')), 60)
                || '_'
                || COALESCE(
                    NULLIF(SUBSTRING(REGEXP_REPLACE("StudentId", '[^0-9]+', '', 'g') FROM 1 FOR 3), ''),
                    '000');
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Batch",
            table: "Students",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Students_Batch",
            table: "Students",
            column: "Batch");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Students_Batch", table: "Students");
        migrationBuilder.DropColumn(name: "Batch", table: "Students");
    }
}
