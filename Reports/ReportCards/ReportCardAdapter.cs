using System;
using System.Collections.Generic;
using System.Linq;
using AutoTable.Models;

namespace AutoTable.Reports.ReportCards
{
    /// <summary>
    /// Bridges the existing ReportCardSheetModel / ReportCardRow models to the new
    /// ReportCardData contracts used by the modular QuestPDF generator.
    /// </summary>
    public static class ReportCardAdapter
    {
        /// <summary>
        /// Converts an existing ReportCardSheetModel (from the XAML preview) into the
        /// new ReportCardData contract for PDF generation.
        /// </summary>
        public static ReportCardData ToReportCardData(
            ReportCardSheetModel sheet,
            SchoolReportCardSettings? schoolOverride = null,
            IReadOnlyList<GradeBand>? gradingScale = null)
        {
            var school = schoolOverride ?? CreateDefaultSchoolSettings(sheet);
            var student = CreateStudentData(sheet);
            var subjects = CreateSubjectResults(sheet);
            var grades = gradingScale ?? CreateDefaultGradingScale();

            return new ReportCardData(school, student, subjects, grades);
        }

        /// <summary>
        /// Converts a list of sheet models into multiple ReportCardData objects.
        /// </summary>
        public static IReadOnlyList<ReportCardData> ToReportCardDataList(
            IReadOnlyList<ReportCardSheetModel> sheets,
            SchoolReportCardSettings? schoolOverride = null,
            IReadOnlyList<GradeBand>? gradingScale = null)
        {
            return sheets.Select(s => ToReportCardData(s, schoolOverride, gradingScale)).ToList();
        }

        private static SchoolReportCardSettings CreateDefaultSchoolSettings(ReportCardSheetModel sheet)
        {
            return new SchoolReportCardSettings
            {
                Name = sheet.SchoolName,
                Subtitle = "",
                Motto = "",
                AcademicYear = sheet.AcademicYear,
                PrimaryColor = "#00184D",
                SecondaryColor = "#0A2860",
                AccentColor = "#E2A01B",
                TableBgColor = "#ECF3FE",
                GridColor = "#9FB4D2",
                BodyFont = "Lato",
                DisplayFont = "Georgia",
                PostalAddress = sheet.SchoolAddress,
                PrincipalName = sheet.HeadTeacher,
                Logo = sheet.LogoBytes
            };
        }

        private static StudentReportCardData CreateStudentData(ReportCardSheetModel sheet)
        {
            return new StudentReportCardData
            {
                Name = sheet.StudentName,
                AdmissionNumber = sheet.AdmissionNumber,
                Gender = sheet.Gender,
                ClassName = sheet.ClassName,
                Term = sheet.Term,
                AcademicYear = sheet.AcademicYear,
                House = sheet.Stream,
                ReportDate = DateOnly.FromDateTime(DateTime.Now),

                TeacherComments = sheet.TeacherComment,
                ClassTeacherName = sheet.ClassTeacher,
                PrincipalRemark = sheet.HeadTeacherComment,

                TotalMarks = 0,  // To be calculated from subjects
                MaximumMarks = 0,
                OverallAverage = (decimal)sheet.OverallAverage,
                OverallGrade = sheet.OverallGrade,
                ClassPosition = sheet.Rank,
                ClassSize = 0,
                Status = sheet.Status,

                Photo = sheet.StudentPhotoBytes
            };
        }

        private static IReadOnlyList<SubjectResult> CreateSubjectResults(ReportCardSheetModel sheet)
        {
            var results = new List<SubjectResult>();

            // From gradebook summary (primary source)
            foreach (var gb in sheet.GradebookSummary)
            {
                results.Add(new SubjectResult
                {
                    SubjectName = gb.Subject,
                    MaximumMarks = 100,
                    Cat1 = (decimal)gb.Cat1,
                    Cat2 = (decimal)gb.Cat2,
                    Exam = (decimal)gb.EndTerm,
                    Total = (decimal)(gb.Cat1 + gb.Cat2 + gb.EndTerm),
                    Average = (decimal)gb.Average,
                    Grade = gb.Grade
                });
            }

            // Fallback: from promotional assessments if gradebook is empty
            if (results.Count == 0)
            {
                foreach (var a in sheet.PromotionalAssessments)
                {
                    results.Add(new SubjectResult
                    {
                        SubjectName = a.Subject,
                        MaximumMarks = 100,
                        Total = (decimal)a.Mark,
                        Average = (decimal)a.Mark,
                        Grade = a.Grade
                    });
                }
            }

            return results;
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

        /// <summary>
        /// Converts a ReportCardData (from fixture or PDF generator) back into a
        /// ReportCardSheetModel so the XAML preview can render it.
        /// </summary>
        public static ReportCardSheetModel ToSheetModel(ReportCardData data)
        {
            var sheet = new ReportCardSheetModel
            {
                SchoolName = data.School.Name,
                SchoolAddress = data.School.PostalAddress,
                StudentName = data.Student.Name,
                AdmissionNumber = data.Student.AdmissionNumber,
                ClassName = data.Student.ClassName,
                Stream = data.Student.House ?? string.Empty,
                Term = data.Student.Term,
                AcademicYear = data.Student.AcademicYear,
                Gender = data.Student.Gender ?? string.Empty,
                DateOfBirth = data.Student.DateOfBirth?.ToString("dd MMM yyyy") ?? string.Empty,
                ClassTeacher = data.Student.ClassTeacherName,

                OverallAverage = (double)data.Student.OverallAverage,
                OverallGrade = data.Student.OverallGrade,
                Rank = data.Student.ClassPosition,
                Status = data.Student.Status,

                TeacherComment = data.Student.TeacherComments,
                HeadTeacher = data.School.PrincipalName,
                HeadTeacherComment = data.Student.PrincipalRemark,

                LogoBytes = data.School.Logo,
                StudentPhotoBytes = data.Student.Photo
            };

            // Convert subjects to gradebook summary
            foreach (var subj in data.Subjects)
            {
                sheet.GradebookSummary.Add(new GradebookSummaryRow
                {
                    Subject = subj.SubjectName,
                    Cat1 = (double)subj.Cat1,
                    Cat2 = (double)subj.Cat2,
                    MidTerm = 0,
                    EndTerm = (double)subj.Exam,
                    Average = (double)subj.Average,
                    Grade = subj.Grade,
                    Status = GetStatusFromGrade(subj.Grade)
                });
            }

            return sheet;
        }

        private static string GetStatusFromGrade(string grade)
        {
            return grade switch
            {
                "A" => "Excellent",
                "B+" => "Very Good",
                "B" => "Good",
                "B-" => "Above Average",
                "C" => "Average",
                "D" => "Below Average",
                "E" => "Poor",
                "F" => "Fail",
                _ => "-"
            };
        }
    }
}