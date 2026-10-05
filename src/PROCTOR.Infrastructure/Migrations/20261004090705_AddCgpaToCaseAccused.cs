using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROCTOR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCgpaToCaseAccused : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cgpa",
                table: "CaseAccusedPersons",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cgpa",
                table: "CaseAccusedPersons");
        }
    }
}
