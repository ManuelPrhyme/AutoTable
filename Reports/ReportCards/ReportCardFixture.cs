using System;
using System.Collections.Generic;

namespace AutoTable.Reports.ReportCards
{
    /// <summary>
    /// Test fixture with sample data matching the design specification reference.
    /// Use this for development and visual QA — not for production.
    /// </summary>
    public static class ReportCardFixture
    {
        public static ReportCardData CreateSampleReport()
        {
            return new ReportCardData(
                School: CreateSchoolSettings(),
                Student: CreateStudentData(),
                Subjects: CreateSubjectResults(),
                GradingScale: CreateGradingScale());
        }

        /// <summary>
        /// Stress-test variant: long school name, many subjects, extended comments.
        /// Tests one-page overflow boundaries.
        /// </summary>
        public static ReportCardData CreateStressTestReport()
        {
            var school = CreateSchoolSettings();
            school = school with
            {
                Name = "ST. MARY'S COLLEGE OF ADVANCED LEARNING",
                Subtitle = "COMPREHENSIVE SECONDARY SCHOOL",
                Motto = "Through Knowledge We Serve and Lead with Integrity and Compassion"
            };

            var student = CreateStudentData();
            student = student with
            {
                Name = "NABUATI NANGOBI FLAVIA",
                TeacherComments = "Nabuwati has demonstrated exceptional academic growth this term, particularly in the sciences where she consistently scored above the class average. Her critical thinking skills have matured significantly, and she often contributes insightful perspectives during group discussions. She shows natural leadership qualities and mentored three junior students in Mathematics. Her conduct is exemplary, and she serves as a role model for her peers. I strongly recommend her for the science track in Senior 4.Keep nurturing her talents and never stop believing in herself.",
                PrincipalRemark = "It is with great pleasure that I commend Nabuwati for her outstanding performance this term. She has not only excelled academically but has also demonstrated remarkable character and dedication to the school's values. Her consistent improvement across all subjects reflects genuine effort and commitment. I encourage her to continue this trajectory and aim for the top positions in the upcoming national examinations. The school is proud of her achievements.",
                ClassTeacherName = "Mr. James Okello",
                TotalMarks = 2680,
                MaximumMarks = 3600
            };

            var subjects = new List<SubjectResult>
            {
                new() { SubjectName = "English Language",      MaximumMarks = 100, Cat1 = 22, Cat2 = 20, Exam = 58, Total = 246, Average = 82.0m, Grade = "B+" },
                new() { SubjectName = "Mathematics",           MaximumMarks = 100, Cat1 = 18, Cat2 = 17, Exam = 55, Total = 210, Average = 70.0m, Grade = "B-" },
                new() { SubjectName = "Physics",               MaximumMarks = 100, Cat1 = 19, Cat2 = 21, Exam = 58, Total = 230, Average = 76.7m, Grade = "B"  },
                new() { SubjectName = "Chemistry",             MaximumMarks = 100, Cat1 = 20, Cat2 = 18, Exam = 62, Total = 236, Average = 78.7m, Grade = "B+" },
                new() { SubjectName = "Biology",               MaximumMarks = 100, Cat1 = 22, Cat2 = 20, Exam = 65, Total = 270, Average = 90.0m, Grade = "A"  },
                new() { SubjectName = "Literature in English", MaximumMarks = 100, Cat1 = 17, Cat2 = 19, Exam = 54, Total = 200, Average = 66.7m, Grade = "B-" },
                new() { SubjectName = "Computer Studies",      MaximumMarks = 100, Cat1 = 21, Cat2 = 19, Exam = 60, Total = 240, Average = 80.0m, Grade = "B+" },
                new() { SubjectName = "Social Studies",        MaximumMarks = 100, Cat1 = 16, Cat2 = 18, Exam = 56, Total = 204, Average = 68.0m, Grade = "C"  },
                new() { SubjectName = "Religious Education",   MaximumMarks = 100, Cat1 = 23, Cat2 = 21, Exam = 60, Total = 256, Average = 85.3m, Grade = "A"  },
                new() { SubjectName = "Physical Education",    MaximumMarks = 100, Cat1 = 15, Cat2 = 17, Exam = 50, Total = 184, Average = 61.3m, Grade = "C"  },
                new() { SubjectName = "Agriculture",           MaximumMarks = 100, Cat1 = 18, Cat2 = 20, Exam = 57, Total = 220, Average = 73.3m, Grade = "B"  },
                new() { SubjectName = "Fine Art",              MaximumMarks = 100, Cat1 = 24, Cat2 = 22, Exam = 58, Total = 260, Average = 86.7m, Grade = "A"  }
            };

            return new ReportCardData(school, student, subjects, CreateGradingScale());
        }

