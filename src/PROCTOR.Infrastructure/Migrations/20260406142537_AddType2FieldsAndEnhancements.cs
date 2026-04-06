using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddType2FieldsAndEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFinal",
                table: "Reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UploadedByRole",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccusedContact",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccusedDepartment",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccusedGuardianContact",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccusedId",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccusedName",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IncidentDate",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentAdvisorName",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentContact",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentDepartment",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentFatherContact",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentFatherName",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "Cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoLink",
                table: "Cases",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFinal",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "UploadedByRole",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "AccusedContact",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AccusedDepartment",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AccusedGuardianContact",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AccusedId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AccusedName",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IncidentDate",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "StudentAdvisorName",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "StudentContact",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "StudentDepartment",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "StudentFatherContact",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "StudentFatherName",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "VideoLink",
                table: "Cases");
        }
    }
}
