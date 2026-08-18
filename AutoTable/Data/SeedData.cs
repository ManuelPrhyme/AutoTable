using AutoTable.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace AutoTable.Data
{
    public static class SeedData
    {
        public static void EnsureSeed(AppDbContext db)
        {
            if (db.Classes.Any()) return;

            var classes = new[]
            {
                new ClassEntity { Name = "P1" },
                new ClassEntity { Name = "P2" },
                new ClassEntity { Name = "P3" },
                new ClassEntity { Name = "P4" },
                new ClassEntity { Name = "P5" },
                new ClassEntity { Name = "P6" },
                new ClassEntity { Name = "P7" },
            };
            db.Classes.AddRange(classes);

            var streams = new[]
            {
                new StreamEntity { Name = "Stream A" },
                new StreamEntity { Name = "Stream B" },
                new StreamEntity { Name = "Stream C" },
            };
            db.Streams.AddRange(streams);

            var subjects = new[]
            {
                new SubjectEntity { Name = "Mathematics" },
                new SubjectEntity { Name = "English" },
                new SubjectEntity { Name = "Science" },
                new SubjectEntity { Name = "Social Studies" },
                new SubjectEntity { Name = "Religious Education" },
            };
            db.Subjects.AddRange(subjects);

            var years = new[]
            {
                new AcademicYearEntity { Name = "2024-2025" },
                new AcademicYearEntity { Name = "2025-2026" }
            };
            db.AcademicYears.AddRange(years);

            var terms = new[]
            {
                new TermEntity { Name = "Term 1, 2025" },
                new TermEntity { Name = "Term 2, 2025" }
            };
            db.Terms.AddRange(terms);

            db.SaveChanges();
            // assign default subjects to classes (e.g., P5 -> Mathematics, English)
            var p5Entity = db.Classes.First(c => c.Name == "P5");
            var mathSubj = db.Subjects.First(s => s.Name == "Mathematics");
            var engSubj = db.Subjects.First(s => s.Name == "English");
            db.ClassSubjects.Add(new ClassSubjectEntity { ClassId = p5Entity.Id, SubjectId = mathSubj.Id });
            db.ClassSubjects.Add(new ClassSubjectEntity { ClassId = p5Entity.Id, SubjectId = engSubj.Id });
            db.SaveChanges();


            // Seed a few students
            var p5 = db.Classes.First(c => c.Name == "P5");
            var streamA = db.Streams.First();
            db.Students.Add(new StudentEntity { LIN = "LIN-0001", FullName = "Amina Nakato", AdmissionNumber = "BF-1042", ClassId = p5.Id, StreamId = streamA.Id, CreatedAt = DateTime.UtcNow });
            db.Students.Add(new StudentEntity { LIN = "LIN-0002", FullName = "Brian Okello", AdmissionNumber = "BF-1043", ClassId = p5.Id, StreamId = streamA.Id, CreatedAt = DateTime.UtcNow });

            db.Users.Add(new UserEntity { FullName = "Admin User", Email = "admin@example.local", Role = "Administrator" });

            // Simple sample assessments
            var math = db.Subjects.First(s => s.Name == "Mathematics");
            var term2 = db.Terms.First(t => t.Name.Contains("Term 2"));
            var ay = db.AcademicYears.First();
            db.Assessments.Add(new AssessmentEntity { Name = "Mid Term I", ClassId = p5.Id, SubjectId = math.Id, AcademicYearId = ay.Id, TermId = term2.Id, WeightPercent = 30, DueDate = DateTime.UtcNow.AddDays(7), IsVerified = true, IsPublished = true, MarksEnteredPercent = 100 });

            db.SaveChanges();

            // Add sample marks
            var assessment = db.Assessments.First();
            var students = db.Students.Take(2).ToList();
            db.Marks.Add(new MarkEntity { StudentId = students[0].Id, AssessmentId = assessment.Id, Mark = 78, Grade = "B", EnteredAt = DateTime.UtcNow });
            db.Marks.Add(new MarkEntity { StudentId = students[1].Id, AssessmentId = assessment.Id, Mark = 64, Grade = "C", EnteredAt = DateTime.UtcNow });

            db.SaveChanges();
        }
    }
}
