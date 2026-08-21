using AutoTable.Data;
using AutoTable.Data.Entities;
using AutoTable.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.Services
{
    public class DatabaseDataService : IDataService
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public DatabaseDataService(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        private AppDbContext CreateContext() => new AppDbContext(_options);

        public async Task<IReadOnlyList<AssessmentItem>> GetAssessmentsAsync()
        {
            using var db = CreateContext();
            var list = await db.Assessments
                .Include(a => a.Class)
                .Include(a => a.Subject)
                .OrderBy(a => a.DueDate)
                .ToListAsync();

            return list.Select(a => new AssessmentItem
            {
                Id = a.Id.ToString(),
                Name = a.Name,
                ClassName = a.Class?.Name ?? string.Empty,
                Subject = a.Subject?.Name ?? string.Empty,
                WeightPercent = a.WeightPercent,
                DueDate = a.DueDate ?? DateTime.MinValue,
                MarksEnteredPercent = a.MarksEnteredPercent,
                IsVerified = a.IsVerified,
                IsPublished = a.IsPublished
            }).ToList();
        }

        public async Task<IReadOnlyList<GradebookRow>> GetGradebookAsync(string className, string subject, string? academicYear = null, string? term = null, string? stream = null, string? studentName = null)
        {
            using var db = CreateContext();
            var cls = await db.Classes.FirstOrDefaultAsync(c => c.Name == className) ?? db.Classes.FirstOrDefault();
            var subj = await db.Subjects.FirstOrDefaultAsync(s => s.Name == subject) ?? db.Subjects.FirstOrDefault();

            if (cls == null || subj == null) return new List<GradebookRow>();

            var marksQuery = db.Marks
                .Include(m => m.Student)
                .Include(m => m.Assessment)
                .Where(m => m.Assessment!.ClassId == cls.Id && m.Assessment.SubjectId == subj.Id);

            if (!string.IsNullOrWhiteSpace(academicYear))
            {
                var year = await db.AcademicYears.FirstOrDefaultAsync(y => y.Name == academicYear);
                if (year != null) marksQuery = marksQuery.Where(m => m.Assessment!.AcademicYearId == year.Id);
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                var termEntity = await db.Terms.FirstOrDefaultAsync(t => t.Name == term);
                if (termEntity != null) marksQuery = marksQuery.Where(m => m.Assessment!.TermId == termEntity.Id);
            }

            var marks = await marksQuery.ToListAsync();

            if (!string.IsNullOrWhiteSpace(stream))
                marks = marks.Where(m => string.Equals(m.Student?.Stream?.Name, stream, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(studentName))
                marks = marks.Where(m => m.Student?.FullName?.Contains(studentName, StringComparison.OrdinalIgnoreCase) == true).ToList();

            var rows = marks
                .GroupBy(m => m.Student!)
                .Select(g =>
                {
                    var scores = g.Select(x => x.Mark ?? 0).ToList();
                    var avg = scores.Any() ? Math.Round(scores.Average(), 1) : 0;
                    return new GradebookRow
                    {
                        StudentName = g.Key.FullName,
                    // Use LIN as the identifier presented to users instead of the legacy admission number
                    AdmissionNumber = g.Key.LIN ?? string.Empty,
                        ClassName = cls.Name,
                        Stream = g.Key.Stream?.Name ?? string.Empty,
                        Subject = subj.Name,
                        AcademicYear = academicYear ?? string.Empty,
                        Term = term ?? string.Empty,
                        Cat1 = scores.ElementAtOrDefault(0),
                        Cat2 = scores.ElementAtOrDefault(1),
                        MidTerm = scores.ElementAtOrDefault(2),
                        EndTerm = scores.ElementAtOrDefault(3),
                        Average = avg,
                        Grade = avg > 0 ? GradeFromAverage(avg) : "-",
                        Status = avg < 40 ? "At Risk" : avg >= 70 ? "Excellent" : "On Track"
                    };
                }).OrderByDescending(r => r.Average).ToList();

            for (int i = 0; i < rows.Count; i++) rows[i].Rank = i + 1;
            return rows;
        }

        private static string GradeFromAverage(double mark) => mark switch
        {
            >= 80 => "A",
            >= 70 => "B",
            >= 60 => "C",
            >= 50 => "D",
            >= 40 => "E",
            _ => "F"
        };

        public async Task<IReadOnlyList<StudentMarkRow>> GetStudentMarksAsync(string className, string subject, string assessmentName)
        {
            using var db = CreateContext();
            var cls = await db.Classes.FirstOrDefaultAsync(c => c.Name == className) ?? db.Classes.FirstOrDefault();
            var subj = await db.Subjects.FirstOrDefaultAsync(s => s.Name == subject) ?? db.Subjects.FirstOrDefault();
            var clsId = cls?.Id ?? 0;
            var subjId = subj?.Id ?? 0;
            var assess = await db.Assessments.FirstOrDefaultAsync(a =>
                a.Name == assessmentName && a.ClassId == clsId && a.SubjectId == subjId);

            if (cls == null || subj == null || assess == null)
            {
                var studs = await db.Students.Where(s => s.ClassId == clsId && s.IsActive).ToListAsync();
                return studs.Select((s, i) => new StudentMarkRow
                {
                    StudentId = s.Id.ToString(),
                    StudentName = s.FullName,
                        AdmissionNumber = s.LIN ?? string.Empty,
                    ClassName = cls?.Name ?? string.Empty,
                    Mark = null,
                    Grade = "-",
                    Remarks = string.Empty,
                    IsEditable = true
                }).ToList();
            }

            var marks = await db.Marks.Include(m => m.Student)
                .Where(m => m.AssessmentId == assess.Id)
                .ToListAsync();

            var students = await db.Students.Where(s => s.ClassId == cls.Id && s.IsActive).ToListAsync();
            var result = students.Select(s =>
            {
                var mark = marks.FirstOrDefault(m => m.StudentId == s.Id);
                return new StudentMarkRow
                {
                    StudentId = s.Id.ToString(),
                    StudentName = s.FullName,
                    AdmissionNumber = s.LIN ?? string.Empty,
                    ClassName = cls.Name,
                    Mark = mark?.Mark,
                    Grade = mark?.Grade ?? "-",
                    Remarks = mark?.Remarks ?? string.Empty,
                    IsEditable = true
                };
            }).ToList();

            return result;
        }

        public async Task<IReadOnlyList<string>> GetTermsAsync()
        {
            using var db = CreateContext();
            return await db.Terms.OrderBy(t => t.Name).Select(t => t.Name).ToListAsync();
        }

        public async Task<IReadOnlyList<string>> GetAcademicYearsAsync()
        {
            using var db = CreateContext();
            return await db.AcademicYears.OrderBy(y => y.Name).Select(y => y.Name).ToListAsync();
        }

        public async Task<IReadOnlyList<string>> GetStreamsAsync()
        {
            using var db = CreateContext();
            return await db.Streams.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync();
        }

        public async Task<IReadOnlyList<string>> GetAllStudentsAsync()
        {
            using var db = CreateContext();
            return await db.Students.Where(s => s.IsActive).OrderBy(s => s.FullName).Select(s => s.FullName).ToListAsync();
        }

        public async Task<Models.StudentPerformanceDetail> GetStudentPerformanceDetailAsync(
            string studentName, string className, string subject, string academicYear, string term, string stream)
        {
            using var db = CreateContext();
            var student = await db.Students
                .Include(s => s.Class)
                .Include(s => s.Stream)
                .FirstOrDefaultAsync(s => s.FullName == studentName && s.IsActive);

            var detail = new Models.StudentPerformanceDetail
            {
                StudentName = studentName,
                AdmissionNumber = student?.LIN ?? string.Empty,
                ClassName = className,
                Stream = stream,
                AcademicYear = academicYear,
                Term = term
            };

            var marksQuery = db.Marks.Include(m => m.Assessment).ThenInclude(a => a!.Subject)
                .Where(m => m.Student!.FullName == studentName);

            var year = await db.AcademicYears.FirstOrDefaultAsync(y => y.Name == academicYear);
            var termEntity = await db.Terms.FirstOrDefaultAsync(t => t.Name == term);
            if (year != null) marksQuery = marksQuery.Where(m => m.Assessment!.AcademicYearId == year.Id);
            if (termEntity != null) marksQuery = marksQuery.Where(m => m.Assessment!.TermId == termEntity.Id);

            var marks = await marksQuery.ToListAsync();
            foreach (var group in marks.GroupBy(m => m.Assessment!.Subject!.Name).OrderBy(g => g.Key))
            {
                var scores = group.Select(m => m.Mark ?? 0).OrderByDescending(v => v).ToList();
                var avg = scores.Count > 0 ? Math.Round(scores.Average(), 1) : 0;
                detail.SubjectPerformances.Add(new Models.StudentSubjectPerformance
                {
                    Subject = group.Key,
                    Cat1 = scores.ElementAtOrDefault(0),
                    Cat2 = scores.ElementAtOrDefault(1),
                    MidTerm = scores.ElementAtOrDefault(2),
                    EndTerm = scores.ElementAtOrDefault(3),
                    Average = avg,
                    Grade = avg > 0 ? GradeFromAverage(avg) : "-",
                    Status = avg < 40 ? "At Risk" : avg >= 70 ? "Excellent" : "On Track"
                });
            }

            if (detail.SubjectPerformances.Count == 0)
            {
                // No marks available for selected filters — show a placeholder row for the subject
                detail.SubjectPerformances.Add(new Models.StudentSubjectPerformance
                {
                    Subject = subject,
                    Average = 0,
                    Grade = "-",
                    Status = "No Data"
                });
            }

            detail.OverallAverage = detail.SubjectPerformances.Count == 0 ? 0
                : Math.Round(detail.SubjectPerformances.Average(s => s.Average), 1);
            detail.OverallGrade = detail.OverallAverage > 0 ? GradeFromAverage(detail.OverallAverage) : "-";
            detail.Status = detail.OverallAverage < 40 ? "At Risk" : detail.OverallAverage >= 70 ? "Excellent" : detail.OverallAverage > 0 ? "On Track" : "No Data";

            // Rank within class for the requested subject
            var cls = student?.ClassId != null ? await db.Classes.FindAsync(student.ClassId) : null;
            if (cls != null)
            {
                var clsMarks = await db.Marks.Include(m => m.Assessment).Where(m => m.Assessment!.ClassId == cls.Id && m.Assessment.Subject!.Name == subject).ToListAsync();
                var classAvgs = clsMarks
                    .GroupBy(m => m.StudentId)
                    .Select(g => g.Average(m => m.Mark ?? 0))
                    .OrderByDescending(v => v)
                    .ToList();
                var ownAvg = detail.OverallAverage;
                detail.Rank = classAvgs.Count == 0 ? 0 : classAvgs.TakeWhile(v => v > ownAvg).Count() + 1;
            }

            return detail;
        }

        public async Task<IReadOnlyList<Student>> GetStudentsAsync()
        {
            using var db = CreateContext();
            var list = await db.Students.Include(s => s.Class).Include(s => s.Stream).OrderBy(s => s.FullName).ToListAsync();
            return list.Select(s => new Student
            {
                Id = s.Id,
                LIN = s.LIN,
                FullName = s.FullName,
                    AdmissionNumber = s.LIN,
                ClassId = s.ClassId,
                StreamId = s.StreamId,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                IsActive = s.IsActive,
                TerminationReason = (StudentTerminationReason)s.TerminationReason,
                TerminationDate = s.TerminationDate,
                CreatedAt = s.CreatedAt
            }).ToList();
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            using var db = CreateContext();
            var s = await db.Students.FindAsync(id);
            if (s == null) return null;
            return new Student
            {
                Id = s.Id,
                LIN = s.LIN,
                FullName = s.FullName,
                    AdmissionNumber = s.LIN,
                ClassId = s.ClassId,
                StreamId = s.StreamId,
                DateOfBirth = s.DateOfBirth,
                Gender = s.Gender,
                IsActive = s.IsActive,
                TerminationReason = (StudentTerminationReason)s.TerminationReason,
                TerminationDate = s.TerminationDate,
                CreatedAt = s.CreatedAt
            };
        }

        public async Task<Student> CreateStudentAsync(Student student)
        {
            // delegate to transactional create with no initial data
            var created = await CreateStudentWithInitialDataAsync(student, null, null);
            return created;
        }

        public async Task<Student> CreateStudentWithInitialDataAsync(
            Student student,
            double? initialFeeAmount = null,
            IEnumerable<(int AssessmentId, double? Mark, string? Grade)>? initialMarks = null)
        {
            using var db = CreateContext();
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                if (await db.Students.AnyAsync(s => s.LIN == student.LIN))
                    throw new InvalidOperationException("LIN already exists.");

                var entity = new StudentEntity
                {
                    LIN = student.LIN,
                    FullName = student.FullName,
                AdmissionNumber = student.LIN,
                    ClassId = student.ClassId,
                    StreamId = student.StreamId,
                    DateOfBirth = student.DateOfBirth,
                    Gender = student.Gender,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // map extended enrollment fields if provided on the model
                entity.AdmissionNumber = student.AdmissionNumber ?? entity.AdmissionNumber;
                entity.GuardianName = student.GuardianName;
                entity.GuardianRelationship = student.GuardianRelationship;
                entity.GuardianPhone = student.GuardianPhone;
                entity.GuardianEmail = student.GuardianEmail;
                entity.GuardianAddress = student.GuardianAddress;
                entity.HasCustodyDocuments = student.HasCustodyDocuments;
                entity.ResidenceProofType = student.ResidenceProofType;
                entity.ResidenceDistrict = student.ResidenceDistrict;
                entity.ResidenceZone = student.ResidenceZone;
                entity.HasImmunizationCard = student.HasImmunizationCard;
                entity.HasMedicalExamReport = student.HasMedicalExamReport;
                entity.AllergiesOrConditions = student.AllergiesOrConditions;
                entity.HealthInsurance = student.HealthInsurance;
                entity.EmergencyName = student.EmergencyName;
                entity.EmergencyRelationship = student.EmergencyRelationship;
                entity.EmergencyPhone = student.EmergencyPhone;
                entity.AuthorizedPickupPerson = student.AuthorizedPickupPerson;

                db.Students.Add(entity);
                await db.SaveChangesAsync();

                if (initialFeeAmount.HasValue)
                {
                    var fee = new FeePaymentEntity
                    {
                        StudentId = entity.Id,
                        Amount = initialFeeAmount.Value,
                        PaymentDate = DateTime.UtcNow,
                        Description = "Initial fee (seeded)"
                    };
                    db.FeePayments.Add(fee);
                    await db.SaveChangesAsync();
                }

                if (initialMarks != null)
                {
                    foreach (var m in initialMarks)
                    {
                        var assessment = await db.Assessments.FindAsync(m.AssessmentId);
                        if (assessment == null)
                            throw new InvalidOperationException($"Assessment {m.AssessmentId} not found.");

                        if (await db.Marks.AnyAsync(x => x.StudentId == entity.Id && x.AssessmentId == m.AssessmentId))
                            continue;

                        var markEntity = new MarkEntity
                        {
                            StudentId = entity.Id,
                            AssessmentId = m.AssessmentId,
                            Mark = m.Mark,
                            Grade = m.Grade,
                            EnteredAt = DateTime.UtcNow
                        };
                        db.Marks.Add(markEntity);
                    }
                    await db.SaveChangesAsync();
                }

                await tx.CommitAsync();

                student.Id = entity.Id;
                student.CreatedAt = entity.CreatedAt;
                student.IsActive = entity.IsActive;
                return student;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<Student?> UpdateStudentAsync(Student student)
        {
            using var db = CreateContext();
            var e = await db.Students.FindAsync(student.Id);
            if (e == null) return null;
            if (e.LIN != student.LIN && await db.Students.AnyAsync(s => s.LIN == student.LIN && s.Id != student.Id))
                throw new InvalidOperationException("LIN already exists.");

            e.LIN = student.LIN;
            e.FullName = student.FullName;
            // Persist the LIN value into the model's AdmissionNumber slot for legacy consumers
            e.AdmissionNumber = student.AdmissionNumber;
            e.LIN = student.LIN;
            e.ClassId = student.ClassId;
            e.StreamId = student.StreamId;
            e.DateOfBirth = student.DateOfBirth;
            e.Gender = student.Gender;
            // update extended enrollment fields
            e.GuardianName = student.GuardianName;
            e.GuardianRelationship = student.GuardianRelationship;
            e.GuardianPhone = student.GuardianPhone;
            e.GuardianEmail = student.GuardianEmail;
            e.GuardianAddress = student.GuardianAddress;
            e.HasCustodyDocuments = student.HasCustodyDocuments;
            e.ResidenceProofType = student.ResidenceProofType;
            e.ResidenceDistrict = student.ResidenceDistrict;
            e.ResidenceZone = student.ResidenceZone;
            e.HasImmunizationCard = student.HasImmunizationCard;
            e.HasMedicalExamReport = student.HasMedicalExamReport;
            e.AllergiesOrConditions = student.AllergiesOrConditions;
            e.HealthInsurance = student.HealthInsurance;
            e.EmergencyName = student.EmergencyName;
            e.EmergencyRelationship = student.EmergencyRelationship;
            e.EmergencyPhone = student.EmergencyPhone;
            e.AuthorizedPickupPerson = student.AuthorizedPickupPerson;
            await db.SaveChangesAsync();

            return student;
        }

        public async Task TerminateStudentAsync(int studentId, StudentTerminationReason reason, DateTime date, bool anonymize = false)
        {
            using var db = CreateContext();
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var e = await db.Students.FindAsync(studentId);
                if (e == null) throw new InvalidOperationException("Student not found.");
                e.IsActive = false;
                e.TerminationReason = (int)reason;
                e.TerminationDate = date;

                var nameAtTermination = e.FullName;

                if (anonymize)
                {
                    e.FullName = $"Terminated-{e.Id}";
                    e.AdmissionNumber = null;
                    e.LIN = $"REMOVED-{e.Id}";
                    e.LIN = $"REMOVED-{e.Id}";
                }

                db.Students.Update(e);

                // Audit trail: record termination event in the same transaction
                db.TerminationLogs.Add(new Data.Entities.TerminationLogEntity
                {
                    StudentId = e.Id,
                    StudentNameAtTermination = nameAtTermination,
                    TerminationReason = (int)reason,
                    TerminationDate = date,
                    Anonymized = anonymize,
                    LoggedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<IReadOnlyList<TerminationLogItem>> GetTerminationLogAsync()
        {
            using var db = CreateContext();
            var logs = await db.TerminationLogs
                .OrderByDescending(t => t.LoggedAt)
                .ToListAsync();

            return logs.Select(t => new TerminationLogItem
            {
                Id = t.Id,
                StudentId = t.StudentId,
                StudentName = t.StudentNameAtTermination,
                Reason = ((StudentTerminationReason)t.TerminationReason).ToString(),
                TerminationDate = t.TerminationDate,
                Anonymized = t.Anonymized,
                LoggedAt = t.LoggedAt
            }).ToList();
        }

        public async Task SaveEnrollmentAsync(EnrollmentFormData enrollment)
        {
            using var db = CreateContext();
            db.Enrollments.Add(new EnrollmentEntity
            {
                FullName = enrollment.FullName,
                DateOfBirth = enrollment.DateOfBirth,
                Gender = enrollment.Gender,
                Nationality = enrollment.Nationality,
                Religion = enrollment.Religion,
                PreviousSchool = enrollment.PreviousSchool,
                // Store the provided LIN as the primary identifier; keep AdmissionNumber for compatibility
                AdmissionNumber = enrollment.AdmissionNumber,
                LIN = enrollment.LIN,
                GuardianName = enrollment.GuardianName,
                GuardianRelationship = enrollment.GuardianRelationship,
                GuardianPhone = enrollment.GuardianPhone,
                GuardianEmail = enrollment.GuardianEmail,
                GuardianAddress = enrollment.GuardianAddress,
                HasCustodyDocuments = enrollment.HasCustodyDocuments,
                ResidenceProofType = enrollment.ResidenceProofType,
                ResidenceDistrict = enrollment.ResidenceDistrict,
                ResidenceZone = enrollment.ResidenceZone,
                HasImmunizationCard = enrollment.HasImmunizationCard,
                HasMedicalExamReport = enrollment.HasMedicalExamReport,
                AllergiesOrConditions = enrollment.AllergiesOrConditions,
                HealthInsurance = enrollment.HealthInsurance,
                EmergencyName = enrollment.EmergencyName,
                EmergencyRelationship = enrollment.EmergencyRelationship,
                EmergencyPhone = enrollment.EmergencyPhone,
                AuthorizedPickupPerson = enrollment.AuthorizedPickupPerson,
                SubmittedAt = DateTime.UtcNow,
                Status = "New"
            });
            await db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<EnrollmentFormData>> GetEnrollmentsAsync()
        {
            using var db = CreateContext();
            var list = await db.Enrollments
                .OrderByDescending(e => e.SubmittedAt)
                .ToListAsync();

            return list.Select(e => new EnrollmentFormData
            {
                FullName = e.FullName,
                LIN = e.LIN,
                DateOfBirth = e.DateOfBirth,
                Gender = e.Gender,
                Nationality = e.Nationality,
                Religion = e.Religion,
                PreviousSchool = e.PreviousSchool,
                AdmissionNumber = e.AdmissionNumber,
                GuardianName = e.GuardianName,
                GuardianRelationship = e.GuardianRelationship,
                GuardianPhone = e.GuardianPhone,
                GuardianEmail = e.GuardianEmail,
                GuardianAddress = e.GuardianAddress,
                HasCustodyDocuments = e.HasCustodyDocuments,
                ResidenceProofType = e.ResidenceProofType,
                ResidenceDistrict = e.ResidenceDistrict,
                ResidenceZone = e.ResidenceZone,
                HasImmunizationCard = e.HasImmunizationCard,
                HasMedicalExamReport = e.HasMedicalExamReport,
                AllergiesOrConditions = e.AllergiesOrConditions,
                HealthInsurance = e.HealthInsurance,
                EmergencyName = e.EmergencyName,
                EmergencyRelationship = e.EmergencyRelationship,
                EmergencyPhone = e.EmergencyPhone,
                AuthorizedPickupPerson = e.AuthorizedPickupPerson,
                SubmittedAt = e.SubmittedAt,
                Status = e.Status
            }).ToList();
        }

        // --- Class & Subject management ---
        public async Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetClassesAsync()
        {
            using var db = CreateContext();
            var list = await db.Classes.OrderBy(c => c.Name).ToListAsync();
            return list.Select(c => new AutoTable.Models.SimpleLookup { Id = c.Id, Name = c.Name }).ToList();
        }

        public async Task<AutoTable.Models.SimpleLookup> CreateClassAsync(string name)
        {
            using var db = CreateContext();
            if (await db.Classes.AnyAsync(c => c.Name == name))
                throw new InvalidOperationException("Class already exists.");
            var c = new ClassEntity { Name = name };
            db.Classes.Add(c);
            await db.SaveChangesAsync();
            return new AutoTable.Models.SimpleLookup { Id = c.Id, Name = c.Name };
        }

        public async Task DeleteClassAsync(int classId)
        {
            using var db = CreateContext();
            var c = await db.Classes.FindAsync(classId);
            if (c == null) return;
            // prevent deletion if students or assessments exist
            var hasStudents = await db.Students.AnyAsync(s => s.ClassId == classId);
            var hasAssessments = await db.Assessments.AnyAsync(a => a.ClassId == classId);
            if (hasStudents || hasAssessments)
                throw new InvalidOperationException("Cannot delete class with existing students or assessments.");
            db.Classes.Remove(c);
            await db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetSubjectsAsync()
        {
            using var db = CreateContext();
            var list = await db.Subjects.OrderBy(s => s.Name).ToListAsync();
            return list.Select(s => new AutoTable.Models.SimpleLookup { Id = s.Id, Name = s.Name }).ToList();
        }

        public async Task<AutoTable.Models.SimpleLookup> CreateSubjectAsync(string name)
        {
            using var db = CreateContext();
            if (await db.Subjects.AnyAsync(s => s.Name == name))
                throw new InvalidOperationException("Subject already exists.");
            var s = new SubjectEntity { Name = name };
            db.Subjects.Add(s);
            await db.SaveChangesAsync();
            return new AutoTable.Models.SimpleLookup { Id = s.Id, Name = s.Name };
        }

        public async Task DeleteSubjectAsync(int subjectId)
        {
            using var db = CreateContext();
            var s = await db.Subjects.FindAsync(subjectId);
            if (s == null) return;
            var hasAssessments = await db.Assessments.AnyAsync(a => a.SubjectId == subjectId);
            if (hasAssessments)
                throw new InvalidOperationException("Cannot delete subject with existing assessments.");
            db.Subjects.Remove(s);
            await db.SaveChangesAsync();
        }

        public async Task AssignSubjectToClassAsync(int classId, int subjectId)
        {
            using var db = CreateContext();
            if (!await db.Classes.AnyAsync(c => c.Id == classId)) throw new InvalidOperationException("Class not found.");
            if (!await db.Subjects.AnyAsync(s => s.Id == subjectId)) throw new InvalidOperationException("Subject not found.");
            if (await db.ClassSubjects.AnyAsync(cs => cs.ClassId == classId && cs.SubjectId == subjectId)) return;
            db.ClassSubjects.Add(new ClassSubjectEntity { ClassId = classId, SubjectId = subjectId });
            await db.SaveChangesAsync();
        }

        public async Task RemoveSubjectFromClassAsync(int classId, int subjectId)
        {
            using var db = CreateContext();
            var cs = await db.ClassSubjects.FindAsync(classId, subjectId);
            if (cs == null) return;
            // prevent removal if assessments exist for this class-subject
            var hasAssessments = await db.Assessments.AnyAsync(a => a.ClassId == classId && a.SubjectId == subjectId);
            if (hasAssessments)
                throw new InvalidOperationException("Cannot remove subject assigned to class while assessments exist for that pairing.");
            db.ClassSubjects.Remove(cs);
            await db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetSubjectsForClassAsync(int classId)
        {
            using var db = CreateContext();
            var list = await db.ClassSubjects
                .Where(cs => cs.ClassId == classId)
                .Include(cs => cs.Subject)
                .Select(cs => cs.Subject!)
                .ToListAsync();
            return list.Select(s => new AutoTable.Models.SimpleLookup { Id = s.Id, Name = s.Name }).ToList();
        }

        public async Task CreateAssessmentAsync(AutoTable.Models.AssessmentItem item)
        {
            using var db = CreateContext();
            // resolve class and subject
            var cls = await db.Classes.FirstOrDefaultAsync(c => c.Name == item.ClassName) ?? db.Classes.FirstOrDefault();
            var subj = await db.Subjects.FirstOrDefaultAsync(s => s.Name == item.Subject) ?? db.Subjects.FirstOrDefault();
            var ay = await db.AcademicYears.FirstOrDefaultAsync() ?? null;
            var term = await db.Terms.FirstOrDefaultAsync() ?? null;
            if (cls == null || subj == null || ay == null || term == null)
                throw new InvalidOperationException("Class, subject, academic year or term not found.");

            var entity = new AssessmentEntity
            {
                Name = item.Name,
                ClassId = cls.Id,
                SubjectId = subj.Id,
                AcademicYearId = ay.Id,
                TermId = term.Id,
                WeightPercent = item.WeightPercent,
                DueDate = item.DueDate,
                IsVerified = false,
                IsPublished = false,
                MarksEnteredPercent = 0
            };
            db.Assessments.Add(entity);
            await db.SaveChangesAsync();
        }
    }
}
