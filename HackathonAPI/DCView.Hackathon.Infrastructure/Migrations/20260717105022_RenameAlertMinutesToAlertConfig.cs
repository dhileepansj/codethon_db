using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAlertMinutesToAlertConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertMinutes",
                table: "Hackathon_Schedule");

            migrationBuilder.AddColumn<string>(
                name: "AlertConfig",
                table: "Hackathon_Schedule",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertConfig",
                table: "Hackathon_Schedule");

            migrationBuilder.AddColumn<string>(
                name: "AlertMinutes",
                table: "Hackathon_Schedule",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
