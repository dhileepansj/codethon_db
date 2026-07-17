using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAndPasswordLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hackathon_PasswordChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_PasswordChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hackathon_PasswordChangeLogs_Hackathon_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Hackathon_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hackathon_SecuritySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MinLength = table.Column<int>(type: "integer", nullable: false),
                    MaxLength = table.Column<int>(type: "integer", nullable: false),
                    RequireUppercase = table.Column<bool>(type: "boolean", nullable: false),
                    RequireLowercase = table.Column<bool>(type: "boolean", nullable: false),
                    RequireDigit = table.Column<bool>(type: "boolean", nullable: false),
                    RequireSpecialChar = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHistoryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxFailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockoutDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    PasswordExpiryDays = table.Column<int>(type: "integer", nullable: false),
                    MaxConcurrentSessions = table.Column<int>(type: "integer", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_SecuritySettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Hackathon_SecuritySettings",
                columns: new[] { "Id", "LockoutDurationMinutes", "MaxConcurrentSessions", "MaxFailedLoginAttempts", "MaxLength", "MinLength", "ModifiedBy", "ModifiedDate", "PasswordExpiryDays", "PasswordHistoryCount", "RequireDigit", "RequireLowercase", "RequireSpecialChar", "RequireUppercase" },
                values: new object[] { 1, 15, 1, 5, 64, 8, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, 5, true, true, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_PasswordChangeLogs_ChangedAt",
                table: "Hackathon_PasswordChangeLogs",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_PasswordChangeLogs_UserId",
                table: "Hackathon_PasswordChangeLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hackathon_PasswordChangeLogs");

            migrationBuilder.DropTable(
                name: "Hackathon_SecuritySettings");
        }
    }
}
