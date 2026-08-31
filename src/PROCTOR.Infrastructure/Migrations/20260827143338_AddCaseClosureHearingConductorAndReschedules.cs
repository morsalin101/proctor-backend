using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseClosureHearingConductorAndReschedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConductedAt",
                table: "Hearings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConductedById",
                table: "Hearings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConductedByName",
                table: "Hearings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reschedules",
                table: "Hearings",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedByName",
                table: "Cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosingMessage",
                table: "Cases",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConductedAt",
                table: "Hearings");

            migrationBuilder.DropColumn(
                name: "ConductedById",
                table: "Hearings");

            migrationBuilder.DropColumn(
                name: "ConductedByName",
                table: "Hearings");

            migrationBuilder.DropColumn(
                name: "Reschedules",
                table: "Hearings");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ClosedByName",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ClosingMessage",
                table: "Cases");
        }
    }
}
