using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTable.Data.Migrations
{
    public partial class AddAssessmentStreamAndTermFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Terms: add StartDate, EndDate
            migrationBuilder.AddColumn<string>(
                name: "StartDate",
                table: "Terms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndDate",
                table: "Terms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsActive",
                table: "Terms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Assessments: add StreamId and IsClassWide
            migrationBuilder.AddColumn<int>(
                name: "StreamId",
                table: "Assessments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsClassWide",
                table: "Assessments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // FeePayments: add TermId
            migrationBuilder.AddColumn<int>(
                name: "TermId",
                table: "FeePayments",
                type: "INTEGER",
                nullable: true);

            // Note: foreign keys not added here to keep migration simple; EF model has relationship configuration.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "StartDate", table: "Terms");
            migrationBuilder.DropColumn(name: "EndDate", table: "Terms");
            migrationBuilder.DropColumn(name: "StreamId", table: "Assessments");
            migrationBuilder.DropColumn(name: "IsClassWide", table: "Assessments");
            migrationBuilder.DropColumn(name: "TermId", table: "FeePayments");
        }
    }
}
