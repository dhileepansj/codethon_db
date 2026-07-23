using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Survey_Surveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AllowMultiple = table.Column<bool>(type: "boolean", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    ThankYouMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_Surveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_Surveys_Hackathon_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Survey_EmailSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncludeRmByDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeVhByDefault = table.Column<bool>(type: "boolean", nullable: false),
                    AdditionalCcEmails = table.Column<string>(type: "text", nullable: true),
                    EmailSubject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailBody = table.Column<string>(type: "text", nullable: true),
                    ReminderEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderDays = table.Column<int>(type: "integer", nullable: false),
                    MaxReminders = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_EmailSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_EmailSettings_Survey_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Survey_Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Survey_Fields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldType = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Placeholder = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Options = table.Column<string>(type: "jsonb", nullable: true),
                    Validation = table.Column<string>(type: "jsonb", nullable: true),
                    SectionTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefaultValue = table.Column<string>(type: "text", nullable: true),
                    MatrixRows = table.Column<string>(type: "jsonb", nullable: true),
                    MatrixColumns = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_Fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_Fields_Survey_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Survey_Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Survey_Participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmployeeEmail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RmName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RmEmail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    VhName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VhEmail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_Participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_Participants_Survey_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Survey_Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Survey_FieldDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependsOnFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    OptionMap = table.Column<string>(type: "jsonb", nullable: true),
                    LogicGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    LogicOperator = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_FieldDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_FieldDependencies_Survey_Fields_DependsOnFieldId",
                        column: x => x.DependsOnFieldId,
                        principalTable: "Survey_Fields",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Survey_FieldDependencies_Survey_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Survey_Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Survey_Distributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IncludeRm = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeVh = table.Column<bool>(type: "boolean", nullable: false),
                    CcEmails = table.Column<string>(type: "text", nullable: true),
                    EmailStatus = table.Column<int>(type: "integer", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_Distributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_Distributions_Survey_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Survey_Participants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Survey_Distributions_Survey_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Survey_Surveys",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Survey_Otps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OtpHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResendCount = table.Column<int>(type: "integer", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_Otps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_Otps_Survey_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Survey_Participants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Survey_Otps_Survey_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Survey_Surveys",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Survey_ParticipantStatusLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeclinedBy = table.Column<int>(type: "integer", nullable: true),
                    DeclineReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DeclineAttachmentPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeclinedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    MarkedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_ParticipantStatusLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_ParticipantStatusLogs_Hackathon_Users_MarkedByUserId",
                        column: x => x.MarkedByUserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Survey_ParticipantStatusLogs_Survey_Participants_Participan~",
                        column: x => x.ParticipantId,
                        principalTable: "Survey_Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Survey_ParticipantStatusLogs_Survey_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Survey_Surveys",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Survey_ReminderLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReminderNumber = table.Column<int>(type: "integer", nullable: false),
                    EmailStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_ReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_ReminderLogs_Survey_Distributions_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "Survey_Distributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Survey_Responses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    DistributionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    TimeTakenSeconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_Responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_Responses_Survey_Distributions_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "Survey_Distributions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Survey_Responses_Survey_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Survey_Participants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Survey_Responses_Survey_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Survey_Surveys",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Survey_ResponseAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Survey_ResponseAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Survey_ResponseAnswers_Survey_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Survey_Fields",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Survey_ResponseAnswers_Survey_Responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "Survey_Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Distributions_EmailStatus",
                table: "Survey_Distributions",
                column: "EmailStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Distributions_ParticipantId",
                table: "Survey_Distributions",
                column: "ParticipantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Distributions_SurveyId_ParticipantId",
                table: "Survey_Distributions",
                columns: new[] { "SurveyId", "ParticipantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Distributions_Token",
                table: "Survey_Distributions",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Survey_EmailSettings_SurveyId",
                table: "Survey_EmailSettings",
                column: "SurveyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Survey_FieldDependencies_DependsOnFieldId",
                table: "Survey_FieldDependencies",
                column: "DependsOnFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_FieldDependencies_FieldId",
                table: "Survey_FieldDependencies",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Fields_SurveyId_SortOrder",
                table: "Survey_Fields",
                columns: new[] { "SurveyId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Otps_ExpiresAt",
                table: "Survey_Otps",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Otps_ParticipantId_SurveyId",
                table: "Survey_Otps",
                columns: new[] { "ParticipantId", "SurveyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Otps_SurveyId",
                table: "Survey_Otps",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Participants_BatchId",
                table: "Survey_Participants",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Participants_Status",
                table: "Survey_Participants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Participants_SurveyId_EmployeeEmail",
                table: "Survey_Participants",
                columns: new[] { "SurveyId", "EmployeeEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_Survey_ParticipantStatusLogs_MarkedByUserId",
                table: "Survey_ParticipantStatusLogs",
                column: "MarkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_ParticipantStatusLogs_ParticipantId",
                table: "Survey_ParticipantStatusLogs",
                column: "ParticipantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Survey_ParticipantStatusLogs_SurveyId",
                table: "Survey_ParticipantStatusLogs",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_ReminderLogs_DistributionId",
                table: "Survey_ReminderLogs",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_ResponseAnswers_FieldId",
                table: "Survey_ResponseAnswers",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_ResponseAnswers_ResponseId",
                table: "Survey_ResponseAnswers",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_ResponseAnswers_ResponseId_FieldId",
                table: "Survey_ResponseAnswers",
                columns: new[] { "ResponseId", "FieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Responses_DistributionId",
                table: "Survey_Responses",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Responses_ParticipantId_SurveyId",
                table: "Survey_Responses",
                columns: new[] { "ParticipantId", "SurveyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Responses_SubmittedAt",
                table: "Survey_Responses",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Responses_SurveyId",
                table: "Survey_Responses",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Surveys_CreatedByUserId",
                table: "Survey_Surveys",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Surveys_IsDeleted",
                table: "Survey_Surveys",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Survey_Surveys_Status",
                table: "Survey_Surveys",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Survey_EmailSettings");

            migrationBuilder.DropTable(
                name: "Survey_FieldDependencies");

            migrationBuilder.DropTable(
                name: "Survey_Otps");

            migrationBuilder.DropTable(
                name: "Survey_ParticipantStatusLogs");

            migrationBuilder.DropTable(
                name: "Survey_ReminderLogs");

            migrationBuilder.DropTable(
                name: "Survey_ResponseAnswers");

            migrationBuilder.DropTable(
                name: "Survey_Fields");

            migrationBuilder.DropTable(
                name: "Survey_Responses");

            migrationBuilder.DropTable(
                name: "Survey_Distributions");

            migrationBuilder.DropTable(
                name: "Survey_Participants");

            migrationBuilder.DropTable(
                name: "Survey_Surveys");
        }
    }
}
