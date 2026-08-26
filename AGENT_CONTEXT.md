# AutoTable - Agent Context Export

This file summarizes the current workspace state, recent changes, and run/setup instructions so another agent (or automation) can continue work.

> Paths in this document are relative to the repository root: `C:\Users\manue\Desktop\Desktop_Apps\AutoTable\`

---

## Environment

- OS/IDE: Microsoft Visual Studio Community 2026 (18.7.3)
- Project target: .NET 8
- Solution file: `AutoTable.slnx`
- Active branch: `sql_rec` (origin: https://github.com/ManuelPrhyme/AutoTable)
- UI framework: WinUI 3 (Windows App SDK 2.3.x)
- Build command: `dotnet build AutoTable.csproj -p:Platform=x64` → **0 errors**
- Test command: `dotnet test Tests/AutoTable.IntegrationTests -p:Platform=x64` → **9/9 passing**

---

## High-level goal

AutoTable is a single-source-of-truth desktop school management app where every CRUD operation persists to SQLite via EF Core. The operational plan (OPERATIONAL_PLAN.md) is **fully implemented** for Phases 2–5. The current work is on the **P5 feature backlog**: grading system integration, assessment promotion roles, promotion/repeat flow, class management enhancements, role gating, analytics, and enforcement.

---

## Current State (26 Aug 2026)

### Build: 0 errors, clean

### What's Done (all operational plan steps)
- Persistent DB at `%LOCALAPPDATA%\AutoTable\autotable.db` with dev ephemeral flag
- Fail-loud startup with diagnostics, explicit demo mode only
- Full CRUD: Students, Teachers, Classes, Subjects, Terms, Assessments, Marks, Fees, Budget
- All ViewModels use `AppServices.DataService` (no mock fallback)
- A4 report-card printing (native Windows PrintManager)
- Grading systems domain (named scales with promotion/repeat bands)
- Class creation single-modal rework (teacher + grading system + streams/subjects)
- **Class edit modal** with full metadata editing (name, teacher, grading system) + streams/subjects
- **Immediate subject/stream persistence** — new items saved to DB on Add click, not deferred
- **Streams Add button fix** — priority logic corrected, button styled
- Integration tests (9 passing)
- **P5.1: Assessment promotion role** — tri-state enum + creation dialog + report card classification
- **P5.2: Promotion/repeat flow** — full UI with promote/repeat/shift/reset + batch "Process All"
- **P5.3: Grade resolution via grading systems** — `GradeFromBands` replaces `GradeFromAverage`
- **TermFees table fix** — schema migration for existing DBs
- **Financial Dashboard KPI merge** — term selector + merged KPI cards
- **Fee Collection per-student amounts** — Expected/Paid/Balance per student/class/term
- **Record Payment modal white text** — all text on dark dialog
- **LIN uniqueness gap fix** — 3-layer defense against orphaned enrollment records

### What's In Progress (uncommitted, 25+ files)
| Feature | Status |
|---------|--------|
| Grade resolution via grading systems (P5.3) | ✅ Code complete |
| Assessment promotion role (P5.1) | ✅ Done |
| Promotion/repeat flow (P5.2) | ✅ Done |
| Analytics view/ViewModel | ✅ Code complete |
| FeeCollection real data | ✅ Code complete |
| Stream teacher assignment | ✅ Code complete |
| Dashboard/ClassesView improvements | ✅ Code complete |
| Class edit modal (name/teacher/grading system) | ✅ Done |
| Immediate subject/stream persistence | ✅ Done |
| Streams Add button fix | ✅ Done |
| TermFees migration + KPI cards | ✅ Done |
| Financial Dashboard merge | ✅ Done |
| Fee Collection per-student amounts | ✅ Done |
| LIN uniqueness gap fix | ✅ Done |

### What's Next (P5 backlog)
| # | Feature | Status |
|---|---------|--------|
| P5.4 | Admin role-gating | 📋 **PLANNED** — documented in WAY_FORWARD_PLAN.md |
| P5.5 | Defaulters / cohort analytics | 🔴 Not started |
| P5.6 | Mid-term slips | 🔴 Not started |
| P5.7 | Active-term enforcement | 🔴 Not started |

---

## Key Files

### Data Layer
- `AutoTable/Data/Entities/StudentEntity.cs` — ALL EF entities (Student, Class, Stream, Subject, Term, AcademicYear, Assessment, Mark, FeePayment, User, ClassSubject, ClassStream, TermFee, BudgetLine, GradingSystem, GradeBand, TerminationLog, Enrollment)
- `AutoTable/Data/AppDbContext.cs` — EF Core DbContext with ForeignKeyInterceptor
- `AutoTable/Data/SeedData.cs` — NOT called at startup (by design)

### Services
- `AutoTable/Services/IDataService.cs` — full async interface (students, assessments, marks, grades, fees, teachers, budget, grading systems, promotion, report cards, class updates)
- `AutoTable/Services/DatabaseDataService.cs` — EF Core implementation (~2000 lines)
- `AutoTable/AppServices.cs` — global static IDataService holder

### Models
- `Models/AssessmentItem.cs` — `AssessmentScope` enum, `AssessmentPromotionRole` enum, `AssessmentItem` DTO
- `AutoTable/Models/GradingSystemModels.cs` — `GradingSystemInfo`, `GradeBandInfo`
- `Models/ReportCardModels.cs` — `ReportCardSheetModel`, `ReportCardAssessmentRow`
- `AutoTable/Models/ClassInfo.cs` — ClassInfo DTO with `ClassTeacherId` and `GradingSystemId`
- `AutoTable/Models/Student.cs` — Student DTO with enrollment fields
- `AutoTable/Models/SimpleLookup.cs` — Id/Name DTO for lists

### Views & ViewModels
- `Views/ClassesView.xaml.cs` — class management (single-modal create, edit modal with name/teacher/grading/streams/subjects, white chip UI with ✕ buttons)
- `Views/AssessmentsView.xaml.cs` — assessment creation dialog (4 scope options + promotion role selector)
- `Views/FeeCollectionView.xaml.cs` — fee collection with Record Payment modal (searchable typeahead, white text)
- `Views/ReportCardsView.xaml.cs` — A4 report card preview + PrintManager printing
- `Views/Controls/ReportCardSheetView.xaml` — A4 report card sheet control
- `Views/ShellView.xaml.cs` — navigation shell with role-gated sidebar

### Startup
- `App.xaml.cs` — DB init, connection string, schema patches (ALTER TABLE for legacy DBs, TermFees table creation), ForeignKeyInterceptor registration

---

## Grading System Resolution

When resolving grades for a class:
```
Class.GradingSystem → GradingSystems.FirstOrDefault(IsDefault) → first available system → hard-coded 50% fallback
```

`GradeFromBands(mark, bands)` replaces `GradeFromAverage(mark)`. If no bands exist, falls back to the old A-F scale.

---

## PromotionRole Mapping

```
AssessmentPromotionRole.None            → int 0  (default)
AssessmentPromotionRole.CountsTowardPromotion → int 1
AssessmentPromotionRole.PromotionExam   → int 2
```

Report card classification:
- If any assessment has `PromotionRole != 0`: use explicit roles
- Otherwise (legacy data): fallback to weight-based heuristic

---

## How to Run

1. Build: `dotnet build AutoTable.csproj -p:Platform=x64`
2. Run from Visual Studio or `dotnet run` — app creates/uses persistent DB at `%LOCALAPPDATA%\AutoTable\autotable.db`
3. For ephemeral dev DB: set env var `AUTOTABLE_DEV_EPHEMERAL_DB=true`
4. For demo mode (mock adapter): set env var `AUTOTABLE_DEMO_MODE=true`

---

## Implementation Log

See **IMPLEMENTATION_LOG.md** for a detailed, up-to-date log of what's being implemented as it progresses. It tracks:
- Feature status (done / in progress / not started)
- Files changed per feature
- How features work (architecture decisions)
- Remaining gaps
- Key patterns and conventions

---

*Last updated: 26 Aug 2026 — Buffy (Codebuff agent)*
