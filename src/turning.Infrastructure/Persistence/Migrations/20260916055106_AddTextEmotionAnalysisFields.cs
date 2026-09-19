using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTextEmotionAnalysisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnalysisLatencyMs",
                table: "EmotionReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "EmotionReadings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "EmotionReadings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Polarity",
                table: "EmotionReadings",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoresJson",
                table: "EmotionReadings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisLatencyMs",
                table: "EmotionReadings");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "EmotionReadings");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "EmotionReadings");

            migrationBuilder.DropColumn(
                name: "Polarity",
                table: "EmotionReadings");

            migrationBuilder.DropColumn(
                name: "ScoresJson",
                table: "EmotionReadings");
        }
    }
}