        private static SchoolReportCardSettings CreateSchoolSettings()
        {
            return new SchoolReportCardSettings
            {
                Name = "BRIGHT FUTURE",
                Subtitle = "SECONDARY SCHOOL",
                Motto = "Knowledge. Discipline. Excellence.",
                AcademicYear = "2025/2026",

                PrimaryColor = "#00184D",
                SecondaryColor = "#0A2860",
                AccentColor = "#E2A01B",
                TableBgColor = "#ECF3FE",
                GridColor = "#9FB4D2",

                BodyFont = "Lato",
                DisplayFont = "Georgia",

                PostalAddress = "P.O. Box 12345, Kampala, Uganda",
                Phone = "+256 701 234 567",
                Email = "info@brightfuture.sc.ug",
                Website = "www.brightfuture.sc.ug",

                PrincipalName = "Dr. Sarah Johnson"
            };
        }

        private static StudentReportCardData CreateStudentData()
        {
            return new StudentReportCardData
            {
                Name = "NABUATI NANGOBI",
                AdmissionNumber = "SSS/2023/0158",
                DateOfBirth = new DateOnly(2010, 3, 12),
                Gender = "Female",
                ClassName = "S.3 BLUE",
                Term = "SECOND TERM",
                AcademicYear = "2025/2026",
                House = "EDISON",
                ReportDate = new DateOnly(2026, 5, 28),

                TeacherComments = "Nabuwati has shown consistent improvement this term. She demonstrates strong analytical skills in Science and Mathematics. Her participation in class discussions has been exemplary. I encourage her to maintain this momentum and continue striving for excellence.",
                PrincipalRemark = "Congratulations on a successful term. Your dedication to academic excellence is commendable. Keep up the good work and continue to be a positive influence on your peers.",
                ClassTeacherName = "Mr. James Okello",

                TotalMarks = 1836,
                MaximumMarks = 2400,
                OverallAverage = 79.5m,
                OverallGrade = "B+",
                ClassPosition = 7,
                ClassSize = 32,
                Status = "PROMOTED"
            };
        }

        private static IReadOnlyList<SubjectResult> CreateSubjectResults()
        {
            return new List<SubjectResult>
            {
                new() { SubjectName = "English",            MaximumMarks = 100, Cat1 = 20, Cat2 = 20, Exam = 60, Total = 246, Average = 82.0m, Grade = "B+" },
                new() { SubjectName = "Mathematics",        MaximumMarks = 100, Cat1 = 18, Cat2 = 17, Exam = 55, Total = 210, Average = 70.0m, Grade = "B-" },
                new() { SubjectName = "Science",            MaximumMarks = 100, Cat1 = 19, Cat2 = 21, Exam = 58, Total = 230, Average = 76.7m, Grade = "B"  },
                new() { SubjectName = "Social Studies",     MaximumMarks = 100, Cat1 = 20, Cat2 = 18, Exam = 62, Total = 236, Average = 78.7m, Grade = "B+" },
                new() { SubjectName = "Religious Education", MaximumMarks = 100, Cat1 = 22, Cat2 = 20, Exam = 65, Total = 270, Average = 90.0m, Grade = "A"  },
                new() { SubjectName = "Literature in English", MaximumMarks = 100, Cat1 = 17, Cat2 = 19, Exam = 54, Total = 200, Average = 66.7m, Grade = "B-" },
                new() { SubjectName = "Computer Studies",   MaximumMarks = 100, Cat1 = 21, Cat2 = 19, Exam = 60, Total = 240, Average = 80.0m, Grade = "B+" },
                new() { SubjectName = "Physical Education", MaximumMarks = 100, Cat1 = 16, Cat2 = 18, Exam = 56, Total = 204, Average = 68.0m, Grade = "C"  }
            };
        }

        private static IReadOnlyList<GradeBand> CreateGradingScale()
        {
            return new List<GradeBand>
            {
                new() { Grade = "A",   RangeDisplay = "80 – 100", Remark = "Excellent"  },
                new() { Grade = "B+",  RangeDisplay = "75 – 79",  Remark = "Very Good"  },
                new() { Grade = "B",   RangeDisplay = "70 – 74",  Remark = "Good"       },
                new() { Grade = "B-",  RangeDisplay = "65 – 69",  Remark = "Above Average" },
                new() { Grade = "C",   RangeDisplay = "60 – 64",  Remark = "Average"    },
                new() { Grade = "D",   RangeDisplay = "50 – 59",  Remark = "Below Average" },
                new() { Grade = "E",   RangeDisplay = "40 – 49",  Remark = "Poor"       },
                new() { Grade = "F",   RangeDisplay = "0 – 39",   Remark = "Fail"       }
            };
        }
    }
}