using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcPilot.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedProcesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedProcesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedProcesses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedProcesses_ProcessName",
                table: "BlockedProcesses",
                column: "ProcessName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedProcesses");
        }
    }
}
