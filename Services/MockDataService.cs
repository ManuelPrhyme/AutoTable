using AutoTable.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoTable.Services
{
    public class MockDataService
    {
        private static MockDataService? _instance;
        public static MockDataService Instance => _instance ??= new MockDataService();

        public IReadOnlyList<string> Classes { get; } = new[] { "P1", "P2", "P3", "P4", "P5", "P6", "P7" };
        public IReadOnlyList<string> Subjects { get; } = new[] { "Mathematics", "English", "Science", "Social Studies", "Religious Education" };
        public IReadOnlyList<string> Terms { get; } = new[] { "Term 2, 2025", "Term 1, 2025" };

        public List<AssessmentItem> GetAssessments()
        {
            return new List<AssessmentItem>
            {
                new() { Name = "Mid Term I", ClassName = "P5", Subject = "Mathematics", WeightPercent = 30, DueDate = new DateTime(2025, 5, 22), MarksEnteredPercent = 100, IsVerified = true, IsPublished = true },
                new() { Name = "CAT 2", ClassName = "P4", Subject = "Science", WeightPercent = 20, DueDate = new DateTime(2025, 5, 26), MarksEnteredPercent = 78, IsVerified = false, IsPublished = false },
                new() { Name = "End Term", ClassName = "P6", Subject = "English", WeightPercent = 40, DueDate = new DateTime(2025, 6, 10), MarksEnteredPercent = 45, IsVerified = false, IsPublished = false },
                new() { Name = "CAT 1", ClassName = "P3", Subject = "Mathematics", WeightPercent = 10, DueDate = new DateTime(2025, 4, 15), MarksEnteredPercent = 100, IsVerified = true, IsPublished = false },
                new() { Name = "Mid Term I", ClassName = "P7", Subject = "Social Studies", WeightPercent = 30, DueDate = new DateTime(2025, 5, 20), MarksEnteredPercent = 92, IsVerified = true, IsPublished = false },
                new() { Name = "CAT 2", ClassName = "P2", Subject = "English", WeightPercent = 20, DueDate = new DateTime(2025, 5, 28), MarksEnteredPercent = 34, IsVerified = false, IsPublished = false },
            };
        }

        public List<StudentMarkRow> GetStudentMarks(string className, string subject, string assessmentName)
        {
            var students = new[]
            {
                ("Amina Nakato", "BF-1042"),
                ("Brian Okello", "BF-1043"),
                ("Carol Namukasa", "BF-1044"),
                ("David Ssempijja", "BF-1045"),
                ("Esther Akello", "BF-1046"),
                ("Francis Muwonge", "BF-1047"),
                ("Grace Nabwire", "BF-1048"),
                ("Henry Tumusiime", "BF-1049"),
            };

            var random = new Random(className.GetHashCode() ^ subject.GetHashCode());
            return students.Select((s, i) => new StudentMarkRow
            {
                StudentId = $"STU-{i + 1}",
                StudentName = s.Item1,
                AdmissionNumber = s.Item2,
                ClassName = className,
                Mark = random.Next(0, 10) > 2 ? random.Next(35, 98) : (double?)null,
                Grade = random.Next(0, 10) > 2 ? GradeFromMark(random.Next(35, 98)) : "-",
                Remarks = string.Empty,
                IsEditable = true
            }).ToList();
        }

        public List<GradebookRow> GetGradebook(string className, string subject)
        {
            var marks = GetStudentMarks(className, subject, "Combined");
            var rows = marks.Select((m, i) =>
            {
                var cat1 = m.Mark ?? 0;
                var cat2 = Math.Min(100, cat1 + 4);
                var mid = Math.Min(100, cat1 + 2);
                var end = Math.Min(100, cat1 - 3);
                var avg = Math.Round((cat1 + cat2 + mid + end) / 4.0, 1);
                return new GradebookRow
                {
                    StudentName = m.StudentName,
                    AdmissionNumber = m.AdmissionNumber,
                    ClassName = className,
                    Cat1 = cat1,
                    Cat2 = cat2,
                    MidTerm = mid,
                    EndTerm = end,
                    Average = avg,
                    Rank = 0,
                    Grade = GradeFromMark(avg),
                    Status = avg < 40 ? "At Risk" : avg >= 70 ? "Excellent" : "On Track"
                };
            }).OrderByDescending(r => r.Average).ToList();

            for (var i = 0; i < rows.Count; i++)
                rows[i].Rank = i + 1;

            return rows;
        }

        private static string GradeFromMark(double mark) => mark switch
        {
            >= 80 => "A",
            >= 70 => "B",
            >= 60 => "C",
            >= 50 => "D",
            >= 40 => "E",
            _ => "F"
        };
    }
}
