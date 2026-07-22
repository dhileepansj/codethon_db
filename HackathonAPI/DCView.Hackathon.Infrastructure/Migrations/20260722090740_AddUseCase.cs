using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCView.Hackathon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUseCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UseCaseHtml",
                table: "Hackathon_Assessments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseCaseHtml",
                table: "Hackathon_Assessments");
        }
    }
}
