using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteWatchAIBackend.Migrations
{
    /// <inheritdoc />
    public partial class ChangedPrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeatureLocationType",
                table: "PredictionResults");

            migrationBuilder.DropColumn(
                name: "FeatureWeather",
                table: "PredictionResults");

            migrationBuilder.RenameColumn(
                name: "Prediction",
                table: "PredictionResults",
                newName: "Weather");

            migrationBuilder.RenameColumn(
                name: "FeatureTemp",
                table: "PredictionResults",
                newName: "Temp");

            migrationBuilder.AddColumn<float>(
                name: "AvgConfidence",
                table: "PredictionResults",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.CreateTable(
                name: "CategoryPredictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PredictedValue = table.Column<int>(type: "int", nullable: false),
                    ConfidenceScore = table.Column<float>(type: "real", nullable: false),
                    ModelUsed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PredictionResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryPredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryPredictions_PredictionResults_PredictionResultId",
                        column: x => x.PredictionResultId,
                        principalTable: "PredictionResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryPredictions_PredictionResultId",
                table: "CategoryPredictions",
                column: "PredictionResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryPredictions");

            migrationBuilder.DropColumn(
                name: "AvgConfidence",
                table: "PredictionResults");

            migrationBuilder.RenameColumn(
                name: "Weather",
                table: "PredictionResults",
                newName: "Prediction");

            migrationBuilder.RenameColumn(
                name: "Temp",
                table: "PredictionResults",
                newName: "FeatureTemp");

            migrationBuilder.AddColumn<string>(
                name: "FeatureLocationType",
                table: "PredictionResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FeatureWeather",
                table: "PredictionResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
