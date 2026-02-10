using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalkRallyApp.Migrations
{
    /// <inheritdoc />
    public partial class RenameTimeLimitMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeLimiMinutes",
                table: "Courses",
                newName: "TimeLimitMinutes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeLimitMinutes",
                table: "Courses",
                newName: "TimeLimiMinutes");
        }
    }
}
