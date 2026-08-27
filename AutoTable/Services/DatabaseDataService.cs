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

        // Teacher CRUD implementations
        public async Task<IReadOnlyList<AutoTable.Models.Teacher>> GetTeachersAsync()
        {
            using var db = CreateContext();
            var users = await db.Users.Where(u => u.Role == "Teacher").OrderBy(u => u.FullName).ToListAsync();
            return users.Select(MapTeacher).ToList();
        }

        public async Task<AutoTable.Models.Teacher> CreateTeacherAsync(AutoTable.Models.Teacher teacher)
        {
            using var db = CreateContext();
            var entity = new UserEntity
            {
                FullName = teacher.FullName ?? string.Empty,
                Email = teacher.Email,
                Role = "Teacher",
                CreatedAt = DateTime.UtcNow,
                Phone = teacher.Phone,
                SubjectsTaught = teacher.SubjectsTaught,
                ClassesTaught = teacher.ClassesTaught,
                NextOfKinName = teacher.NextOfKinName,
                NextOfKinRelationship = teacher.NextOfKinRelationship,
                NextOfKinPhone = teacher.NextOfKinPhone,
                PreviousSchools = teacher.PreviousSchools,
                IsRegisteredTeacher = teacher.IsRegisteredTeacher,
                IsStudentTeacher = teacher.IsStudentTeacher
            };
            db.Users.Add(entity);
            await db.SaveChangesAsync();
            teacher.Id = entity.Id;
            return teacher;
        }

        public async Task<AutoTable.Models.Teacher?> UpdateTeacherAsync(AutoTable.Models.Teacher teacher)
        {
            using var db = CreateContext();
            var u = await db.Users.FindAsync(teacher.Id);
            if (u == null || u.Role != "Teacher") return null;
            u.FullName = teacher.FullName;
            u.Email = teacher.Email;
            u.Phone = teacher.Phone;
            u.SubjectsTaught = teacher.SubjectsTaught;
            u.ClassesTaught = teacher.ClassesTaught;
            u.NextOfKinName = teacher.NextOfKinName;
            u.NextOfKinRelationship = teacher.NextOfKinRelationship;
            u.NextOfKinPhone = teacher.NextOfKinPhone;
            u.PreviousSchools = teacher.PreviousSchools;
            u.IsRegisteredTeacher = teacher.IsRegisteredTeacher;
            u.IsStudentTeacher = teacher.IsStudentTeacher;
            db.Users.Update(u);
            await db.SaveChangesAsync();
            return MapTeacher(u);
        }

        private static AutoTable.Models.Teacher MapTeacher(UserEntity u) => new()
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            Phone = u.Phone,
            SubjectsTaught = u.SubjectsTaught,
            ClassesTaught = u.ClassesTaught,
            NextOfKinName = u.NextOfKinName,
            NextOfKinRelationship = u.NextOfKinRelationship,
            NextOfKinPhone = u.NextOfKinPhone,
            PreviousSchools = u.PreviousSchools,
            IsRegisteredTeacher = u.IsRegisteredTeacher,
            IsStudentTeacher = u.IsStudentTeacher
        };

        public async Task DeleteTeacherAsync(int teacherId)
        {
            using var db = CreateContext();
            var u = await db.Users.FindAsync(teacherId);
            if (u == null || u.Role != "Teacher") return;
            db.Users.Remove(u);
            await db.SaveChangesAsync();
        }

        public async Task AssignStudentToStreamAsync(int studentId, int streamId)
        {
            using var db = CreateContext();
            var s = await db.Students.FindAsync(studentId);
            if (s == null) throw new InvalidOperationException("Student not found.");
            if (!await db.Streams.AnyAsync(st => st.Id == streamId)) throw new InvalidOperationException("Stream not found.");
            s.StreamId = streamId;
            db.Students.Update(s);
            await db.SaveChangesAsync();
        }

        public async Task CreateFeePaymentAsync(int studentId, double amount, int? termId = null, int? recordedByUserId = null, string? description = null)
        {
            using var db = CreateContext();
            // Validate student exists
            var s = await db.Students.FindAsync(studentId);
            if (s == null) throw new InvalidOperationException("Student not found.");

            // If termId provided ensure it exists
            TermEntity? term = null;
            if (termId.HasValue)
            {
                term = await db.Terms.FindAsync(termId.Value);
                if (term == null) throw new InvalidOperationException("Term not found.");
            }

            var fee = new FeePaymentEntity
            {
                StudentId = studentId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                TermId = termId,
                RecordedByUserId = recordedByUserId,
                Description = description
            };
            db.FeePayments.Add(fee);
            await db.SaveChangesAsync();

            // Process overpayment → create/update credit record
            await ProcessOverpaymentCreditAsync(db, studentId, termId);
        }

        public async Task UpdateMarkAsync(int assessmentId, int studentId, double? mark, string? grade, string? remarks = null)
        {
            using var db = CreateContext();
            var existing = await db.Marks.FirstOrDefaultAsync(m => m.AssessmentId == assessmentId && m.StudentId == studentId);
            if (existing == null)
            {
                existing = new MarkEntity { AssessmentId = assessmentId, StudentId = studentId, Mark = mark, Grade = grade, Remarks = remarks, EnteredAt = DateTime.UtcNow };
                db.Marks.Add(existing);
            }
            else
            {
                existing.Mark = mark;
                existing.Grade = grade;
                existing.Remarks = remarks;
                existing.EnteredAt = DateTime.UtcNow;
                db.Marks.Update(existing);
            }
            await db.SaveChangesAsync();
            await UpdateAssessmentCompletionAsync(assessmentId);
        }

        public async Task DeleteMarkAsync(int assessmentId, int studentId)
        {
            using var db = CreateContext();
            var existing = await db.Marks.FirstOrDefaultAsync(m => m.AssessmentId == assessmentId && m.StudentId == studentId);
            if (existing == null) return;
            db.Marks.Remove(existing);
            await db.SaveChangesAsync();
            await UpdateAssessmentCompletionAsync(assessmentId);
        }

        public async Task UpdateAssessmentCompletionAsync(int assessmentId)
        {
            using var db = CreateContext();
            var assess = await db.Assessments.Include(a => a.Marks).FirstOrDefaultAsync(a => a.Id == assessmentId);
            if (assess == null) return;
            var studentsInClass = await db.Students.Where(s => s.ClassId == assess.ClassId && s.IsActive).CountAsync();
            var marksEntered = assess.Marks.Count(m => m.Mark != null);
            assess.MarksEnteredPercent = studentsInClass == 0 ? 0 : (int)Math.Round(marksEntered * 100.0 / studentsInClass);
            db.Assessments.Update(assess);
            await db.SaveChangesAsync();
        }

        // Moderation lifecycle (Phase 4)
        public async Task VerifyAssessmentAsync(int assessmentId, bool verified)
        {
            using var db = CreateContext();
            var a = await db.Assessments.FindAsync(assessmentId);
            if (a == null) throw new InvalidOperationException("Assessment not found.");
            a.IsVerified = verified;
            if (!verified) a.IsPublished = false; // un-verifying also unpublishes
            db.Assessments.Update(a);
            await db.SaveChangesAsync();
        }

        public async Task PublishAssessmentAsync(int assessmentId, bool published)
        {
            using var db = CreateContext();
            var a = await db.Assessments.FindAsync(assessmentId);
            if (a == null) throw new InvalidOperationException("Assessment not found.");
            if (published && !a.IsVerified)
                throw new InvalidOperationException("Assessment must be verified before it can be published.");
            a.IsPublished = published;
            db.Assessments.Update(a);
            await db.SaveChangesAsync();
        }

        // Fee payment reads (Phase 5)
        public async Task<IReadOnlyList<FeePaymentSummary>> GetFeePaymentsAsync(int? classId = null, int? termId = null)
        {
            using var db = CreateContext();
            var query = db.FeePayments
                .Include(fp => fp.Student).ThenInclude(s => s!.Class)
                .Include(fp => fp.Term)
                .AsQueryable();

            if (classId.HasValue)
                query = query.Where(fp => fp.Student != null && fp.Student.ClassId == classId.Value);
            if (termId.HasValue)
                query = query.Where(fp => fp.TermId == termId.Value);

            var list = await query.OrderByDescending(fp => fp.PaymentDate).ToListAsync();

            return list.Select(fp => new FeePaymentSummary
            {
                Id = fp.Id,
                StudentId = fp.StudentId,
                StudentName = fp.Student?.FullName ?? string.Empty,
                AdmissionNumber = fp.Student?.LIN ?? string.Empty,
                ClassName = fp.Student?.Class?.Name ?? string.Empty,
                Amount = fp.Amount,
                PaymentDate = fp.PaymentDate,
                TermId = fp.TermId,
                TermName = fp.Term?.Name ?? string.Empty,
                Description = fp.Description
            }).ToList();
        }

        public async Task<IReadOnlyList<AutoTable.Models.DefaulterRecord>> GetDefaultersAsync(int? termId = null, int? classId = null, decimal? minBalance = null)
        {
            using var db = CreateContext();

            var studentsQuery = db.Students
                .Include(s => s.Class)
                .Where(s => s.IsActive)
                .AsQueryable();

            if (classId.HasValue)
                studentsQuery = studentsQuery.Where(s => s.ClassId == classId.Value);

            var students = await studentsQuery.ToListAsync();

            var termFeesQuery = db.TermFees.AsQueryable();
            if (termId.HasValue)
                termFeesQuery = termFeesQuery.Where(tf => tf.TermId == termId.Value);
            var termFees = await termFeesQuery.ToListAsync();

            var paymentsQuery = db.FeePayments.AsQueryable();
            if (termId.HasValue)
                paymentsQuery = paymentsQuery.Where(fp => fp.TermId == termId.Value);
            var payments = await paymentsQuery.ToListAsync();

            var termIds = termFees.Select(tf => tf.TermId).Distinct().ToList();
            var terms = await db.Terms.Where(t => termIds.Contains(t.Id)).ToListAsync();
            var termDict = terms.ToDictionary(t => t.Id, t => t.Name);

            var result = new List<AutoTable.Models.DefaulterRecord>();
            foreach (var s in students)
            {
                if (s.ClassId == null) continue;

                var expected = termFees
                    .Where(tf => tf.ClassId == s.ClassId.Value)
                    .Sum(tf => tf.Amount);

                var paid = payments
                    .Where(fp => fp.StudentId == s.Id)
                    .Sum(fp => fp.Amount);

                var balance = (decimal)(expected - paid);

                if (minBalance.HasValue && balance < minBalance.Value) continue;
                if (expected == 0) continue;

                var termName = "All Terms";
                if (termId.HasValue)
                {
                    var tfForTerm = termFees.FirstOrDefault(tf => tf.TermId == termId.Value && tf.ClassId == s.ClassId.Value);
                    if (tfForTerm != null && termDict.TryGetValue(tfForTerm.TermId, out var tn))
                        termName = tn;
                }

                result.Add(new AutoTable.Models.DefaulterRecord
                {
                    StudentId = s.Id,
                    StudentName = s.FullName,
                    AdmissionNumber = s.LIN ?? string.Empty,
                    ClassId = s.ClassId,
                    ClassName = s.Class?.Name ?? string.Empty,
                    ExpectedAmount = (decimal)expected,
                    PaidAmount = (decimal)paid,
                    TermName = termName,
                    DaysSinceEnrollment = (DateTime.UtcNow - s.CreatedAt).Days
                });
            }

            return result.OrderByDescending(d => d.Balance).ToList();
        }        public async Task<IReadOnlyList<AutoTable.Models.StudentCredit>> GetStudentCreditsAsync(int studentId)
        {
            using var db = CreateContext();
            var credits = await db.StudentCredits
                .Include(c => c.FromTerm)
                .Include(c => c.AppliedToTerm)
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return credits.Select(c => new AutoTable.Models.StudentCredit
            {
                Id = c.Id,
                StudentId = c.StudentId,
                FromTermId = c.FromTermId,
                FromTermName = c.FromTerm?.Name ?? string.Empty,
                AppliedToTermId = c.AppliedToTermId,
                AppliedToTermName = c.AppliedToTerm?.Name ?? string.Empty,
                Amount = c.Amount,
                CreatedAt = c.CreatedAt,
                AppliedAt = c.AppliedAt,
                Description = c.Description
            }).ToList();
        }

        public async Task<double> GetAvailableCreditAsync(int studentId, int termId)
        {
            using var db = CreateContext();
            // Sum all unapplied credits for this student that were created in terms
            // BEFORE the given term (carry-forward from earlier terms).
            var term = await db.Terms.FindAsync(termId);
            if (term == null) return 0;

            var available = await db.StudentCredits
                .Where(c => c.StudentId == studentId && !c.AppliedAt.HasValue)
                .ToListAsync();

            return available.Sum(c => c.Amount);
        }

        /// <summary>
        /// Internal helper: after a payment is recorded, check if total paid exceeds
        /// the expected fee for that term. If so, create a credit record for the excess.
        /// </summary>
        private async Task ProcessOverpaymentCreditAsync(AppDbContext db, int studentId, int? termId)
        {
            if (!termId.HasValue) return;

            var student = await db.Students.FindAsync(studentId);
            if (student?.ClassId == null) return;

            // Expected fee for this student's class in this term
            var expected = await db.TermFees
                .Where(tf => tf.ClassId == student.ClassId.Value && tf.TermId == termId.Value)
                .Select(tf => tf.Amount)
                .FirstOrDefaultAsync();

            if (expected <= 0) return;

            // Total paid for this student in this term
            var totalPaid = await db.FeePayments
                .Where(fp => fp.StudentId == studentId && fp.TermId == termId.Value)
                .SumAsync(fp => fp.Amount);

            // Total existing credits for this student (unapplied)
            var existingCredits = await db.StudentCredits
                .Where(c => c.StudentId == studentId && !c.AppliedAt.HasValue)
                .SumAsync(c => c.Amount);

            // Effective paid = payments + credits already applied
            var effectivePaid = totalPaid + existingCredits;

            // Overpayment = effective paid - expected
            var overpayment = effectivePaid - expected;

            // If overpayment > 0 and no existing unapplied credit for this term yet, create one
            if (overpayment > 0)
            {
                var existingCredit = await db.StudentCredits
                    .FirstOrDefaultAsync(c => c.StudentId == studentId && c.FromTermId == termId.Value && !c.AppliedAt.HasValue);

                if (existingCredit == null)
                {
                    db.StudentCredits.Add(new StudentCreditEntity
                    {
                        StudentId = studentId,
                        FromTermId = termId.Value,
                        Amount = overpayment,
                        CreatedAt = DateTime.UtcNow,
                        Description = $"Overpayment carry-forward from Term {termId.Value}"
                    });
                    await db.SaveChangesAsync();
                }
                else
                {
                    // Update existing credit amount if overpayment changed
                    existingCredit.Amount = overpayment;
                    await db.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Internal helper: apply available credits to a term's balance.
        /// When a student has unapplied credits, mark them as applied to the current term.
        /// </summary>
        private async Task ApplyCreditsToTermAsync(AppDbContext db, int studentId, int termId)
        {
            var unapplied = await db.StudentCredits
                .Where(c => c.StudentId == studentId && !c.AppliedAt.HasValue)
                .ToListAsync();

            foreach (var credit in unapplied)
            {
                credit.AppliedToTermId = termId;
                credit.AppliedAt = DateTime.UtcNow;
            }

            if (unapplied.Count > 0)
                await db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<AutoTable.Models.CohortSummary>> GetCohortSummariesAsync(int? termId = null)
        {
            using var db = CreateContext();

            var students = await db.Students
                .Include(s => s.Class)
                .Where(s => s.IsActive)
                .ToListAsync();

            var termFeesQuery = db.TermFees.AsQueryable();
            if (termId.HasValue)
                termFeesQuery = termFeesQuery.Where(tf => tf.TermId == termId.Value);
            var termFees = await termFeesQuery.ToListAsync();

            var paymentsQuery = db.FeePayments.AsQueryable();
            if (termId.HasValue)
                paymentsQuery = paymentsQuery.Where(fp => fp.TermId == termId.Value);
            var payments = await paymentsQuery.ToListAsync();

            // Group students by class
            var classGroups = students
                .Where(s => s.ClassId != null)
                .GroupBy(s => new { s.ClassId, ClassName = s.Class?.Name ?? "Unknown" })
                .ToList();

            var summaries = new List<AutoTable.Models.CohortSummary>();
            decimal schoolExpected = 0, schoolCollected = 0;
            int schoolPaid = 0, schoolPartial = 0, schoolUnpaid = 0, schoolTotal = 0;

            foreach (var grp in classGroups)
            {
                var classIdVal = grp.Key.ClassId!.Value;
                var className = grp.Key.ClassName;
                var classStudents = grp.ToList();

                var expected = termFees
                    .Where(tf => tf.ClassId == classIdVal)
                    .Sum(tf => tf.Amount);

                int paid = 0, partial = 0, unpaid = 0;
                decimal collected = 0;

                foreach (var s in classStudents)
                {
                    var studentPaid = payments
                        .Where(fp => fp.StudentId == s.Id)
                        .Sum(fp => fp.Amount);
                    collected += (decimal)studentPaid;

                    var balance = (decimal)expected - (decimal)studentPaid;
                    if (balance <= 0) paid++;
                    else if (studentPaid > 0) partial++;
                    else unpaid++;
                }

                summaries.Add(new AutoTable.Models.CohortSummary
                {
                    Label = className,
                    TotalStudents = classStudents.Count,
                    PaidCount = paid,
                    PartialCount = partial,
                    UnpaidCount = unpaid,
                    TotalExpected = (decimal)(expected * classStudents.Count),
                    TotalCollected = collected
                });

                schoolExpected += (decimal)(expected * classStudents.Count);
                schoolCollected += collected;
                schoolPaid += paid;
                schoolPartial += partial;
                schoolUnpaid += unpaid;
                schoolTotal += classStudents.Count;
            }

            // Add school-wide summary at the top
            summaries.Insert(0, new AutoTable.Models.CohortSummary
            {
                Label = "School-wide",
                TotalStudents = schoolTotal,
                PaidCount = schoolPaid,
                PartialCount = schoolPartial,
                UnpaidCount = schoolUnpaid,
                TotalExpected = schoolExpected,
                TotalCollected = schoolCollected
            });

            return summaries;
        }

        public async Task<AutoTable.Models.AssessmentItem?> GetAssessmentAsync(string name, string className, string subject)
        {
            using var db = CreateContext();
            var clsName = className?.Trim() ?? string.Empty;
            var subjName = subject?.Trim() ?? string.Empty;
            var assName = name?.Trim() ?? string.Empty;

            var cls = await db.Classes.FirstOrDefaultAsync(c => c.Name.ToLower() == clsName.ToLower());
            var subj = await db.Subjects.FirstOrDefaultAsync(s => s.Name.ToLower() == subjName.ToLower());
            if (cls == null || subj == null) return null;

            // Case-insensitive / trim-tolerant match so a name typed or stored with
            // different casing still resolves instead of failing with "not found".
            var a = await db.Assessments
                .Include(x => x.AuthorUser)
                .FirstOrDefaultAsync(x =>
                x.ClassId == cls.Id && x.SubjectId == subj.Id &&
                x.Name.Trim().ToLower() == assName.ToLower());

            if (a == null) return null;

            return new AutoTable.Models.AssessmentItem
            {
                Id = a.Id.ToString(),
                Name = a.Name,
                ClassName = cls.Name,
                Subject = subj.Name,
                WeightPercent = a.WeightPercent,
                DueDate = a.DueDate ?? DateTime.MinValue,
                MarksEnteredPercent = a.MarksEnteredPercent,
                IsVerified = a.IsVerified,
                IsPublished = a.IsPublished,
                PromotionRole = (AssessmentPromotionRole)a.PromotionRole,
                AuthorId = a.AuthorUserId,
                AuthorName = a.AuthorName ?? a.AuthorUser?.FullName
            };
        }

        public async Task<SimpleLookup> CreateTermAsync(string name, DateTime? startDate = null, DateTime? endDate = null)
        {
            using var db = CreateContext();
            var existing = await db.Terms.FirstOrDefaultAsync(t => t.Name == name);
            if (existing != null)
            {
                // Term already exists - return existing lookup instead of throwing to make UI idempotent
                return new SimpleLookup { Id = existing.Id, Name = existing.Name };
            }

            var t = new TermEntity { Name = name, StartDate = startDate, EndDate = endDate };
            // When creating a new term, make it the active term and deactivate others
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                // Deactivate other terms
                var others = await db.Terms.Where(x => x.IsActive).ToListAsync();
                foreach (var o in others)
                {
                    o.IsActive = false;
                    db.Terms.Update(o);
                }

                t.IsActive = true;
                db.Terms.Add(t);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return new SimpleLookup { Id = t.Id, Name = t.Name };
        }

        public async Task<SimpleLookup?> UpdateTermAsync(int termId, string name, DateTime? startDate = null, DateTime? endDate = null)
        {
            using var db = CreateContext();
            var t = await db.Terms.FindAsync(termId);
            if (t == null) return null;
            t.Name = name;
            t.StartDate = startDate;
            t.EndDate = endDate;
            // If the term end date is in the past, mark it inactive
            if (t.EndDate.HasValue && t.EndDate.Value < DateTime.UtcNow)
                t.IsActive = false;

            db.Terms.Update(t);
            await db.SaveChangesAsync();
            return new SimpleLookup { Id = t.Id, Name = t.Name };
        }

        public async Task DeleteTermAsync(int termId)
        {
            using var db = CreateContext();
            var t = await db.Terms.FindAsync(termId);
            if (t == null) return;
            // Prevent deletion if assessments exist for term
            var hasAssessments = await db.Assessments.AnyAsync(a => a.TermId == termId);
            if (hasAssessments) throw new InvalidOperationException("Cannot delete term with existing assessments.");
            db.Terms.Remove(t);
            await db.SaveChangesAsync();
        }

        private AppDbContext CreateContext() => new AppDbContext(_options);

        public async Task<IReadOnlyList<AssessmentItem>> GetAssessmentsAsync()
        {
            using var db = CreateContext();
            var list = await db.Assessments
                .Include(a => a.Class)
                .Include(a => a.Subject)
                .Include(a => a.AuthorUser)
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
                IsPublished = a.IsPublished,
                PromotionRole = (AssessmentPromotionRole)a.PromotionRole,
                AuthorId = a.AuthorUserId,
                AuthorName = a.AuthorName ?? a.AuthorUser?.FullName
            }).ToList();
        }

        public async Task<IReadOnlyList<GradebookRow>> GetGradebookAsync(string className, string subject, string? academicYear = null, string? term = null, string? stream = null, string? studentName = null)
        {
            using var db = CreateContext();
            var cls = await db.Classes.Include(c => c.GradingSystem).FirstOrDefaultAsync(c => c.Name == className) ?? db.Classes.FirstOrDefault();
            var subj = await db.Subjects.FirstOrDefaultAsync(s => s.Name == subject) ?? db.Subjects.FirstOrDefault();

            if (cls == null || subj == null) return new List<GradebookRow>();

            // Resolve grading system for this class.
            var gradingSystem = cls.GradingSystem;
            if (gradingSystem == null)
                gradingSystem = await db.GradingSystems.FirstOrDefaultAsync(g => g.IsDefault)
                    ?? await db.GradingSystems.OrderBy(g => g.Name).FirstOrDefaultAsync();
            var passMark = gradingSystem?.PassMark ?? 50;
            var bands = gradingSystem != null
                ? await db.GradeBands.Where(b => b.GradingSystemId == gradingSystem.Id).OrderBy(b => b.MinScore).ToListAsync()
                : new List<GradeBandEntity>();
            var bandInfos = bands.Select(b => new AutoTable.Models.GradeBandInfo
            {
                Id = b.Id, Label = b.Label,
                MinScore = b.MinScore, MaxScore = b.MaxScore,
                IsPromotionalPass = b.IsPromotionalPass,
                IsRepeater = b.IsRepeater,
                IsPromotionalFail = b.IsPromotionalFail
            }).ToList();

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
                        Grade = avg > 0 ? GradeFromBands(avg, bandInfos) : "-",
                        Status = avg < passMark ? "At Risk" : avg >= 70 ? "Excellent" : "On Track"
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
            // Trim + case-insensitive resolution; NO silent fallback to an arbitrary
            // class/subject (the old FirstOrDefault fallback made existing marks vanish).
            var clsName = className?.Trim() ?? string.Empty;
            var subjName = subject?.Trim() ?? string.Empty;
            var assName = assessmentName?.Trim() ?? string.Empty;

            var cls = await db.Classes.FirstOrDefaultAsync(c => c.Name.ToLower() == clsName.ToLower());
            var subj = await db.Subjects.FirstOrDefaultAsync(s => s.Name.ToLower() == subjName.ToLower());
            if (cls == null || subj == null || clsName.Length == 0 || subjName.Length == 0)
                return new List<StudentMarkRow>();

            var clsId = cls.Id;
            var subjId = subj.Id;
            var assess = await db.Assessments.FirstOrDefaultAsync(a =>
                a.ClassId == clsId && a.SubjectId == subjId &&
                a.Name.Trim().ToLower() == assName.ToLower());

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

            // Determine eligible students based on assessment scope
            List<StudentEntity> students;
            if (assess.IsClassWide || assess.StreamId == null)
            {
                students = await db.Students.Where(s => s.ClassId == cls.Id && s.IsActive).ToListAsync();
            }
            else
            {
                students = await db.Students.Where(s => s.ClassId == cls.Id && s.StreamId == assess.StreamId && s.IsActive).ToListAsync();
            }
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

        public async Task<IReadOnlyList<SimpleLookup>> GetTermLookupsAsync()
        {
            using var db = CreateContext();
            var list = await db.Terms.OrderBy(t => t.Name).ToListAsync();
            return list.Select(t => new SimpleLookup { Id = t.Id, Name = t.Name }).ToList();
        }

        public async Task<SimpleLookup?> GetActiveTermAsync()
        {
            using var db = CreateContext();
            var t = await db.Terms.FirstOrDefaultAsync(x => x.IsActive);
            return t == null ? null : new SimpleLookup { Id = t.Id, Name = t.Name };
        }

        public async Task<IReadOnlyList<string>> GetAcademicYearsAsync()
        {
            using var db = CreateContext();
            return await db.AcademicYears.OrderBy(y => y.Name).Select(y => y.Name).ToListAsync();
        }

        public async Task<SimpleLookup> CreateAcademicYearAsync(string name)
        {
            using var db = CreateContext();
            var existing = await db.AcademicYears.FirstOrDefaultAsync(y => y.Name == name);
            if (existing != null)
                return new SimpleLookup { Id = existing.Id, Name = existing.Name };

            var y = new AcademicYearEntity { Name = name };
            db.AcademicYears.Add(y);
            await db.SaveChangesAsync();
            return new SimpleLookup { Id = y.Id, Name = y.Name };
        }

        public async Task<IReadOnlyList<string>> GetStreamsAsync()
        {
            using var db = CreateContext();
            return await db.Streams.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync();
        }

        public async Task<IReadOnlyList<SimpleLookup>> GetStreamsForClassAsync(int classId)
        {
            using var db = CreateContext();
            var list = await db.ClassStreams
                .Where(cs => cs.ClassId == classId)
                .Include(cs => cs.Stream)
                .Select(cs => cs.Stream!)
                .Where(s => s != null)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return list.Select(s => new SimpleLookup { Id = s.Id, Name = s.Name }).ToList();
        }

        public async Task<SimpleLookup> CreateStreamAsync(string name)
        {
            using var db = CreateContext();
            var existing = await db.Streams.FirstOrDefaultAsync(s => s.Name == name);
            if (existing != null)
                return new SimpleLookup { Id = existing.Id, Name = existing.Name };

            var entity = new StreamEntity { Name = name };
            db.Streams.Add(entity);
            await db.SaveChangesAsync();
            return new SimpleLookup { Id = entity.Id, Name = entity.Name };
        }

        public async Task<SimpleLookup> CreateStreamAsync(string name, int? classId = null)
        {
            var lookup = await CreateStreamAsync(name);
            if (classId.HasValue)
            {
                // assign to class if requested
                await AssignStreamToClassAsync(classId.Value, lookup.Id);
            }
            return lookup;
        }

        public async Task<IReadOnlyList<SimpleLookup>> GetAllStreamsAsync()
        {
            using var db = CreateContext();
            var list = await db.Streams.OrderBy(s => s.Name).ToListAsync();
            return list.Select(s => new SimpleLookup { Id = s.Id, Name = s.Name }).ToList();
        }

        public async Task AssignStreamToClassAsync(int classId, int streamId, int? streamTeacherId = null)
        {
            using var db = CreateContext();
            if (!await db.Classes.AnyAsync(c => c.Id == classId)) throw new InvalidOperationException("Class not found.");
            if (!await db.Streams.AnyAsync(s => s.Id == streamId)) throw new InvalidOperationException("Stream not found.");
            var existing = await db.ClassStreams.FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.StreamId == streamId);
            if (existing != null)
            {
                existing.StreamTeacherId = streamTeacherId;
                await db.SaveChangesAsync();
                return;
            }
            db.ClassStreams.Add(new ClassStreamEntity { ClassId = classId, StreamId = streamId, StreamTeacherId = streamTeacherId });
            await db.SaveChangesAsync();
        }

        public async Task RemoveStreamFromClassAsync(int classId, int streamId)
        {
            using var db = CreateContext();
            var cs = await db.ClassStreams.FindAsync(classId, streamId);
            if (cs == null) return;
            db.ClassStreams.Remove(cs);
            await db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<TermFee>> GetTermFeesAsync()
        {
            using var db = CreateContext();
            var list = await db.TermFees
                .Include(tf => tf.Term)
                .Include(tf => tf.Class)
                .ToListAsync();

            return list.Select(tf => new TermFee
            {
                Id = tf.Id,
                TermId = tf.TermId,
                TermName = tf.Term?.Name ?? string.Empty,
                ClassId = tf.ClassId,
                ClassName = tf.Class?.Name ?? string.Empty,
                Amount = tf.Amount
            }).ToList();
        }

        public async Task<TermFee> SetTermFeeAsync(int termId, int classId, double amount)
        {
            using var db = CreateContext();
            var existing = await db.TermFees.FirstOrDefaultAsync(t => t.TermId == termId && t.ClassId == classId);
            if (existing == null)
            {
                existing = new TermFeeEntity { TermId = termId, ClassId = classId, Amount = amount };
                db.TermFees.Add(existing);
            }
            else
            {
                existing.Amount = amount;
                db.TermFees.Update(existing);
            }

            await db.SaveChangesAsync();

            var term = await db.Terms.FindAsync(termId);
            var cls = await db.Classes.FindAsync(classId);
            return new TermFee
            {
                Id = existing.Id,
                TermId = existing.TermId,
                TermName = term?.Name ?? string.Empty,
                ClassId = existing.ClassId,
                ClassName = cls?.Name ?? string.Empty,
                Amount = existing.Amount
            };
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

            // Resolve grading system for the student's class.
            var gradingSystem = student?.ClassId != null
                ? (await db.Classes.Include(c => c.GradingSystem).FirstOrDefaultAsync(c => c.Id == student.ClassId))?.GradingSystem
                : null;
            if (gradingSystem == null)
                gradingSystem = await db.GradingSystems.FirstOrDefaultAsync(g => g.IsDefault)
                    ?? await db.GradingSystems.OrderBy(g => g.Name).FirstOrDefaultAsync();
            var passMark = gradingSystem?.PassMark ?? 50;
            var bands = gradingSystem != null
                ? await db.GradeBands.Where(b => b.GradingSystemId == gradingSystem.Id).OrderBy(b => b.MinScore).ToListAsync()
                : new List<GradeBandEntity>();
            var bandInfos = bands.Select(b => new AutoTable.Models.GradeBandInfo
            {
                Id = b.Id, Label = b.Label,
                MinScore = b.MinScore, MaxScore = b.MaxScore,
                IsPromotionalPass = b.IsPromotionalPass,
                IsRepeater = b.IsRepeater,
                IsPromotionalFail = b.IsPromotionalFail
            }).ToList();

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
                    Grade = avg > 0 ? GradeFromBands(avg, bandInfos) : "-",
                    Status = avg < passMark ? "At Risk" : avg >= 70 ? "Excellent" : "On Track"
                });
            }

            if (detail.SubjectPerformances.Count == 0)
            {
                // No marks available for selected filters â€” show a placeholder row for the subject
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
            detail.OverallGrade = detail.OverallAverage > 0 ? GradeFromBands(detail.OverallAverage, bandInfos) : "-";
            detail.Status = detail.OverallAverage < passMark ? "At Risk" : detail.OverallAverage >= 70 ? "Excellent" : detail.OverallAverage > 0 ? "On Track" : "No Data";

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

        public async Task<Models.ReportCardSheetModel?> GetReportCardSheetAsync(
            string studentName, string className, string term)
        {
            using var db = CreateContext();

            var clsName = (className ?? string.Empty).Trim().ToLower();
            var cls = await db.Classes
                .Include(c => c.ClassTeacher)
                .Include(c => c.GradingSystem)
                .FirstOrDefaultAsync(c => c.Name.Trim().ToLower() == clsName);
            var termEntity = await db.Terms.FirstOrDefaultAsync(t => t.Name == term);
            if (cls == null || termEntity == null) return null;

            // Resolve the grading system: class-specific → school default → hard-coded fallback.
            var gradingSystem = cls.GradingSystem;
            if (gradingSystem == null)
                gradingSystem = await db.GradingSystems.FirstOrDefaultAsync(g => g.IsDefault)
                    ?? await db.GradingSystems.OrderBy(g => g.Name).FirstOrDefaultAsync();
            var passMark = gradingSystem?.PassMark ?? 50;
            var bands = gradingSystem != null
                ? await db.GradeBands.Where(b => b.GradingSystemId == gradingSystem.Id).OrderBy(b => b.MinScore).ToListAsync()
                : new List<GradeBandEntity>();
            var bandInfos = bands.Select(b => new AutoTable.Models.GradeBandInfo
            {
                Id = b.Id, Label = b.Label,
                MinScore = b.MinScore, MaxScore = b.MaxScore,
                IsPromotionalPass = b.IsPromotionalPass,
                IsRepeater = b.IsRepeater,
                IsPromotionalFail = b.IsPromotionalFail
            }).ToList();

            var student = await db.Students
                .Include(s => s.Class)
                .Include(s => s.Stream)
                .FirstOrDefaultAsync(s => s.FullName == studentName && s.IsActive);
            if (student == null) return null;

            var assessments = await db.Assessments
                .Include(a => a.Subject)
                .Where(a => a.ClassId == cls.Id && a.TermId == termEntity.Id)
                .OrderBy(a => a.Subject!.Name).ThenByDescending(a => a.DueDate)
                .ToListAsync();

            var marks = await db.Marks
                .Where(m => m.StudentId == student.Id)
                .Where(m => assessments.Select(a => a.Id).Contains(m.AssessmentId))
                .ToListAsync();

            // Use the explicit PromotionRole on each assessment to classify:
            //   PromotionExam → promotional, CountsTowardPromotion → contributory,
            //   None → contributory (does not count toward promotion).
            // Fallback: if no assessment has a PromotionRole set (legacy data),
            // fall back to the old heuristic (highest-weighted = promotional).
            var promotional = new List<ReportCardAssessmentRow>();
            var contributory = new List<ReportCardAssessmentRow>();
            bool hasAnyRoleSet = assessments.Any(a => a.PromotionRole != 0);

            foreach (var subjectGroup in assessments.GroupBy(a => a.Subject?.Name ?? "General").OrderBy(g => g.Key))
            {
                if (hasAnyRoleSet)
                {
                    // Explicit classification based on PromotionRole
                    foreach (var a in subjectGroup)
                    {
                        var mark = marks.FirstOrDefault(m => m.AssessmentId == a.Id)?.Mark;
                        var avg = mark ?? 0;
                        var grade = avg > 0 ? GradeFromBands(avg, bandInfos) : "-";
                        var isPromo = a.PromotionRole == (int)AssessmentPromotionRole.PromotionExam;
                        var row = new ReportCardAssessmentRow
                        {
                            Subject = subjectGroup.Key,
                            AssessmentName = a.Name,
                            Mark = Math.Round(avg, 1),
                            Grade = grade,
                            WeightPercent = a.WeightPercent,
                            IsPromotional = isPromo,
                            IsPass = CheckPromotionalPass(avg, passMark, bandInfos)
                        };
                        if (isPromo) promotional.Add(row); else contributory.Add(row);
                    }
                }
                else
                {
                    // Legacy fallback: highest-weighted / last-due = promotional
                    var ordered = subjectGroup.OrderByDescending(a => a.WeightPercent).ThenByDescending(a => a.DueDate).ToList();
                    for (int i = 0; i < ordered.Count; i++)
                    {
                        var a = ordered[i];
                        var mark = marks.FirstOrDefault(m => m.AssessmentId == a.Id)?.Mark;
                        var avg = mark ?? 0;
                        var grade = avg > 0 ? GradeFromBands(avg, bandInfos) : "-";
                        var row = new ReportCardAssessmentRow
                        {
                            Subject = subjectGroup.Key,
                            AssessmentName = a.Name,
                            Mark = Math.Round(avg, 1),
                            Grade = grade,
                            WeightPercent = a.WeightPercent,
                            IsPromotional = i == 0,
                            IsPass = CheckPromotionalPass(avg, passMark, bandInfos)
                        };
                        if (i == 0) promotional.Add(row); else contributory.Add(row);
                    }
                }
            }

            var allRows = promotional.Concat(contributory).ToList();
            var overallAverage = allRows.Count > 0
                ? Math.Round(allRows.Average(r => r.Mark), 1)
                : 0;
            var overallGrade = overallAverage > 0 ? GradeFromBands(overallAverage, bandInfos) : "-";
            var status = overallAverage < passMark ? "At Risk" : overallAverage >= 70 ? "Excellent" : overallAverage > 0 ? "On Track" : "No Data";

// Rank within class using overall averages of all students in the class.
            var classStudents = await db.Students.Where(s => s.ClassId == cls.Id && s.IsActive).Select(s => s.Id).ToListAsync();
            var rank = 0;
            if (classStudents.Count > 0)
            {
                var classMarks = await db.Marks
                    .Where(m => classStudents.Contains(m.StudentId))
                    .Where(m => assessments.Select(a => a.Id).Contains(m.AssessmentId))
                    .ToListAsync();
                var avgs = classMarks.GroupBy(m => m.StudentId)
                    .Select(g => g.Average(m => m.Mark ?? 0))
                    .OrderByDescending(v => v).ToList();
                rank = avgs.Count == 0 ? 0 : avgs.TakeWhile(v => v > overallAverage).Count() + 1;
            }

            var teacherComment = BuildTeacherComment(overallAverage, status, promotional);

            return new Models.ReportCardSheetModel
            {
                SchoolName = "AutoTable Academy",
                StudentName = student.FullName,
                AdmissionNumber = student.LIN,
                ClassName = cls.Name,
                Stream = student.Stream?.Name ?? string.Empty,
                Term = termEntity.Name,
                AcademicYear = termEntity.Name,
                Gender = student.Gender ?? string.Empty,
                DateOfBirth = student.DateOfBirth?.ToString("dd MMM yyyy") ?? string.Empty,
                GuardianName = student.GuardianName ?? string.Empty,
                GuardianPhone = student.GuardianPhone ?? string.Empty,
                ClassTeacher = cls.ClassTeacher?.FullName ?? string.Empty,
                GradingSystemName = gradingSystem?.Name ?? string.Empty,
                PassMark = passMark,
                PromotionalAssessments = promotional,
                ContributoryAssessments = contributory,
                OverallAverage = overallAverage,
                OverallGrade = overallGrade,
                Rank = rank,
                Status = status,
                TeacherComment = teacherComment
            };
        }

        private static string BuildTeacherComment(double average, string status, List<ReportCardAssessmentRow> promotional)
        {
            var passed = promotional.Count(r => r.IsPass);
            var total = promotional.Count;
            var verdict = total > 0 ? $"{passed}/{total} promotional papers passed" : "No promotional paper on record";
            var core = status switch
            {
                "Excellent" => "A very strong term. ",
                "On Track" => "Good progress; keep the momentum. ",
                "At Risk" => "Performance is below expectation and needs urgent attention. ",
                _ => "No results recorded for this term yet. "
            };
            var strongest = promotional.OrderByDescending(r => r.Mark).FirstOrDefault();
            var weakest = promotional.OrderByDescending(r => r.Mark).LastOrDefault();
            var detail = string.Empty;
            if (strongest != null && strongest.Mark > 0)
                detail = $"Strongest: {strongest.Subject} ({strongest.Mark:F0}%). ";
            if (weakest != null && weakest.Mark > 0 && weakest.Mark < 50)
                detail += $"Needs extra help in {weakest.Subject} ({weakest.Mark:F0}%). ";
            return $"{core}{verdict}. {detail}Recommended next step: {(average >= 50 ? "promote to the next class." : "repeat the class.")}";
        }

        /// <summary>
        /// Returns ALL active students in a class with their overall average across all subjects
        /// for a given term. Used by the Report Cards list view.
        /// </summary>
        public async Task<IReadOnlyList<Models.ReportCardRow>> GetReportCardListAsync(
            string className, string? term, string? stream)
        {
            using var db = CreateContext();
            var clsName = (className ?? string.Empty).Trim().ToLower();
            var cls = await db.Classes
                .Include(c => c.GradingSystem)
                .FirstOrDefaultAsync(c => c.Name.Trim().ToLower() == clsName);
            if (cls == null) return new List<Models.ReportCardRow>();

            // Resolve grading system
            var gradingSystem = cls.GradingSystem;
            if (gradingSystem == null)
                gradingSystem = await db.GradingSystems.FirstOrDefaultAsync(g => g.IsDefault)
                    ?? await db.GradingSystems.OrderBy(g => g.Name).FirstOrDefaultAsync();
            var passMark = gradingSystem?.PassMark ?? 50;
            var bands = gradingSystem != null
                ? await db.GradeBands.Where(b => b.GradingSystemId == gradingSystem.Id).OrderBy(b => b.MinScore).ToListAsync()
                : new List<GradeBandEntity>();
            var bandInfos = bands.Select(b => new AutoTable.Models.GradeBandInfo
            {
                Id = b.Id, Label = b.Label,
                MinScore = b.MinScore, MaxScore = b.MaxScore,
                IsPromotionalPass = b.IsPromotionalPass,
                IsRepeater = b.IsRepeater,
                IsPromotionalFail = b.IsPromotionalFail
            }).ToList();

            // Get all active students in the class
            var studentsQuery = db.Students
                .Include(s => s.Stream)
                .Where(s => s.ClassId == cls.Id && s.IsActive);

            if (!string.IsNullOrWhiteSpace(stream))
            {
                var streamLower = stream.Trim().ToLower();
                studentsQuery = studentsQuery.Where(s => s.Stream != null && s.Stream.Name.ToLower() == streamLower);
            }

            var students = await studentsQuery.OrderBy(s => s.FullName).ToListAsync();

            // Get all assessments for this class + term
            var assessmentsQuery = db.Assessments
                .Where(a => a.ClassId == cls.Id);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var termEntity = await db.Terms.FirstOrDefaultAsync(t => t.Name == term);
                if (termEntity != null)
                    assessmentsQuery = assessmentsQuery.Where(a => a.TermId == termEntity.Id);
            }

            var assessmentIds = await assessmentsQuery.Select(a => a.Id).ToListAsync();

            // Get all marks for these students + assessments
            var studentIds = students.Select(s => s.Id).ToList();
            var marks = await db.Marks
                .Where(m => studentIds.Contains(m.StudentId) && assessmentIds.Contains(m.AssessmentId))
                .ToListAsync();

            // Build report card rows
            var rows = students.Select(s =>
            {
                var studentMarks = marks.Where(m => m.StudentId == s.Id).ToList();
                var scores = studentMarks.Select(x => x.Mark ?? 0).ToList();
                var avg = scores.Count > 0 ? Math.Round(scores.Average(), 1) : 0;
                var grade = avg > 0 ? GradeFromBands(avg, bandInfos) : "-";
                var status = avg == 0 ? "No Data" : avg < passMark ? "At Risk" : avg >= 70 ? "Excellent" : "On Track";
                return new Models.ReportCardRow
                {
                    StudentName = s.FullName,
                    AdmissionNumber = s.LIN ?? string.Empty,
                    ClassName = cls.Name,
                    Average = avg,
                    Status = status
                };
            }).OrderByDescending(r => r.Average).ToList();

            for (int i = 0; i < rows.Count; i++)
                rows[i].Rank = i + 1;

            return rows;
        }

        public async Task<IReadOnlyList<Student>> GetStudentsAsync()
        {
            using var db = CreateContext();
            var list = await db.Students.Include(s => s.Class).Include(s => s.Stream).OrderByDescending(s => s.CreatedAt).ToListAsync();
            return list.Select(s => new Student
            {
                Id = s.Id,
                LIN = s.LIN,
                FullName = s.FullName,
                    AdmissionNumber = s.LIN,
                ClassId = s.ClassId,
                StreamId = s.StreamId,
                ClassName = s.Class?.Name,
                StreamName = s.Stream?.Name,
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
            // Block duplicate LIN at the staging level too — prevents orphaned enrollment rows
            if (!string.IsNullOrWhiteSpace(enrollment.LIN) &&
                await db.Students.AnyAsync(s => s.LIN == enrollment.LIN && s.IsActive))
            {
                throw new InvalidOperationException($"LIN '{enrollment.LIN}' is already assigned to another student.");
            }
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

        public async Task<bool> IsLinTakenAsync(string lin)
        {
            if (string.IsNullOrWhiteSpace(lin)) return false;
            using var db = CreateContext();
            return await db.Students.AnyAsync(s => s.LIN == lin && s.IsActive);
        }

        // --- Class & Subject management ---
        public async Task<IReadOnlyList<AutoTable.Models.SimpleLookup>> GetClassesAsync()
        {
            using var db = CreateContext();
            var list = await db.Classes.OrderBy(c => c.Name).ToListAsync();
            return list.Select(c => new AutoTable.Models.SimpleLookup { Id = c.Id, Name = c.Name }).ToList();
        }

        public async Task<AutoTable.Models.SimpleLookup> CreateClassAsync(string name, int? classTeacherId = null, int? gradingSystemId = null)
        {
            using var db = CreateContext();
            if (await db.Classes.AnyAsync(c => c.Name == name))
                throw new InvalidOperationException("Class already exists.");

            // Validate the class teacher when provided: any teacher qualifies
            // (registered teachers AND student teachers are both allowed).
            if (classTeacherId.HasValue)
            {
                var teacher = await db.Users.FindAsync(classTeacherId.Value);
                if (teacher == null || teacher.Role != "Teacher")
                    throw new InvalidOperationException("Selected class teacher was not found among teachers.");
            }

            // Validate the grading system when provided.
            if (gradingSystemId.HasValue && !await db.GradingSystems.AnyAsync(g => g.Id == gradingSystemId.Value))
                throw new InvalidOperationException("Selected grading system was not found.");

            var c = new ClassEntity { Name = name, ClassTeacherId = classTeacherId, GradingSystemId = gradingSystemId };
            db.Classes.Add(c);
            await db.SaveChangesAsync();
            return new AutoTable.Models.SimpleLookup { Id = c.Id, Name = c.Name };
        }

        public async Task UpdateClassAsync(int classId, string name, int? classTeacherId, int? gradingSystemId)
        {
            using var db = CreateContext();
            var c = await db.Classes.FindAsync(classId);
            if (c == null) throw new InvalidOperationException("Class not found.");

            // Validate name uniqueness (excluding this class)
            if (!string.IsNullOrWhiteSpace(name) && !string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                if (await db.Classes.AnyAsync(x => x.Id != classId && x.Name == name))
                    throw new InvalidOperationException("Another class with that name already exists.");
                c.Name = name.Trim();
            }

            // Validate and set class teacher
            if (classTeacherId.HasValue)
            {
                var teacher = await db.Users.FindAsync(classTeacherId.Value);
                if (teacher == null || teacher.Role != "Teacher")
                    throw new InvalidOperationException("Selected class teacher was not found among teachers.");
            }
            c.ClassTeacherId = classTeacherId;

            // Validate and set grading system
            if (gradingSystemId.HasValue && !await db.GradingSystems.AnyAsync(g => g.Id == gradingSystemId.Value))
                throw new InvalidOperationException("Selected grading system was not found.");
            c.GradingSystemId = gradingSystemId;

            await db.SaveChangesAsync();
        }

        public async Task<IReadOnlyDictionary<int, string>> GetClassGradingSystemNamesAsync()
        {
            using var db = CreateContext();
            var list = await db.Classes
                .Where(c => c.GradingSystemId != null)
                .Select(c => new { c.Id, GsName = c.GradingSystem != null ? c.GradingSystem.Name : string.Empty })
                .ToListAsync();
            return list.ToDictionary(x => x.Id, x => x.GsName);
        }

        public async Task<IReadOnlyDictionary<int, string>> GetClassTeacherNamesAsync()
        {
            using var db = CreateContext();
            var list = await db.Classes
                .Where(c => c.ClassTeacherId != null)
                .Select(c => new { c.Id, TeacherName = c.ClassTeacher != null ? c.ClassTeacher.FullName : string.Empty })
                .ToListAsync();
            return list.ToDictionary(x => x.Id, x => x.TeacherName);
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

        // â”€â”€ Grading systems â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public async Task<IReadOnlyList<AutoTable.Models.GradingSystemInfo>> GetGradingSystemsAsync()
        {
            using var db = CreateContext();
            var systems = await db.GradingSystems.OrderBy(g => g.Name).ToListAsync();
            var bands = await db.GradeBands.OrderBy(b => b.MinScore).ToListAsync();

            var result = new List<AutoTable.Models.GradingSystemInfo>();
            foreach (var g in systems)
            {
                var info = new AutoTable.Models.GradingSystemInfo { Id = g.Id, Name = g.Name, IsDefault = g.IsDefault, PassMark = g.PassMark };
                foreach (var b in bands.Where(b => b.GradingSystemId == g.Id))
                {
                    info.Bands.Add(new AutoTable.Models.GradeBandInfo
                    {
                        Id = b.Id,
                        Label = b.Label,
                        MinScore = b.MinScore,
                        MaxScore = b.MaxScore,
                        IsPromotionalPass = b.IsPromotionalPass,
                        IsRepeater = b.IsRepeater,
                        IsPromotionalFail = b.IsPromotionalFail
                    });
                }
                result.Add(info);
            }
            return result;
        }

        public async Task<AutoTable.Models.GradingSystemInfo> CreateGradingSystemAsync(string name, bool isDefault = false, double passMark = 50)
        {
            using var db = CreateContext();
            if (await db.GradingSystems.AnyAsync(g => g.Name == name))
                throw new InvalidOperationException("A grading system with that name already exists.");

            // Only one school default at a time.
            if (isDefault)
            {
                var defaults = await db.GradingSystems.Where(g => g.IsDefault).ToListAsync();
                foreach (var d in defaults) d.IsDefault = false;
            }

            var g = new GradingSystemEntity { Name = name, IsDefault = isDefault, PassMark = passMark };
            db.GradingSystems.Add(g);
            await db.SaveChangesAsync();
            return new AutoTable.Models.GradingSystemInfo { Id = g.Id, Name = g.Name, IsDefault = g.IsDefault, PassMark = g.PassMark };
        }

        public async Task DeleteGradingSystemAsync(int gradingSystemId)
        {
            using var db = CreateContext();
            var g = await db.GradingSystems.FindAsync(gradingSystemId);
            if (g == null) return;
            // Detach any classes using this system before removing it.
            var classes = await db.Classes.Where(c => c.GradingSystemId == gradingSystemId).ToListAsync();
            foreach (var c in classes) c.GradingSystemId = null;
            var bands = db.GradeBands.Where(b => b.GradingSystemId == gradingSystemId);
            db.GradeBands.RemoveRange(bands);
            db.GradingSystems.Remove(g);
            await db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<AutoTable.Models.GradeBandInfo>> GetGradeBandsAsync(int gradingSystemId)
        {
            using var db = CreateContext();
            var bands = await db.GradeBands
                .Where(b => b.GradingSystemId == gradingSystemId)
                .OrderBy(b => b.MinScore)
                .ToListAsync();
            return bands.Select(b => new AutoTable.Models.GradeBandInfo
            {
                Id = b.Id,
                Label = b.Label,
                MinScore = b.MinScore,
                MaxScore = b.MaxScore,
                IsPromotionalPass = b.IsPromotionalPass,
                IsRepeater = b.IsRepeater,
                IsPromotionalFail = b.IsPromotionalFail
            }).ToList();
        }

        public async Task CreateGradeBandAsync(int gradingSystemId, string label, double minScore, double maxScore,
            bool isPromotionalPass, bool isRepeater, bool isPromotionalFail)
        {
            using var db = CreateContext();
            if (!await db.GradingSystems.AnyAsync(g => g.Id == gradingSystemId))
                throw new InvalidOperationException("Grading system was not found.");
            db.GradeBands.Add(new GradeBandEntity
            {
                GradingSystemId = gradingSystemId,
                Label = label,
                MinScore = minScore,
                MaxScore = maxScore,
                IsPromotionalPass = isPromotionalPass,
                IsRepeater = isRepeater,
                IsPromotionalFail = isPromotionalFail
            });
            await db.SaveChangesAsync();
        }

        public async Task<AutoTable.Models.GradingSystemInfo?> GetClassGradingSystemAsync(int classId)
        {
            using var db = CreateContext();
            var cls = await db.Classes.FindAsync(classId);
            if (cls == null) return null;

            // Try the class-specific system first; fall back to school default.
            GradingSystemEntity? gs = null;
            if (cls.GradingSystemId != null)
                gs = await db.GradingSystems.FindAsync(cls.GradingSystemId);
            gs ??= await db.GradingSystems.FirstOrDefaultAsync(g => g.IsDefault)
                   ?? await db.GradingSystems.OrderBy(g => g.Name).FirstOrDefaultAsync();
            if (gs == null) return null;

            var bands = await db.GradeBands
                .Where(b => b.GradingSystemId == gs.Id)
                .OrderBy(b => b.MinScore)
                .ToListAsync();

            var info = new AutoTable.Models.GradingSystemInfo
            {
                Id = gs.Id, Name = gs.Name, IsDefault = gs.IsDefault, PassMark = gs.PassMark
            };
            foreach (var b in bands)
            {
                info.Bands.Add(new AutoTable.Models.GradeBandInfo
                {
                    Id = b.Id, Label = b.Label,
                    MinScore = b.MinScore, MaxScore = b.MaxScore,
                    IsPromotionalPass = b.IsPromotionalPass,
                    IsRepeater = b.IsRepeater,
                    IsPromotionalFail = b.IsPromotionalFail
                });
            }
            return info;
        }

        /// <summary>
        /// Resolve a percentage mark to a grade label using the class's grading system bands.
        /// Falls back to the hard-coded GradeFromAverage when no bands are defined.
        /// </summary>
        private static string GradeFromBands(double mark, IReadOnlyList<AutoTable.Models.GradeBandInfo> bands)
        {
            if (bands.Count == 0) return GradeFromAverage(mark);
            // Bands are ordered by MinScore ascending; find the first one that covers this mark.
            var band = bands.FirstOrDefault(b => mark >= b.MinScore && mark <= b.MaxScore);
            return band?.Label ?? GradeFromAverage(mark);
        }
        /// <summary>
        /// Determine pass/repeat/fail for a promotional mark using the class's grading system bands.
        /// Returns true when the mark falls in a band flagged IsPromotionalPass.
        /// Falls back to mark >= passMark when no bands are defined.
        /// </summary>
        private static bool CheckPromotionalPass(double mark, double passMark, IReadOnlyList<AutoTable.Models.GradeBandInfo> bands)
        {
            if (bands.Count > 0)
            {
                var band = bands.FirstOrDefault(b => mark >= b.MinScore && mark <= b.MaxScore);
                if (band != null) return band.IsPromotionalPass;
            }
            return mark >= passMark;
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

        public async Task<AutoTable.Models.AssessmentItem> CreateAssessmentAsync(AutoTable.Models.AssessmentItem item)
        {
            using var db = CreateContext();
            // resolve class and subject
            var cls = await db.Classes.FirstOrDefaultAsync(c => c.Name == item.ClassName) ?? await db.Classes.FirstOrDefaultAsync();
            var subj = await db.Subjects.FirstOrDefaultAsync(s => s.Name == item.Subject) ?? await db.Subjects.FirstOrDefaultAsync();

            // Prefer the active term; fall back to the most recent term by start date.
            var term = await db.Terms.FirstOrDefaultAsync(t => t.IsActive)
                ?? await db.Terms.OrderByDescending(t => t.StartDate).FirstOrDefaultAsync();

            // Academic year: reuse an existing one; auto-create one from the due-date year if none exists.
            var ay = await db.AcademicYears.FirstOrDefaultAsync();
            if (ay == null)
            {
                var year = item.DueDate == default ? DateTime.UtcNow.Year : item.DueDate.Year;
                ay = new AcademicYearEntity { Name = $"{year}-{year + 1}" };
                db.AcademicYears.Add(ay);
                await db.SaveChangesAsync();
            }

            // Report precisely which prerequisite is missing so the UI can guide the user.
            if (cls == null)
                throw new InvalidOperationException("No class exists yet. Create a class under Administration â†’ Classes Management first.");
            if (subj == null)
                throw new InvalidOperationException("No subject exists yet. Create a subject under Administration â†’ Classes Management first.");
            if (term == null)
                throw new InvalidOperationException("No term exists yet. Create a term under Administration â†’ Term Management first.");

            var entity = new AssessmentEntity
            {
                Name = item.Name,
                ClassId = cls.Id,
                SubjectId = subj.Id,
                AcademicYearId = ay.Id,
                TermId = term.Id,
                WeightPercent = item.WeightPercent,
                DueDate = item.DueDate,
                StreamId = item.StreamId,
                IsClassWide = item.IsClassWide,
                IsVerified = false,
                IsPublished = false,
                MarksEnteredPercent = 0,
                PromotionRole = (int)item.PromotionRole,
                AuthorUserId = item.AuthorId,
                AuthorName = item.AuthorName
            };
            db.Assessments.Add(entity);
            await db.SaveChangesAsync();

            return new AutoTable.Models.AssessmentItem
            {
                Id = entity.Id.ToString(),
                Name = entity.Name,
                ClassName = cls.Name,
                Subject = subj.Name,
                WeightPercent = entity.WeightPercent,
                DueDate = entity.DueDate ?? DateTime.MinValue,
                MarksEnteredPercent = entity.MarksEnteredPercent,
                IsVerified = entity.IsVerified,
                IsPublished = entity.IsPublished,
                PromotionRole = (AssessmentPromotionRole)entity.PromotionRole,
                AuthorId = entity.AuthorUserId,
                AuthorName = entity.AuthorName
            };
        }

        // Budget line CRUD
        public async Task<IReadOnlyList<AutoTable.Models.BudgetLine>> GetBudgetLinesAsync(string? financialYear = null)
        {
            using var db = CreateContext();
            var query = db.BudgetLines.AsQueryable();
            if (!string.IsNullOrWhiteSpace(financialYear))
                query = query.Where(b => b.FinancialYear == financialYear);
            var list = await query.OrderBy(b => b.Category).ToListAsync();
            return list.Select(b => new AutoTable.Models.BudgetLine
            {
                Id = b.Id,
                Category = b.Category,
                Budgeted = b.Budgeted,
                Spent = b.Spent,
                FinancialYear = b.FinancialYear
            }).ToList();
        }

        public async Task<AutoTable.Models.BudgetLine> CreateBudgetLineAsync(AutoTable.Models.BudgetLine line)
        {
            using var db = CreateContext();
            if (await db.BudgetLines.AnyAsync(b => b.Category == line.Category && b.FinancialYear == line.FinancialYear))
                throw new InvalidOperationException("A budget line with this category already exists for the selected year.");
            var entity = new BudgetLineEntity
            {
                Category = line.Category,
                Budgeted = line.Budgeted,
                Spent = line.Spent,
                FinancialYear = line.FinancialYear,
                CreatedAt = DateTime.UtcNow
            };
            db.BudgetLines.Add(entity);
            await db.SaveChangesAsync();
            line.Id = entity.Id;
            return line;
        }

        public async Task<AutoTable.Models.BudgetLine?> UpdateBudgetLineAsync(AutoTable.Models.BudgetLine line)
        {
            using var db = CreateContext();
            var entity = await db.BudgetLines.FindAsync(line.Id);
            if (entity == null) return null;
            entity.Category = line.Category;
            entity.Budgeted = line.Budgeted;
            entity.Spent = line.Spent;
            entity.FinancialYear = line.FinancialYear;
            db.BudgetLines.Update(entity);
            await db.SaveChangesAsync();
            return line;
        }

        public async Task DeleteBudgetLineAsync(int budgetLineId)
        {
            using var db = CreateContext();
            var entity = await db.BudgetLines.FindAsync(budgetLineId);
            if (entity == null) return;
            db.BudgetLines.Remove(entity);
            await db.SaveChangesAsync();
}
        // â”€â”€â”€ Promotion / repeat (Term 3 move-up) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static int NumericSuffix(string name)
        {
            var digits = new string((name ?? string.Empty).Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : int.MaxValue;
        }

        /// <summary>
        /// Returns the next class in the school's sequence (by numeric suffix, P1â†’P2â€¦)
        /// that is greater than the supplied class, or null when it is the highest class.
        /// </summary>
        public async Task<AutoTable.Models.SimpleLookup?> SuggestNextClassAsync(int currentClassId)
        {
            using var db = CreateContext();
            var current = await db.Classes.FindAsync(currentClassId);
            if (current == null) return null;
            var currentNum = NumericSuffix(current.Name);
            var all = await db.Classes.OrderBy(c => NumericSuffix(c.Name)).ToListAsync();
            var next = all
                .Where(c => c.Id != current.Id && NumericSuffix(c.Name) > currentNum)
                .OrderBy(c => NumericSuffix(c.Name))
                .FirstOrDefault();
            return next == null
                ? null
                : new AutoTable.Models.SimpleLookup { Id = next.Id, Name = next.Name };
        }
        /// <summary>
        /// Computes a student's terminal average across subjects. For each subject the
        /// highest-weight assessment's mark is taken (the same approach as the report card),
        /// and those subject marks are averaged.
        /// </summary>
        private async Task<double> ComputeStudentAverageAsync(AppDbContext db, int studentId)
        {
            var marks = await db.Marks
                .Where(m => m.StudentId == studentId && m.Mark.HasValue)
                .Include(m => m.Assessment).ThenInclude(a => a.Subject)
                .Include(m => m.Assessment).ThenInclude(a => a.Term)
                .ToListAsync();

            // The terminal term is considered the latest term that has assessments (Term 3 move-up).
            var latestTerm = marks
                .Select(m => m.Assessment.Term)
                .Where(t => t != null)
                .OrderByDescending(t => t.EndDate ?? t.StartDate)
                .ThenByDescending(t => t.Id)
                .FirstOrDefault();

            var termMarks = latestTerm == null
                ? marks
                : marks.Where(m => m.Assessment.Term != null && m.Assessment.Term.Id == latestTerm.Id).ToList();

            var subjectAvgs = new List<double>();
            foreach (var grp in termMarks.GroupBy(m => m.Assessment.Subject?.Name ?? "General"))
            {
                var best = grp
                    .OrderByDescending(m => m.Assessment.WeightPercent)
                    .ThenByDescending(m => m.Assessment.DueDate)
                    .FirstOrDefault();
                if (best != null)
                    subjectAvgs.Add(best.Mark ?? 0);
            }
            return subjectAvgs.Count > 0 ? subjectAvgs.Average() : 0;
        }

        /// <summary>
        /// Returns the promotion overview (Term 3 move-up) for active students, optionally
        /// filtered to one class. The suggested outcome is Promote when the student's terminal
        /// average meets the class's grading-system pass mark, otherwise Repeat.
        /// </summary>
        public async Task<IReadOnlyList<AutoTable.Models.PromotionRow>> GetPromotionOverviewAsync(int? classId = null)
        {
            using var db = CreateContext();
            var query = db.Students
                .Include(s => s.Class).ThenInclude(c => c!.GradingSystem)
                .Include(s => s.Stream)
                .Where(s => s.IsActive);
            if (classId.HasValue)
                query = query.Where(s => s.ClassId == classId);
            var students = await query.ToListAsync();

            // Resolve grading systems per class: class-specific → school default → empty.
            var schoolDefault = await db.GradingSystems.FirstOrDefaultAsync(g => g.IsDefault)
                ?? await db.GradingSystems.OrderBy(g => g.Name).FirstOrDefaultAsync();
            double defaultPassMark = schoolDefault?.PassMark ?? 50;
            var defaultBands = schoolDefault != null
                ? await db.GradeBands.Where(b => b.GradingSystemId == schoolDefault.Id).OrderBy(b => b.MinScore).ToListAsync()
                : new List<GradeBandEntity>();
            var defaultBandInfos = defaultBands.Select(b => new AutoTable.Models.GradeBandInfo
            {
                Id = b.Id, Label = b.Label,
                MinScore = b.MinScore, MaxScore = b.MaxScore,
                IsPromotionalPass = b.IsPromotionalPass,
                IsRepeater = b.IsRepeater,
                IsPromotionalFail = b.IsPromotionalFail
            }).ToList();

            // Cache grading system bands per class to avoid redundant queries.
            var classBandCache = new Dictionary<int, (double PassMark, IReadOnlyList<AutoTable.Models.GradeBandInfo> Bands)>();

            var rows = new List<AutoTable.Models.PromotionRow>();
            foreach (var s in students)
            {
                // Resolve grading system for this student's class.
                double passMark;
                IReadOnlyList<AutoTable.Models.GradeBandInfo> bandInfos;
                if (s.ClassId.HasValue)
                {
                    if (!classBandCache.TryGetValue(s.ClassId.Value, out var cached))
                    {
                        var gs = s.Class?.GradingSystem;
                        if (gs == null)
                            gs = await db.GradingSystems.FirstOrDefaultAsync(g => g.IsDefault)
                                ?? await db.GradingSystems.OrderBy(g => g.Name).FirstOrDefaultAsync();
                        var pMark = gs?.PassMark ?? defaultPassMark;
                        var bands = gs != null
                            ? await db.GradeBands.Where(b => b.GradingSystemId == gs.Id).OrderBy(b => b.MinScore).ToListAsync()
                            : defaultBands;
                        var infos = bands.Select(b => new AutoTable.Models.GradeBandInfo
                        {
                            Id = b.Id, Label = b.Label,
                            MinScore = b.MinScore, MaxScore = b.MaxScore,
                            IsPromotionalPass = b.IsPromotionalPass,
                            IsRepeater = b.IsRepeater,
                            IsPromotionalFail = b.IsPromotionalFail
                        }).ToList();
                        cached = (pMark, infos);
                        classBandCache[s.ClassId.Value] = cached;
                    }
                    passMark = cached.PassMark;
                    bandInfos = cached.Bands;
                }
                else
                {
                    passMark = defaultPassMark;
                    bandInfos = defaultBandInfos;
                }

                var avg = await ComputeStudentAverageAsync(db, s.Id);
                var suggested = CheckPromotionalPass(avg, passMark, bandInfos)
                    ? AutoTable.Models.PromotionStatus.Promoted
                    : AutoTable.Models.PromotionStatus.Repeat;

                int? targetClassId = null;
                string targetClassName = string.Empty;
                if (s.ClassId.HasValue)
                {
                    var nxt = await SuggestNextClassAsync(s.ClassId.Value);
                    if (nxt != null)
                    {
                        targetClassId = nxt.Id;
                        targetClassName = nxt.Name;
                    }
                }

                rows.Add(new AutoTable.Models.PromotionRow
                {
                    StudentId = s.Id,
                    StudentName = s.FullName,
                    LIN = s.LIN,
                    ClassId = s.ClassId ?? 0,
                    ClassName = s.Class?.Name ?? "-",
                    Stream = s.Stream?.Name ?? "-",
                    Average = Math.Round(avg, 1),
                    PassMark = passMark,
                    Grade = avg > 0 ? GradeFromBands(avg, bandInfos) : "-",
                    Suggested = suggested,
                    Status = (AutoTable.Models.PromotionStatus)s.PromotionStatus,
                    TargetClassId = targetClassId,
                    TargetClassName = targetClassName
                });
            }

            return rows
                .OrderBy(r => r.ClassName)
                .ThenBy(r => r.StudentName)
                .ToList();
        }

        /// <summary>Promote a student for the next academic year, moving them to the suggested (or given) next class.</summary>
        public async Task PromoteStudentAsync(int studentId, int? targetClassId = null)
        {
            using var db = CreateContext();
            var s = await db.Students.FindAsync(studentId);
            if (s == null) return;

            int? dest = targetClassId;
            if (!dest.HasValue && s.ClassId.HasValue)
            {
                var nxt = await SuggestNextClassAsync(s.ClassId.Value);
                dest = nxt?.Id;
            }

            s.PromotionStatus = (int)AutoTable.Models.PromotionStatus.Promoted;
            s.PromotedToClassId = dest;
            s.PromotionProcessedAt = DateTime.UtcNow;
            if (dest.HasValue)
            {
                s.ClassId = dest;
                s.StreamId = null;
            }
            await db.SaveChangesAsync();
        }

        /// <summary>Keep a student repeating their current class.</summary>
        public async Task RepeatStudentAsync(int studentId)
        {
            using var db = CreateContext();
            var s = await db.Students.FindAsync(studentId);
            if (s == null) return;
            s.PromotionStatus = (int)AutoTable.Models.PromotionStatus.Repeat;
            s.PromotedToClassId = null;
            s.PromotionProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        /// <summary>Manually shift a student to another class (skip-ahead / class change).</summary>
        public async Task ShiftStudentClassAsync(int studentId, int targetClassId)
        {
            using var db = CreateContext();
            var s = await db.Students.FindAsync(studentId);
            if (s == null) return;
            s.PromotionStatus = (int)AutoTable.Models.PromotionStatus.Shifted;
            s.PromotedToClassId = targetClassId;
            s.ClassId = targetClassId;
            s.StreamId = null;
            s.PromotionProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        /// <summary>Reset a student's promotion decision back to pending.</summary>
        public async Task ResetPromotionAsync(int studentId)
        {
            using var db = CreateContext();
            var s = await db.Students.FindAsync(studentId);
            if (s == null) return;
            s.PromotionStatus = (int)AutoTable.Models.PromotionStatus.Pending;
            s.PromotedToClassId = null;
            s.PromotionProcessedAt = null;
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Batch-process all pending students: promote those whose average meets the pass mark,
        /// repeat those who don't. Returns the number of students processed.
        /// </summary>
        public async Task<int> ProcessAllPromotionsAsync(int? classId = null)
        {
            var overview = await GetPromotionOverviewAsync(classId);
            int processed = 0;
            foreach (var row in overview.Where(r => r.Status == AutoTable.Models.PromotionStatus.Pending))
            {
                if (row.Suggested == AutoTable.Models.PromotionStatus.Promoted)
                    await PromoteStudentAsync(row.StudentId, row.TargetClassId);
                else
                    await RepeatStudentAsync(row.StudentId);
                processed++;
            }
            return processed;
        }
    }
}
