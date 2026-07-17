using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hackathon_AiDetectionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BlockThreshold = table.Column<int>(type: "integer", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_AiDetectionSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_Config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AdminUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AdminPasswordEncrypted = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DbPrefix = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxQueryTimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxRowsPerPage = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_Config", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_QuestionPaper",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_QuestionPaper", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordResetRequested = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LoginCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_AiDetectionUserOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BlockThreshold = table.Column<int>(type: "integer", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_AiDetectionUserOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_AiDetectionUserOverrides_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_ExecutionHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DatabaseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    QueryText = table.Column<string>(type: "text", nullable: false),
                    QueryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RowsAffected = table.Column<int>(type: "integer", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_ExecutionHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_ExecutionHistory_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_Sessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DatabaseCreated = table.Column<bool>(type: "boolean", nullable: false),
                    DatabaseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DbLoginPassword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_Hackathon_Sessions_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_SubmissionFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileData = table.Column<byte[]>(type: "bytea", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_SubmissionFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_SubmissionFiles_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_TabSwitchLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AwayDurationSeconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_TabSwitchLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_TabSwitchLogs_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_UserFolders",
                columns: table => new
                {
                    FolderId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ParentFolderId = table.Column<int>(type: "integer", nullable: true),
                    FolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_UserFolders", x => x.FolderId);
                    table.ForeignKey(
                        name: "FK_Hackathon_UserFolders_Hackathon_UserFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "Hackathon_UserFolders",
                        principalColumn: "FolderId");
                    table.ForeignKey(
                        name: "FK_Hackathon_UserFolders_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_UserFiles",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FolderId = table.Column<int>(type: "integer", nullable: true),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_UserFiles", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_Hackathon_UserFiles_Hackathon_UserFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Hackathon_UserFolders",
                        principalColumn: "FolderId");
                    table.ForeignKey(
                        name: "FK_Hackathon_UserFiles_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_AiBlockedSaves",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FileId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AttemptedContent = table.Column<string>(type: "text", nullable: true),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AdminRemarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BlockedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_AiBlockedSaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_AiBlockedSaves_Hackathon_UserFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "Hackathon_UserFiles",
                        principalColumn: "FileId");
                    table.ForeignKey(
                        name: "FK_Hackathon_AiBlockedSaves_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_AiDetectionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FileId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    DetectionResult = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: true),
                    ContentLength = table.Column<int>(type: "integer", nullable: false),
                    ContentDelta = table.Column<int>(type: "integer", nullable: false),
                    TabSwitchBeforeSave = table.Column<bool>(type: "boolean", nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "integer", nullable: true),
                    AnalyzedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_AiDetectionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_AiDetectionLogs_Hackathon_UserFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "Hackathon_UserFiles",
                        principalColumn: "FileId");
                    table.ForeignKey(
                        name: "FK_Hackathon_AiDetectionLogs_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Hackathon_AiDetectionSettings",
                columns: new[] { "Id", "BlockThreshold", "Mode", "ModifiedBy", "ModifiedDate" },
                values: new object[] { 1, 70, "AllowAndMark", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Hackathon_Users",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Email", "FullName", "IsActive", "LastLoginAt", "LoginCount", "ModifiedBy", "ModifiedDate", "MustChangePassword", "PasswordHash", "PasswordResetRequested", "Role", "UserID" },
                values: new object[] { 1, "SYSTEM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Super Admin", true, null, 0, null, null, false, "$2a$12$UokIogJtz0fLfUW7ubIzYOgNJZ8cZTo14YbiBB0ddKiL2m4Y2T/Vy", false, "SuperAdmin", "superadmin" });

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_AiBlockedSaves_FileId",
                table: "Hackathon_AiBlockedSaves",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_AiBlockedSaves_Status",
                table: "Hackathon_AiBlockedSaves",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_AiBlockedSaves_UserId",
                table: "Hackathon_AiBlockedSaves",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_AiDetectionLogs_AnalyzedDate",
                table: "Hackathon_AiDetectionLogs",
                column: "AnalyzedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_AiDetectionLogs_ConfidenceScore",
                table: "Hackathon_AiDetectionLogs",
                column: "ConfidenceScore");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_AiDetectionLogs_FileId",
                table: "Hackathon_AiDetectionLogs",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_AiDetectionLogs_UserId",
                table: "Hackathon_AiDetectionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_AiDetectionUserOverrides_UserId",
                table: "Hackathon_AiDetectionUserOverrides",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_ExecutionHistory_ExecutedAt",
                table: "Hackathon_ExecutionHistory",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_ExecutionHistory_UserId",
                table: "Hackathon_ExecutionHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_Sessions_UserId",
                table: "Hackathon_Sessions",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_SubmissionFiles_UserId",
                table: "Hackathon_SubmissionFiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_TabSwitchLogs_EventTime",
                table: "Hackathon_TabSwitchLogs",
                column: "EventTime");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_TabSwitchLogs_UserId",
                table: "Hackathon_TabSwitchLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_UserFiles_FolderId",
                table: "Hackathon_UserFiles",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_UserFiles_UserId_FolderId",
                table: "Hackathon_UserFiles",
                columns: new[] { "UserId", "FolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_UserFolders_ParentFolderId",
                table: "Hackathon_UserFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_UserFolders_UserId",
                table: "Hackathon_UserFolders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_Users_UserID",
                table: "Hackathon_Users",
                column: "UserID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hackathon_AiBlockedSaves");

            migrationBuilder.DropTable(
                name: "Hackathon_AiDetectionLogs");

            migrationBuilder.DropTable(
                name: "Hackathon_AiDetectionSettings");

            migrationBuilder.DropTable(
                name: "Hackathon_AiDetectionUserOverrides");

            migrationBuilder.DropTable(
                name: "Hackathon_Config");

            migrationBuilder.DropTable(
                name: "Hackathon_ExecutionHistory");

            migrationBuilder.DropTable(
                name: "Hackathon_QuestionPaper");

            migrationBuilder.DropTable(
                name: "Hackathon_Sessions");

            migrationBuilder.DropTable(
                name: "Hackathon_SubmissionFiles");

            migrationBuilder.DropTable(
                name: "Hackathon_TabSwitchLogs");

            migrationBuilder.DropTable(
                name: "Hackathon_UserFiles");

            migrationBuilder.DropTable(
                name: "Hackathon_UserFolders");

            migrationBuilder.DropTable(
                name: "Hackathon_Users");
        }
    }
}
