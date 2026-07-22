using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOracleDbEngineSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DbEngineType",
                table: "Hackathon_Config",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OracleServiceName",
                table: "Hackathon_Config",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "Hackathon_Config",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DbEngineType",
                table: "Hackathon_Config");

            migrationBuilder.DropColumn(
                name: "OracleServiceName",
                table: "Hackathon_Config");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "Hackathon_Config");
        }
    }
}
