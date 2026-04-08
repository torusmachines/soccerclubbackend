using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballDashboardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLockedAreasToScouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "locked_areas",
                schema: "stf",
                table: "scouts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "locked_areas",
                schema: "stf",
                table: "scouts");
        }
    }
}
