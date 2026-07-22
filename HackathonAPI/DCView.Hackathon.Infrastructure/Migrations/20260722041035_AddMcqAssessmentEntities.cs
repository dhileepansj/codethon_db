using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMcqAssessmentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssessmentId",
                table: "Hackathon_Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Hackathon_Assessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    TotalQuestions = table.Column<int>(type: "integer", nullable: false),
                    MaxMarks = table.Column<int>(type: "integer", nullable: false),
                    SimplePercentage = table.Column<int>(type: "integer", nullable: false),
                    MediumPercentage = table.Column<int>(type: "integer", nullable: false),
                    ComplexPercentage = table.Column<int>(type: "integer", nullable: false),
                    SimpleMarks = table.Column<int>(type: "integer", nullable: false),
                    MediumMarks = table.Column<int>(type: "integer", nullable: false),
                    ComplexMarks = table.Column<int>(type: "integer", nullable: false),
                    NegativeMarking = table.Column<bool>(type: "boolean", nullable: false),
                    NegativeMarkValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ShuffleQuestions = table.Column<bool>(type: "boolean", nullable: false),
                    ShuffleOptions = table.Column<bool>(type: "boolean", nullable: false),
                    ShowResultImmediately = table.Column<bool>(type: "boolean", nullable: false),
                    PassPercentage = table.Column<int>(type: "integer", nullable: false),
                    AllowNavigation = table.Column<bool>(type: "boolean", nullable: false),
                    AllowReview = table.Column<bool>(type: "boolean", nullable: false),
                    AutoSubmitOnTimeout = table.Column<bool>(type: "boolean", nullable: false),
                    OneQuestionPerPage = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_Assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_McqQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssessmentId = table.Column<int>(type: "integer", nullable: false),
                    SNo = table.Column<int>(type: "integer", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    OptionA = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OptionB = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OptionC = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OptionD = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CorrectAnswer = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Complexity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Marks = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_McqQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_McqQuestions_Hackathon_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Hackathon_Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_McqTests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssessmentId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TimeSpentSeconds = table.Column<int>(type: "integer", nullable: true),
                    TotalQuestions = table.Column<int>(type: "integer", nullable: false),
                    Attempted = table.Column<int>(type: "integer", nullable: false),
                    Correct = table.Column<int>(type: "integer", nullable: false),
                    Wrong = table.Column<int>(type: "integer", nullable: false),
                    Skipped = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: true),
                    IsAutoSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    IsInProgress = table.Column<bool>(type: "boolean", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    QuestionOrder = table.Column<string>(type: "text", nullable: false),
                    OptionOrder = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_McqTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_McqTests_Hackathon_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Hackathon_Assessments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Hackathon_McqTests_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_McqAnswers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TestId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    SelectedAnswer = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    MarksAwarded = table.Column<decimal>(type: "numeric", nullable: false),
                    TimeTakenSeconds = table.Column<int>(type: "integer", nullable: true),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    QuestionIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_McqAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_McqAnswers_Hackathon_McqQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Hackathon_McqQuestions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Hackathon_McqAnswers_Hackathon_McqTests_TestId",
                        column: x => x.TestId,
                        principalTable: "Hackathon_McqTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Hackathon_Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "AssessmentId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_Users_AssessmentId",
                table: "Hackathon_Users",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_Assessments_IsActive",
                table: "Hackathon_Assessments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_Assessments_Type",
                table: "Hackathon_Assessments",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_McqAnswers_QuestionId",
                table: "Hackathon_McqAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_McqAnswers_TestId",
                table: "Hackathon_McqAnswers",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_McqAnswers_TestId_QuestionId",
                table: "Hackathon_McqAnswers",
                columns: new[] { "TestId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_McqQuestions_AssessmentId",
                table: "Hackathon_McqQuestions",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_McqQuestions_AssessmentId_Complexity",
                table: "Hackathon_McqQuestions",
                columns: new[] { "AssessmentId", "Complexity" });

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_McqTests_AssessmentId",
                table: "Hackathon_McqTests",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_McqTests_UserId",
                table: "Hackathon_McqTests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_McqTests_UserId_AssessmentId",
                table: "Hackathon_McqTests",
                columns: new[] { "UserId", "AssessmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Hackathon_Users_Hackathon_Assessments_AssessmentId",
                table: "Hackathon_Users",
                column: "AssessmentId",
                principalTable: "Hackathon_Assessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hackathon_Users_Hackathon_Assessments_AssessmentId",
                table: "Hackathon_Users");

            migrationBuilder.DropTable(
                name: "Hackathon_McqAnswers");

            migrationBuilder.DropTable(
                name: "Hackathon_McqQuestions");

            migrationBuilder.DropTable(
                name: "Hackathon_McqTests");

            migrationBuilder.DropTable(
                name: "Hackathon_Assessments");

            migrationBuilder.DropIndex(
                name: "IX_Hackathon_Users_AssessmentId",
                table: "Hackathon_Users");

            migrationBuilder.DropColumn(
                name: "AssessmentId",
                table: "Hackathon_Users");
        }
    }
}
