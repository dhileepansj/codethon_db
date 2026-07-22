using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateManualTestFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualResult",
                table: "Hackathon_ManualTestCases");

            migrationBuilder.DropColumn(
                name: "StepResult",
                table: "Hackathon_ManualTestCases");

            migrationBuilder.RenameColumn(
                name: "HelpRemarks",
                table: "Hackathon_ManualTestCases",
                newName: "TestCaseDescription");

            migrationBuilder.AlterColumn<string>(
                name: "ScenarioId",
                table: "Hackathon_ManualTestScenarios",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "SNo",
                table: "Hackathon_ManualTestScenarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "TestCaseId",
                table: "Hackathon_ManualTestCases",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "SNo",
                table: "Hackathon_ManualTestCases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScenarioId",
                table: "Hackathon_ManualTestCases",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SNo",
                table: "Hackathon_ManualTestScenarios");

            migrationBuilder.DropColumn(
                name: "SNo",
                table: "Hackathon_ManualTestCases");

            migrationBuilder.DropColumn(
                name: "ScenarioId",
                table: "Hackathon_ManualTestCases");

            migrationBuilder.RenameColumn(
                name: "TestCaseDescription",
                table: "Hackathon_ManualTestCases",
                newName: "HelpRemarks");

            migrationBuilder.AlterColumn<string>(
                name: "ScenarioId",
                table: "Hackathon_ManualTestScenarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "TestCaseId",
                table: "Hackathon_ManualTestCases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "ActualResult",
                table: "Hackathon_ManualTestCases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StepResult",
                table: "Hackathon_ManualTestCases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
