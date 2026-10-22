using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sessions.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcceptedSwaps",
                columns: table => new
                {
                    SwapId = table.Column<int>(type: "int", nullable: false),
                    InitiatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Accepted"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptedSwaps", x => x.SwapId);
                    table.CheckConstraint("CK_AcceptedSwaps_Parties", "[InitiatorId] <> [PartnerId]");
                    table.CheckConstraint("CK_AcceptedSwaps_Status", "[Status] IN (N'Accepted', N'Completed', N'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "SessionFormats",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionFormats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SwapId = table.Column<int>(type: "int", nullable: false),
                    FormatId = table.Column<byte>(type: "tinyint", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Planned"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.CheckConstraint("CK_Sessions_Duration", "[DurationMinutes] > 0");
                    table.CheckConstraint("CK_Sessions_Place", "([FormatId] = 1 AND [Link] IS NOT NULL) OR ([FormatId] = 2 AND [Location] IS NOT NULL)");
                    table.CheckConstraint("CK_Sessions_Status", "[Status] IN (N'Planned', N'Completed', N'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Sessions_AcceptedSwap",
                        column: x => x.SwapId,
                        principalTable: "AcceptedSwaps",
                        principalColumn: "SwapId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Format",
                        column: x => x.FormatId,
                        principalTable: "SessionFormats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionOutcomes",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActualDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionOutcomes", x => x.SessionId);
                    table.CheckConstraint("CK_SessionOutcomes_Duration", "[ActualDurationMinutes] > 0");
                    table.ForeignKey(
                        name: "FK_SessionOutcomes_Session",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SessionSkills",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    PlannedMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionSkills", x => new { x.SessionId, x.SkillId });
                    table.CheckConstraint("CK_SessionSkills_Minutes", "[PlannedMinutes] > 0");
                    table.ForeignKey(
                        name: "FK_SessionSkills_Session",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionSkills_Skill",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SessionFormats",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { (byte)1, "Online", "Онлайн" },
                    { (byte)2, "InPerson", "Особиста зустріч" }
                });

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Програмування C#", null },
                    { 2, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Англійська розмовна", null },
                    { 3, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Гра на гітарі", null },
                    { 4, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "UI-дизайн", null },
                    { 5, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Фотографія", null },
                    { 6, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Приготування випічки", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptedSwaps_Initiator",
                table: "AcceptedSwaps",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptedSwaps_Partner",
                table: "AcceptedSwaps",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "UQ_SessionFormats_Code",
                table: "SessionFormats",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_FormatId",
                table: "Sessions",
                column: "FormatId");

            migrationBuilder.CreateIndex(
                name: "UX_Sessions_Swap_Time",
                table: "Sessions",
                columns: new[] { "SwapId", "ScheduledAt" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SessionSkills_Skill",
                table: "SessionSkills",
                columns: new[] { "SkillId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "UX_Skills_Name",
                table: "Skills",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionOutcomes");

            migrationBuilder.DropTable(
                name: "SessionSkills");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "AcceptedSwaps");

            migrationBuilder.DropTable(
                name: "SessionFormats");
        }
    }
}
