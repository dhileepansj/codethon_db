using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManualTesting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hackathon_ManualTestScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssessmentId = table.Column<int>(type: "integer", nullable: false),
                    ScenarioId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Scenario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MustTest = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PassFail = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_ManualTestScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_ManualTestScenarios_Hackathon_Assessments_Assessm~",
                        column: x => x.AssessmentId,
                        principalTable: "Hackathon_Assessments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Hackathon_ManualTestScenarios_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_ManualTestCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScenarioDbId = table.Column<int>(type: "integer", nullable: false),
                    TestCaseId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StepNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InputSpecification = table.Column<string>(type: "text", nullable: true),
                    HelpRemarks = table.Column<string>(type: "text", nullable: true),
                    InputTestData = table.Column<string>(type: "text", nullable: true),
                    ExpectedResult = table.Column<string>(type: "text", nullable: true),
                    ActualResult = table.Column<string>(type: "text", nullable: true),
                    StepResult = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_ManualTestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_ManualTestCases_Hackathon_ManualTestScenarios_Sce~",
                        column: x => x.ScenarioDbId,
                        principalTable: "Hackathon_ManualTestScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_ManualTestCases_ScenarioDbId",
                table: "Hackathon_ManualTestCases",
                column: "ScenarioDbId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_ManualTestScenarios_AssessmentId",
                table: "Hackathon_ManualTestScenarios",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_ManualTestScenarios_UserId_AssessmentId",
                table: "Hackathon_ManualTestScenarios",
                columns: new[] { "UserId", "AssessmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hackathon_ManualTestCases");

            migrationBuilder.DropTable(
                name: "Hackathon_ManualTestScenarios");
        }
    }
}
