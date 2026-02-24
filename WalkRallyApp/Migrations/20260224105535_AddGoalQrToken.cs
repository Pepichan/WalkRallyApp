using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalkRallyApp.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalQrToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoalQrToken",
                table: "Courses",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoalQrToken",
                table: "Courses");
        }
    }
}
