using System;
using System.Collections.Generic;
using System.Linq;
using AutoTable.Models;

namespace AutoTable.Reports.ReportCards
{
    /// <summary>
    /// Drop-in replacement for QuestReportGenerator that uses the new modular
    /// QuestPDF components. Maintains the same public API signature for easy migration.
    /// </summary>
    public static class ReportCardPdfGenerator
    {
        /// <summary>
        /// Generates a single multi-page A4 PDF - one page per student - using the new
        /// modular QuestPDF components. Same API as QuestReportGenerator.GeneratePdf.
        /// </summary>
        public static byte[] GeneratePdf(
            IReadOnlyList<ReportCardRow> rows,
            IReadOnlyList<ReportCardSheetModel?>? sheetDataList)
        {
            var reports = new List<ReportCardData>();

            // Build ReportCardData for each student
            int total = Math.Max(
                sheetDataList?.Count ?? 0,
                rows?.Count ?? 0);

            for (int i = 0; i < total; i++)
            {
                var sheet = sheetDataList != null && i < sheetDataList.Count
                    ? sheetDataList[i]
                    : null;

                var row = rows != null && i < rows.Count
                    ? rows[i]
                    : null;

                // Use sheet model if available, otherwise create from row
                if (sheet != null)
                {
                    reports.Add(ReportCardAdapter.ToReportCardData(sheet));
                }
                else if (row != null)
                {
                    reports.Add(CreateFromRow(row));
                }
            }

            if (reports.Count == 0)
            {
                // Generate a blank placeholder page
                reports.Add(new ReportCardData(
                    new SchoolReportCardSettings(),
                    new StudentReportCardData(),
                    Array.Empty<SubjectResult>(),
                    CreateDefaultGradingScale()));
            }

            return ReportCardDocument.GenerateMultiPagePdf(reports);
        }

        /// <summary>
        /// Creates a minimal ReportCardData from a ReportCardRow when full sheet data is unavailable.
        /// </summary>
        private static ReportCardData CreateFromRow(ReportCardRow row)
        {
            var school = new SchoolReportCardSettings
            {
                Name = "School",
                AcademicYear = DateTime.Now.Year.ToString()
            };

            var student = new StudentReportCardData
            {
                Name = row.StudentName,
                AdmissionNumber = row.AdmissionNumber,
                ClassName = row.ClassName,
                OverallAverage = (decimal)row.Average,
                Status = row.Status,
                ClassPosition = row.Rank,
                ReportDate = DateOnly.FromDateTime(DateTime.Now)
            };

            return new ReportCardData(
                school,
                student,
                Array.Empty<SubjectResult>(),
                CreateDefaultGradingScale());
        }

        private static IReadOnlyList<GradeBand> CreateDefaultGradingScale()
        {
            return new List<GradeBand>
            {
                new() { Grade = "A",  RangeDisplay = "80 – 100", Remark = "Excellent" },
                new() { Grade = "B+", RangeDisplay = "75 – 79",  Remark = "Very Good" },
                new() { Grade = "B",  RangeDisplay = "70 – 74",  Remark = "Good" },
                new() { Grade = "B-", RangeDisplay = "65 – 69",  Remark = "Above Average" },
                new() { Grade = "C",  RangeDisplay = "60 – 64",  Remark = "Average" },
                new() { Grade = "D",  RangeDisplay = "50 – 59",  Remark = "Below Average" },
                new() { Grade = "E",  RangeDisplay = "40 – 49",  Remark = "Poor" },
                new() { Grade = "F",  RangeDisplay = "0 – 39",   Remark = "Fail" }
            };
        }
    }
}