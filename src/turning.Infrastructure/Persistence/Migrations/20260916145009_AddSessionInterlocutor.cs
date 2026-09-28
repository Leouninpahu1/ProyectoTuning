using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace turning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionInterlocutor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InterlocutorJoinedAtUtc",
                table: "ExperimentSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InterlocutorUserId",
                table: "ExperimentSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentSessions_Condition_InterlocutorUserId_Status",
                table: "ExperimentSessions",
                columns: new[] { "Condition", "InterlocutorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentSessions_InterlocutorUserId",
                table: "ExperimentSessions",
                column: "InterlocutorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExperimentSessions_UserAccounts_InterlocutorUserId",
                table: "ExperimentSessions",
                column: "InterlocutorUserId",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExperimentSessions_UserAccounts_InterlocutorUserId",
                table: "ExperimentSessions");

            migrationBuilder.DropIndex(
                name: "IX_ExperimentSessions_Condition_InterlocutorUserId_Status",
                table: "ExperimentSessions");

            migrationBuilder.DropIndex(
                name: "IX_ExperimentSessions_InterlocutorUserId",
                table: "ExperimentSessions");

            migrationBuilder.DropColumn(
                name: "InterlocutorJoinedAtUtc",
                table: "ExperimentSessions");

            migrationBuilder.DropColumn(
                name: "InterlocutorUserId",
                table: "ExperimentSessions");
        }
    }
}
