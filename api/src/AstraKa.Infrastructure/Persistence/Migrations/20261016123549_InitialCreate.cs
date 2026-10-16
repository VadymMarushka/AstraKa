using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AstraKa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZodiacSigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZodiacSigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Starshepherds",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Starshepherds", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Starshepherds_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wanderers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZodiacSignId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wanderers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Wanderers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Wanderers_ZodiacSigns_ZodiacSignId",
                        column: x => x.ZodiacSignId,
                        principalTable: "ZodiacSigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Promise = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Starshepherds_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Starshepherds",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercises_Starshepherds_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Starshepherds",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cohorts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SeatLimit = table.Column<int>(type: "int", nullable: false),
                    SeatsTaken = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cohorts", x => x.Id);
                    table.CheckConstraint("CK_Cohort_SeatLimit", "[SeatLimit] > 0");
                    table.ForeignKey(
                        name: "FK_Cohorts_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MeetLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseDays_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseSigns",
                columns: table => new
                {
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZodiacSignId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSigns", x => new { x.CourseId, x.ZodiacSignId });
                    table.ForeignKey(
                        name: "FK_CourseSigns_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSigns_ZodiacSigns_ZodiacSignId",
                        column: x => x.ZodiacSignId,
                        principalTable: "ZodiacSigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    WandererId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CohortId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaidPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => new { x.WandererId, x.CohortId });
                    table.ForeignKey(
                        name: "FK_Enrollments_Cohorts_CohortId",
                        column: x => x.CohortId,
                        principalTable: "Cohorts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Wanderers_WandererId",
                        column: x => x.WandererId,
                        principalTable: "Wanderers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WandererId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CohortId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    HarmonyLevel = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReports", x => x.Id);
                    table.CheckConstraint("CK_DailyReport_HarmonyLevel", "[HarmonyLevel] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_DailyReports_Enrollments_WandererId_CohortId",
                        columns: x => new { x.WandererId, x.CohortId },
                        principalTable: "Enrollments",
                        principalColumns: new[] { "WandererId", "CohortId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Revelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WandererId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CohortId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Revelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Revelations_CourseDays_CourseDayId",
                        column: x => x.CourseDayId,
                        principalTable: "CourseDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Revelations_Enrollments_WandererId_CohortId",
                        columns: x => new { x.WandererId, x.CohortId },
                        principalTable: "Enrollments",
                        principalColumns: new[] { "WandererId", "CohortId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DayExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseDayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderNumber = table.Column<int>(type: "int", nullable: false),
                    Sets = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayExercises", x => x.Id);
                    table.CheckConstraint("CK_DayExercise_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_DayExercise_Sets", "[Sets] > 0");
                    table.ForeignKey(
                        name: "FK_DayExercises_CourseDays_CourseDayId",
                        column: x => x.CourseDayId,
                        principalTable: "CourseDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DayExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DayExercises_Revelations_RevelationId",
                        column: x => x.RevelationId,
                        principalTable: "Revelations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseCompletions_DailyReports_DailyReportId",
                        column: x => x.DailyReportId,
                        principalTable: "DailyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseCompletions_DayExercises_DayExerciseId",
                        column: x => x.DayExerciseId,
                        principalTable: "DayExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cohorts_CourseId",
                table: "Cohorts",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseDays_CourseId_DayNumber",
                table: "CourseDays",
                columns: new[] { "CourseId", "DayNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_AuthorId",
                table: "Courses",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSigns_ZodiacSignId",
                table: "CourseSigns",
                column: "ZodiacSignId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_WandererId_CohortId_Date",
                table: "DailyReports",
                columns: new[] { "WandererId", "CohortId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DayExercises_CourseDayId_OrderNumber",
                table: "DayExercises",
                columns: new[] { "CourseDayId", "OrderNumber" },
                unique: true,
                filter: "[RevelationId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DayExercises_ExerciseId",
                table: "DayExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_DayExercises_RevelationId",
                table: "DayExercises",
                column: "RevelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CohortId",
                table: "Enrollments",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseCompletions_DailyReportId_DayExerciseId",
                table: "ExerciseCompletions",
                columns: new[] { "DailyReportId", "DayExerciseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseCompletions_DayExerciseId",
                table: "ExerciseCompletions",
                column: "DayExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_AuthorId",
                table: "Exercises",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Revelations_CourseDayId",
                table: "Revelations",
                column: "CourseDayId");

            migrationBuilder.CreateIndex(
                name: "IX_Revelations_WandererId_CohortId",
                table: "Revelations",
                columns: new[] { "WandererId", "CohortId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalId",
                table: "Users",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wanderers_ZodiacSignId",
                table: "Wanderers",
                column: "ZodiacSignId");

            migrationBuilder.CreateIndex(
                name: "IX_ZodiacSigns_Name",
                table: "ZodiacSigns",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZodiacSigns_SequenceNumber",
                table: "ZodiacSigns",
                column: "SequenceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseSigns");

            migrationBuilder.DropTable(
                name: "ExerciseCompletions");

            migrationBuilder.DropTable(
                name: "DailyReports");

            migrationBuilder.DropTable(
                name: "DayExercises");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "Revelations");

            migrationBuilder.DropTable(
                name: "CourseDays");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Cohorts");

            migrationBuilder.DropTable(
                name: "Wanderers");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "ZodiacSigns");

            migrationBuilder.DropTable(
                name: "Starshepherds");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
