using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseSubmitterGender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmitterGender",
                table: "Cases",
                type: "text",
                nullable: false,
                defaultValue: "Unspecified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmitterGender",
                table: "Cases");
        }
    }
}
