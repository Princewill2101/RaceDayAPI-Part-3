using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDayAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Users ─────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId           = table.Column<int>(nullable: false)
                                           .Annotation("SqlServer:Identity", "1, 1"),
                    FullName         = table.Column<string>(maxLength: 100, nullable: false),
                    Email            = table.Column<string>(maxLength: 150, nullable: false),
                    PasswordHash     = table.Column<string>(maxLength: 255, nullable: false),
                    Role             = table.Column<string>(maxLength: 20, nullable: false),
                    ProfilePictureUrl = table.Column<string>(nullable: true),
                    CreatedAt        = table.Column<DateTime>(nullable: false,
                                           defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Role",
                        "[Role] IN ('Organiser', 'Participant')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            // ── EventTypes ────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "EventTypes",
                columns: table => new
                {
                    EventTypeId = table.Column<int>(nullable: false)
                                      .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName    = table.Column<string>(maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.EventTypeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventTypes_TypeName",
                table: "EventTypes",
                column: "TypeName",
                unique: true);

            // Seed the three event types
            migrationBuilder.InsertData(
                table: "EventTypes",
                columns: new[] { "EventTypeId", "TypeName" },
                values: new object[,]
                {
                    { 1, "Run" },
                    { 2, "Walk" },
                    { 3, "Cycle" }
                });

            // ── Events ────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId        = table.Column<int>(nullable: false)
                                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserId    = table.Column<int>(nullable: false),
                    EventTypeId    = table.Column<int>(nullable: false),
                    Name           = table.Column<string>(maxLength: 150, nullable: false),
                    Description    = table.Column<string>(nullable: true),
                    EventDate      = table.Column<DateTime>(nullable: false),
                    Location       = table.Column<string>(maxLength: 150, nullable: false),
                    DistanceKm     = table.Column<decimal>(precision: 5, scale: 2, nullable: false),
                    BannerImageUrl = table.Column<string>(nullable: true),
                    CreatedAt      = table.Column<DateTime>(nullable: false,
                                        defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.ForeignKey("FK_Events_Users_OrganiserId",
                        x => x.OrganiserId, "Users", "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Events_EventTypes_EventTypeId",
                        x => x.EventTypeId, "EventTypes", "EventTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            // ── Categories ────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId   = table.Column<int>(nullable: false)
                                       .Annotation("SqlServer:Identity", "1, 1"),
                    EventId      = table.Column<int>(nullable: false),
                    CategoryName = table.Column<string>(maxLength: 50, nullable: false),
                    MinAge       = table.Column<int>(nullable: true),
                    MaxAge       = table.Column<int>(nullable: true),
                    DistanceKm   = table.Column<decimal>(precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                    table.ForeignKey("FK_Categories_Events_EventId",
                        x => x.EventId, "Events", "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Enrolments ────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    EnrolmentId   = table.Column<int>(nullable: false)
                                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantId = table.Column<int>(nullable: false),
                    EventId       = table.Column<int>(nullable: false),
                    CategoryId    = table.Column<int>(nullable: false),
                    EnrolmentDate = table.Column<DateTime>(nullable: false,
                                        defaultValueSql: "GETDATE()"),
                    Status        = table.Column<string>(maxLength: 20, nullable: false,
                                        defaultValue: "Confirmed")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentId);
                    table.CheckConstraint("CK_Enrolments_Status",
                        "[Status] IN ('Confirmed', 'Cancelled')");
                    table.ForeignKey("FK_Enrolments_Users_ParticipantId",
                        x => x.ParticipantId, "Users", "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Enrolments_Events_EventId",
                        x => x.EventId, "Events", "EventId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Enrolments_Categories_CategoryId",
                        x => x.CategoryId, "Categories", "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_ParticipantId_EventId",
                table: "Enrolments",
                columns: new[] { "ParticipantId", "EventId" },
                unique: true);

            // ── Results ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultId       = table.Column<int>(nullable: false)
                                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentId    = table.Column<int>(nullable: false),
                    FinishTime     = table.Column<TimeSpan>(nullable: true),
                    FinishPosition = table.Column<int>(nullable: true),
                    RecordedAt     = table.Column<DateTime>(nullable: false,
                                        defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultId);
                    table.ForeignKey("FK_Results_Enrolments_EnrolmentId",
                        x => x.EnrolmentId, "Enrolments", "EnrolmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Results_EnrolmentId",
                table: "Results",
                column: "EnrolmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("Results");
            migrationBuilder.DropTable("Enrolments");
            migrationBuilder.DropTable("Categories");
            migrationBuilder.DropTable("Events");
            migrationBuilder.DropTable("EventTypes");
            migrationBuilder.DropTable("Users");
        }
    }
}
