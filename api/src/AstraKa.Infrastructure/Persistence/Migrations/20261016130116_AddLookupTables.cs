using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AstraKa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Users",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "Exercises",
                newName: "UnitId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Enrollments",
                newName: "StatusId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Courses",
                newName: "StatusId");

            migrationBuilder.AlterColumn<byte>(
                name: "RoleId",
                table: "Users",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<byte>(
                name: "UnitId",
                table: "Exercises",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<byte>(
                name: "StatusId",
                table: "Enrollments",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<byte>(
                name: "StatusId",
                table: "Courses",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "CourseStatuses",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentStatuses",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseUnits",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CourseStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { (byte)1, "Draft" },
                    { (byte)2, "Published" }
                });

            migrationBuilder.InsertData(
                table: "EnrollmentStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { (byte)1, "Active" },
                    { (byte)2, "Left" }
                });

            migrationBuilder.InsertData(
                table: "ExerciseUnits",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { (byte)1, "Reps" },
                    { (byte)2, "Seconds" },
                    { (byte)3, "Breaths" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { (byte)1, "Wanderer" },
                    { (byte)2, "Starshepherd" },
                    { (byte)3, "Oracle" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_UnitId",
                table: "Exercises",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StatusId",
                table: "Enrollments",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_StatusId",
                table: "Courses",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseStatuses_Name",
                table: "CourseStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentStatuses_Name",
                table: "EnrollmentStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseUnits_Name",
                table: "ExerciseUnits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_Name",
                table: "UserRoles",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_CourseStatuses_StatusId",
                table: "Courses",
                column: "StatusId",
                principalTable: "CourseStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_EnrollmentStatuses_StatusId",
                table: "Enrollments",
                column: "StatusId",
                principalTable: "EnrollmentStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_ExerciseUnits_UnitId",
                table: "Exercises",
                column: "UnitId",
                principalTable: "ExerciseUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_UserRoles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "UserRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_CourseStatuses_StatusId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_EnrollmentStatuses_StatusId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_ExerciseUnits_UnitId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_UserRoles_RoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "CourseStatuses");

            migrationBuilder.DropTable(
                name: "EnrollmentStatuses");

            migrationBuilder.DropTable(
                name: "ExerciseUnits");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_UnitId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StatusId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_StatusId",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "Users",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "UnitId",
                table: "Exercises",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "StatusId",
                table: "Enrollments",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "StatusId",
                table: "Courses",
                newName: "Status");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Exercises",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Enrollments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Courses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");
        }
    }
}
