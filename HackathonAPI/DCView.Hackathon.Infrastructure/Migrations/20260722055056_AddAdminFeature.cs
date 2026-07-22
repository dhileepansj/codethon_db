using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hackathon_AdminUsers",
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
                    CanManageUsers = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageSessions = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewMonitoring = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageAssessments = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewResults = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageHackathonSetup = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageServerConfig = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageScaffoldScripts = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageSecuritySettings = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageAiDetection = table.Column<bool>(type: "boolean", nullable: false),
                    CanExportData = table.Column<bool>(type: "boolean", nullable: false),
                    CanResetDatabase = table.Column<bool>(type: "boolean", nullable: false),
                    CanDeleteUsers = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hackathon_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hackathon_AdminUsers_UserID",
                table: "Hackathon_AdminUsers",
                column: "UserID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hackathon_AdminUsers");
        }
    }
}
