import sys, io

LT = chr(60)
GT = chr(62)

path = "AutoTable/Services/DatabaseDataService.cs"
with io.open(path, encoding="utf-8") as f:
    src = f.read()

# 1) Change CreateAssessmentAsync signature: Task -> Task of AssessmentItem.
old_sig = "        public async Task CreateAssessmentAsync(AutoTable.Models.AssessmentItem item)"
new_sig = "        public async Task" + LT + "AutoTable.Models.AssessmentItem" + GT + " CreateAssessmentAsync(AutoTable.Models.AssessmentItem item)"
if old_sig not in src:
    print("ERROR: signature not found")
    sys.exit(1)
src = src.replace(old_sig, new_sig, 1)

# 2) Insert return of created model + new marks-CRUD methods before class/namespace close.
anchor = "            db.Assessments.Add(entity);\n            await db.SaveChangesAsync();\n        }"
if anchor not in src:
    print("ERROR: method body anchor not found")
    sys.exit(1)

methods_template = """
        public async Task<__LT__AutoTable.Models.AssessmentItem?__GT__> GetAssessmentAsync(string name, string className, string subject)
        {
            using var db = CreateContext();
            var cls = await db.Classes.FirstOrDefaultAsync(c => c.Name == className) ?? db.Classes.FirstOrDefault();
            var subj = await db.Subjects.FirstOrDefaultAsync(s => s.Name == subject) ?? db.Subjects.FirstOrDefault();
            if (cls == null || subj == null) return null;

            var entity = await db.Assessments
                .Include(a => a.Class)
                .Include(a => a.Subject)
                .FirstOrDefaultAsync(a => a.Name == name && a.ClassId == cls.Id && a.SubjectId == subj.Id);

            if (entity == null) return null;

            return new AutoTable.Models.AssessmentItem
            {
                Id = entity.Id.ToString(),
                Name = entity.Name,
                ClassName = entity.Class?.Name ?? string.Empty,
                Subject = entity.Subject?.Name ?? string.Empty,
                WeightPercent = entity.WeightPercent,
                DueDate = entity.DueDate ?? DateTime.MinValue,
                MarksEnteredPercent = entity.MarksEnteredPercent,
                IsVerified = entity.IsVerified,
                IsPublished = entity.IsPublished
            };
        }

        public async Task UpdateMarkAsync(int assessmentId, int studentId, double? mark, string? grade, string? remarks = null)
        {
            using var db = CreateContext();
            var existing = await db.Marks.FirstOrDefaultAsync(m => m.AssessmentId == assessmentId && m.StudentId == studentId);
            if (existing == null)
            {
                var markEntity = new MarkEntity
                {
                    StudentId = studentId,
                    AssessmentId = assessmentId,
                    Mark = mark,
                    Grade = grade,
                    Remarks = remarks,
                    EnteredAt = DateTime.UtcNow
                };
                db.Marks.Add(markEntity);
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
            await UpdateCompletionForAssessmentAsync(db, assessmentId);
        }

        public async Task DeleteMarkAsync(int assessmentId, int studentId)
        {
            using var db = CreateContext();
            var mark = await db.Marks.FirstOrDefaultAsync(m => m.AssessmentId == assessmentId && m.StudentId == studentId);
            if (mark != null)
            {
                db.Marks.Remove(mark);
                await db.SaveChangesAsync();
                await UpdateCompletionForAssessmentAsync(db, assessmentId);
            }
        }

        public async Task UpdateAssessmentCompletionAsync(int assessmentId)
        {
            using var db = CreateContext();
            await UpdateCompletionForAssessmentAsync(db, assessmentId);
        }

        private async Task UpdateCompletionForAssessmentAsync(AppDbContext db, int assessmentId)
        {
            var assessment = await db.Assessments.FindAsync(assessmentId);
            if (assessment == null) return;
            var totalStudents = await db.Students.CountAsync(s => s.ClassId == assessment.ClassId && s.IsActive);
            var markedStudents = await db.Marks.CountAsync(m => m.AssessmentId == assessmentId && m.Mark.HasValue);
            assessment.MarksEnteredPercent = totalStudents == 0 ? 0 : (int)Math.Round((double)markedStudents / totalStudents * 100);
            await db.SaveChangesAsync();
        }
"""

methods_code = methods_template.replace("__LT__", LT).replace("__GT__", GT)

new_block = (
    "            db.Assessments.Add(entity);\n"
    "            await db.SaveChangesAsync();\n\n"
    "            // return the created model so callers can insert it in-place\n"
    "            item.Id = entity.Id.ToString();\n"
    "            item.MarksEnteredPercent = entity.MarksEnteredPercent;\n"
    "            item.IsVerified = entity.IsVerified;\n"
    "            item.IsPublished = entity.IsPublished;\n"
    "            return item;\n"
    "        }\n\n"
    + methods_code
)

src = src.replace(anchor, new_block, 1)

with io.open(path, "w", encoding="utf-8") as f:
    f.write(src)

print("OK: DatabaseDataService.cs updated")
</arg_value>
<task_progress>- [x] Append detailed Operational Plan step-by-step status and roadmap to CONTEXT_REPORT.md
- [x] Phase 2: Read IDataService, DatabaseDataService
- [ ] Phase 2: Read AssessmentsView.xaml.cs, MarksEntryViewModel.cs
- [ ] Phase 2: Run edit_phase2.py to update DatabaseDataService (CreateAssessmentAsync return + marks CRUD + student sort already done)
- [ ] Phase 2: Wire AssessmentsView to insert created item at index 0
- [ ] Phase 2: Wire MarksEntryViewModel SaveDraft/SubmitMarks
- [ ] Phase 2: Build and verify
- [ ] Phase 4: Moderation + AI wiring
- [ ] Phase 4: Build and verify
- [ ] Phase 5: Financials</task_progress>
</write_to_file>