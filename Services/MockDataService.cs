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
        public IReadOnlyList<string> AcademicYears { get; } = new[] { "2024-2025", "2025-2026" };
        public IReadOnlyList<string> Streams { get; } = new[] { "Stream A", "Stream B", "Stream C" };

        public IReadOnlyList<string> AllStudents { get; } = new[]
        {
            "Amina Nakato", "Brian Okello", "Carol Namukasa", "David Ssempijja",
            "Esther Akello", "Francis Muwonge", "Grace Nabwire", "Henry Tumusiime",
            "Irene Mbabazi", "Jacob Ssebunya", "Karen Wamalwa", "Lawrence Ochieng",
        };

        private static readonly Dictionary<string, string> StudentAdmissionNumbers = new()
        {
            ["Amina Nakato"] = "BF-1042",
            ["Brian Okello"] = "BF-1043",
            ["Carol Namukasa"] = "BF-1044",
            ["David Ssempijja"] = "BF-1045",
            ["Esther Akello"] = "BF-1046",
            ["Francis Muwonge"] = "BF-1047",
            ["Grace Nabwire"] = "BF-1048",
            ["Henry Tumusiime"] = "BF-1049",
            ["Irene Mbabazi"] = "BF-1050",
            ["Jacob Ssebunya"] = "BF-1051",
            ["Karen Wamalwa"] = "BF-1052",
            ["Lawrence Ochieng"] = "BF-1053",
        };

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

        public List<GradebookRow> GetGradebook(string? className = null, string? subject = null,
            string? academicYear = null, string? term = null, string? stream = null, string? studentName = null)
        {
            var cls = className ?? "P5";
            var subj = subject ?? "Mathematics";
            var marks = GetStudentMarks(cls, subj, "Combined");
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
                    ClassName = cls,
                    Stream = stream ?? "Stream A",
                    Subject = subj,
                    AcademicYear = academicYear ?? "2024-2025",
                    Term = term ?? "Term 2, 2025",
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

            // Filter by student name if provided
            if (!string.IsNullOrWhiteSpace(studentName))
            {
                rows = rows.Where(r => r.StudentName.Contains(studentName, StringComparison.OrdinalIgnoreCase)).ToList();
                for (var i = 0; i < rows.Count; i++)
                    rows[i].Rank = i + 1;
            }

            return rows;
        }

        public StudentPerformanceDetail GetStudentPerformanceDetail(string studentName, string className,
            string subject, string academicYear, string term, string stream)
        {
            var admissionNumber = StudentAdmissionNumbers.TryGetValue(studentName, out var adm) ? adm : "BF-0000";
            var random = new Random(studentName.GetHashCode() ^ className.GetHashCode());

            var subjectPerformances = new System.Collections.ObjectModel.ObservableCollection<StudentSubjectPerformance>();
            var allSubjects = Subjects;

            foreach (var subj in allSubjects)
            {
                var cat1 = random.Next(35, 98);
                var cat2 = Math.Min(100, cat1 + 4);
                var mid = Math.Min(100, cat1 + 2);
                var end = Math.Min(100, cat1 - 3);
                var avg = Math.Round((cat1 + cat2 + mid + end) / 4.0, 1);
                subjectPerformances.Add(new StudentSubjectPerformance
                {
                    Subject = subj,
                    Cat1 = cat1,
                    Cat2 = cat2,
                    MidTerm = mid,
                    EndTerm = end,
                    Average = avg,
                    Grade = GradeFromMark(avg),
                    Status = avg < 40 ? "At Risk" : avg >= 70 ? "Excellent" : "On Track"
                });
            }

            var overallAvg = Math.Round(subjectPerformances.Average(s => s.Average), 1);
            var rank = random.Next(1, 25);

            return new StudentPerformanceDetail
            {
                StudentName = studentName,
                AdmissionNumber = admissionNumber,
                ClassName = className,
                Stream = stream,
                AcademicYear = academicYear,
                Term = term,
                SubjectPerformances = subjectPerformances,
                OverallAverage = overallAvg,
                OverallGrade = GradeFromMark(overallAvg),
                Rank = rank,
                Status = overallAvg < 40 ? "At Risk" : overallAvg >= 70 ? "Excellent" : "On Track"
            };
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
