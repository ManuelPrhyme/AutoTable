using System;
using System.Collections.Generic;

namespace AutoTable.Reports.ReportCards
{
    /// <summary>
    /// Top-level data contract passed to the report card renderer.
    /// Contains all school, student, and assessment data needed to render one page.
    /// </summary>
    public sealed record ReportCardData(
        SchoolReportCardSettings School,
        StudentReportCardData Student,
        IReadOnlyList<SubjectResult> Subjects,
        IReadOnlyList<GradeBand> GradingScale);

    /// <summary>
    /// School-level configuration for the report card.
    /// </summary>
    public sealed record SchoolReportCardSettings
    {
        public string Name { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;       // e.g. "SECONDARY SCHOOL"
        public string Motto { get; init; } = string.Empty;          // e.g. "Knowledge. Discipline. Excellence."
        public string AcademicYear { get; init; } = string.Empty;   // e.g. "2025/2026"

        // Branding
        public string PrimaryColor { get; init; } = "#00184D";
        public string SecondaryColor { get; init; } = "#0A2860";
        public string AccentColor { get; init; } = "#E2A01B";
        public string TableBgColor { get; init; } = "#ECF3FE";
        public string GridColor { get; init; } = "#9FB4D2";

        // Fonts
        public string BodyFont { get; init; } = "Lato";
        public string DisplayFont { get; init; } = "Georgia";

        // Contact
        public string PostalAddress { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Website { get; init; } = string.Empty;

        // Leadership
        public string PrincipalName { get; init; } = string.Empty;
        public byte[]? PrincipalSignature { get; init; }

        // Assets
        public byte[]? Logo { get; init; }
        public string? LogoSvg { get; init; }  // Preferred for vector logos
    }

    /// <summary>
    /// Student-level data for one report card page.
    /// </summary>
    public sealed record StudentReportCardData
    {
        public string Name { get; init; } = string.Empty;
        public string AdmissionNumber { get; init; } = string.Empty;
        public DateOnly? DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public string ClassName { get; init; } = string.Empty;      // e.g. "S.3 BLUE"
        public string Term { get; init; } = string.Empty;           // e.g. "SECOND TERM"
        public string AcademicYear { get; init; } = string.Empty;
        public string? House { get; init; }
        public DateOnly ReportDate { get; init; }
        public byte[]? Photo { get; init; }

        // Comments
        public string TeacherComments { get; init; } = string.Empty;
        public string PrincipalRemark { get; init; } = string.Empty;
        public string ClassTeacherName { get; init; } = string.Empty;
        public byte[]? ClassTeacherSignature { get; init; }

        // Summary
        public int TotalMarks { get; init; }
        public int MaximumMarks { get; init; }
        public decimal OverallAverage { get; init; }
        public string OverallGrade { get; init; } = string.Empty;
        public int ClassPosition { get; init; }
        public int ClassSize { get; init; }
        public string Status { get; init; } = string.Empty;        // e.g. "PROMOTED"

        // Computed display helpers
        public string TotalMarksDisplay => MaximumMarks > 0
            ? $"{TotalMarks:N0} / {MaximumMarks:N0}"
            : TotalMarks.ToString("N0");

        public string PositionDisplay => ClassSize > 0
            ? $"{ClassPosition} / {ClassSize}"
            : ClassPosition.ToString();
    }

    /// <summary>
    /// One subject row in the examination results table.
    /// </summary>
    public sealed record SubjectResult
    {
        public string SubjectName { get; init; } = string.Empty;
        public int MaximumMarks { get; init; } = 100;
        public decimal Cat1 { get; init; }
        public decimal Cat2 { get; init; }
        public decimal Exam { get; init; }
        public decimal Total { get; init; }
        public decimal Average { get; init; }
        public string Grade { get; init; } = string.Empty;
    }

    /// <summary>
    /// One row in the grading key table.
    /// </summary>
    public sealed record GradeBand
    {
        public string Grade { get; init; } = string.Empty;
        public string RangeDisplay { get; init; } = string.Empty;  // e.g. "80-100"
        public string Remark { get; init; } = string.Empty;        // e.g. "Excellent"
    }
}