# AutoTable — Prototype Roadmap

> Assessed against the **school-management prototype brief** (`Prototype.txt`) and the repo's
> own `OPERATIONAL_PLAN.md`. This document records how far the application has come toward the
> described prototype, which parts are implemented versus the plan, and what still remains.
>
> - Repo root: `c:\Users\manue\Desktop\Desktop_Apps\AutoTable`
> - Stack: .NET 8 / WinUI 3 (Windows App SDK 2.3.x) + EF Core SQLite
> - Framework default: persistent DB at `%LOCALAPPDATA%\AutoTable\autotable.db`
> - Build: `dotnet build AutoTable.csproj -p:Platform=x64` → **succeeds**, 0 errors

---

## 1. Overall progress

The application has moved **well beyond a bare prototype** into a working, DB-persisted
platform covering the two core domains the brief names — **student performance** and
**school financials** — plus **administration** (students, teachers, classes, terms, audit).
Roughly **75–80%** of the described prototype surface is present in some functional form.
The gaps are concentrated in four areas: **role-based access-control enforcement**,
**end-of-year promotion/repeat**, **granular financial analytics (defaulters)**, and
**assessment authorship / mid-term report slips**.

| Domain | Status |
|---|---|
| Terms & term context | 🟢 Mostly done |
| Classes, streams, subjects, class teacher | 🟢 Mostly done (stream-level teacher partial) |
| Students & enrollment | 🟢 Done |
| Teachers | 🟢 Done |
| Assessments & marks entry | 🟡 Partial (author missing; rosters wired) |
| Gradebook / performance / report cards | 🟢 Done (+ real printing) |
| Financials (fees, budget) | 🟢 KPI/CRUD done / 🟡 granular analytics missing |
| Access control (admin vs data-entrant) | 🟡 Partial (only Moderation gated) |
| Promotion / repeat (Term 3 move-up) | 🔴 Not implemented |
| Dashboard parity | 🟢 Good |

---

## 2. Required hierarchy checklist (from the brief)

| # | Prototype requirement | Status | Evidence / notes |
|---|---|---|---|
| 1 | A term is the context for everything; no active term ⇒ nothing active | 🟡 Partial | `TermEntity.IsActive`, startup housekeeping ensures ≤1 active term; **not strictly enforced** as a global gate on enrollment/class/assessment (assessment creation throws if term missing). |
| 2 | Creating a term makes it the **active** term; has start/end dates | 🟢 Done | `CreateTermAsync` sets active + deactivates others; start/end on Term; month-window guard added. |
| 3 | Per-term, per-class **fee/tuition** amounts | 🟢 Done | `TermFee` entity, `SetTermFeeAsync`/`GetTermFeesAsync`. |
| 4 | Enroll students in a specific term → class + stream; enter fee on enrollment | 🟢 Done | `EnrollmentFormView` (class + stream), `SaveEnrollmentAsync`, `CreateStudentWithInitialDataAsync(initialFeeAmount)`. |
| 5 | Class must exist before enrolling; create classes | 🟢 Done | `CreateClassAsync`, Classes management page. |
| 6 | Classes have **streams** | 🟢 Done | `ClassStream` join, create/assign/remove streams. |
| 7 | Classes have **subjects** | 🟢 Done | `ClassSubject` join, assign/remove subjects per class. |
| 8 | **Class teacher** assigned to a class/stream | 🟢 Done | `Classes.ClassTeacherId` per **class**; Create Class modal lets you pick any teacher — registered **or** student teacher (no restriction); one teacher can lead many classes. |
| 9 | Teacher must exist to be assignable; teachers carry name/phone/email/next-of-kin/subjects/classes/previous-school/qualification | 🟢 Done | `Teacher` model + registration modal (2-col) + CRUD. |
| 9a | Each class uses a **grading system** (existing or created new at the same time) | 🟢 Done | `GradingSystemEntity`/`GradeBandEntity`; standalone "Grading Systems" card on Class Management + a grading-system builder reused inside the **Create Class** modal (pick existing system or build new with bands/names/pass/repeat/flag rows at class-creation time). |
| 10 | Admin creates term + class; data entrant enrolls students/teachers and handles fees | 🟡 Partial | Roles exist (`Administrator`/`DataEntrant`); only **Moderation** nav is role-gated. Term/Class/Budget are **not** admin-gated. |
| 11 | **Budget** restricted to admin | 🔴 Not gated | Budget page/VM work from DB, but no role restriction. |
| 12 | Assessments can target **whole school / a class / a stream**; each has an **author** | 🟡 Partial | `AssessmentScope` (Single / AllInClass / SpecificSubjects / AllInSchool) + `IsClassWide`/`StreamId` ✔; **no author/teacher field** on assessment ✘. |
| 13 | Marks entry returns **only the roster** that sat the paper (by class or stream) | 🟢 Done | `GetStudentMarksAsync` resolves exact class+subject+assessment; dropdown filtered to that pair. |
| 14 | Assessments indicate whether they **count toward end-of-term** | 🟠 Not explicit | `WeightPercent` exists; an explicit "contributes to end-of-term" flag is not present. |
| 15 | Gradebook allows promotion/repeat decision (Term 3 → next class, P1→P2…P7) | 🔴 Not implemented | No promote/repeat field or next-class auto-shift; no manual "skip a grade" move. |
| 16 | Report cards: end-of-term card + **mid-term slips** (3–4 per A4), finance-filterable, preview | 🟡 Partial | Full A4 per-student + print-all with real `PrintManager` preview ✔; **mid-term slips** ✘; finance filtering of print set ✘. |
| 17 | Very **granular querying** (performance and finance) up to a single student / top defaulters / paid-in-last-3-days | 🟡 Partial | Gradebook filters (class/subject/stream/student/year/term) ✔; finance = expected/collected/outstanding KPIs + fee list only — **no defaulter/cohort analytics** ✘. |
| 18 | Dashboard merges **performance + financials** info | 🟢 Done | Student count, assessment count, avg score, revenue KPIs; quick actions; AI insights; recent activity; search. |

---

## 3. What is implemented (mapped to implementation files)

### Terms & term-context
- `TermEntity` (Id, Name, StartDate, EndDate, IsActive); `IDataService.GetActiveTermAsync()`.
- `TermManagementView` + VM: create (becomes active), deactivate ended terms, per-class fee entry.
- Startup housekeeping in `App.xaml.cs` (ensure one active term; deactivate ended).
- Month-window guard on term creation (per user spec: Term1 Jan–Apr, Term2 late-Apr–Jul, Term3 Sep–Nov).

### Students, classes, streams, teachers
- `StudentEntity` (+ full extended fields: guardian, residency, health, emergency, custody).
- `EnrollmentFormView` (2-column modal per `modal-size.md`), `SaveEnrollmentAsync`, LIN auto-gen, `CreateStudentWithInitialDataAsync`.
- `ClassEntity` (+ `ClassTeacherId`, `ClassStreams`, `ClassSubjects`), `StreamEntity`, `SubjectEntity`.
- `ClassSubjectEntity` / `ClassStreamEntity` join tables; assign/remove wired (`AssignSubjectToClassAsync`, etc.).
- `Teacher` model + registration modal; `GetTeachersAsync`/`CreateTeacherAsync`/`UpdateTeacherAsync`/`DeleteTeacherAsync` backed by `UserEntity role="Teacher"`.

### Performance path
- `AssessmentItem` incl. `AssessmentScope`, `IsClassWide`, `StreamId`, `WeightPercent`, moderation flags.
- `AssessmentsView` creation flow; `MarksEntryView` (Save Draft / Submit persist via `UpdateMarkAsync`, progress + partial save); `GradebookView` (filters + ranks/grades); `StudentPerformanceView` + modal (per-subject cat/mid/end, overall avg/grade/rank); `AnalyticsView`; `ModerationView` (verify/publish persisted).
- `ReportCardsView`: A4 preview + real `PrintManager` printing (per-student and print-all).

### Financial path
- `TermFee` (per-class fees per term), `FeePayments` (with term + recorded-by + description).
- `FeeCollectionView` + record-payment modal (active-term default, student-class override, inline typeahead results).
- `FinancialsDashboardViewModel` — KPIs from DB (total collected, outstanding, budget used %, % students paid).
- `BudgetLineEntity` + CRUD + `BudgetViewModel` wired to DB + CSV export.

### Cross-cutting
- Persistent SQLite via `App.xaml.cs` + `DatabaseDataService` as canonical `IDataService`; `AppServices.DataService`.
- Role model (`UserRole`) + `SessionService.IsAdministrator`; login/signup.
- `NavigationService` with persistent sidebar (quick-action navigation keeps chrome).
- Startup diagnostics (fail-loud, no silent mock); demo mode via `Demo/` adapter only.

---

## 4. Not yet implemented / gaps vs the prototype (the "left")

### P0 — correctness of the brief's core loop
1. **Promotion / repeat (end-of-year, Term 3).** Add a promotion decision in the gradebook /
   report context: **Promoted / Repeat** per student; when promoted, automatically move the
   student from class N → N+1 (P1→P2 … P6→P7), with a **manual "shift/skip"** override to any
   target class. Needs a new column/field, a service method (e.g. `PromoteStudentAsync`), and a
   UI section. This is the single largest missing feature.

### P0 — access control (role gates the brief is explicit about)
2. **Gate admin-only pages**: Term Management, Class Management, and Budget must be
   visible/usable **only to `Administrator`** (mirror the existing `NavModeration` gating).
   Students, Teachers, Fees remain available to data entrants. Also gate at the route level,
   not just sidebar visibility, so a data entrant can't navigate directly.

### P1 — financial granular analytics (brief emphasizes "95% accuracy / limitless combinations")
3. **Defaulters / cohort queries.** Add reports+queries such as: top-N defaulters,
   % paid within the last 3 days/7 days, unpaid balances > threshold, "all males in class X
   unpaid > 500k in a week", filter by class/stream/term/gender/date-range, and single-student
   finance drill-down. Serve via `IDataService` queries and surface in Financials views.

### P1 — assessment authorship & syllabus weighting
4. **Assessment author/teacher.** Add an author (Teacher) to `AssessmentItem`/`AssessmentEntity`
   and enforce that the teacher must exist (consistent with the rest of the hierarchy).
5. **"Counts toward end-of-term"** flag on assessments (beyond `WeightPercent`), so report-card
   totals include only contributing assessments — the brief calls this out explicitly.

### P2 — reporting completeness
6. **Mid-term slips.** A "report centre" layout that prints several small slips on one A4
   (3–4 up) for a chosen assessment (usually mid-term), selectable per subject/stream teacher —
   distinct from the full A4 report card.
7. **Finance-filtered report-card printing** (e.g. only students who have cleared fees / paid ≥ threshold).

### P2 — term-context enforcement
8. **Enforce "no active term ⇒ nothing active".** Blank/disable student enrollment, assessment
   creation, and fee entry until an active term exists, with a clear prompt to create one —
   matching the brief's "term is the context in which everything exists".

### P3 — polish & hardening (partly tracked in CONTEXT_REPORT / temp logs)
9. Integration tests (SQLite in-memory) for the core loops (enroll→fee, create assessment→marks→report card).
10. `ReportCardsView` per-student preview currently builds a simple grid; make it a faithful,
    printable report-card template (bio data, per-subject table, teacher comments, promotion status once #1 lands).
11. Migrations discipline / EF migration coverage for all the compatibility `ALTER TABLE`
    patches added in `App.xaml.cs` (Terms, Assessments, Students, Users, Classes, FeePayments, BudgetLines).

---

## 5. Suggested build order

1. **Promotion / repeat** (#1) + a gradebook promote column — biggest user-visible gap, unblocks "year moves".
2. **Role gates** (#2) for Term/Class/Budget — matches the access-control section of the brief.
3. **Assessment author + end-of-term flag** (#4, #5) — completes the assessment model.
4. **Defaulters / financial analytics** (#3).
5. **Mid-term slips + finance-filtered printing** (#6, #7).
6. **Active-term enforcement** (#8), then tests/polish (#9–#11).

---

## 6. Data model / files most relevant to the remaining work

- `AutoTable/Models/AssessmentItem.cs` — add `AuthorId`/`AuthorName`, `CountsTowardsEndOfTerm`.
- `AutoTable/Data/Entities/StudentEntity.cs` — `ClassEntity.ClassTeacherId` exists; **stream-level** class teacher + a student `PromotionStatus`/`PromotedToClassId` field are the add-ons.
- `AutoTable/Services/IDataService.cs` / `DatabaseDataService.cs` — add `PromoteStudentAsync`, `ShiftStudentClassAsync`, defaulter queries, `GetAssessmentAuthors` etc.
- `Views/ShellView.xaml(.cs)` — extend role gating from `NavModeration` to `NavTermManagement`, `NavClasses`, `NavBudget`; also enforce in `NavigateTo`.
- `Views/ReportCardsView.*` — add slip layout + finance filter; `GradebookView` — add promote/repeat column; `Views/FinancialsDashboardView*` / `FeeCollectionView*` — add defaulters/cohort panels.

---

*Generated from a fresh read of the codebase (Views/ViewModels/Services/Data), OPERATIONAL_PLAN.md, and CONTEXT_REPORT.md. Feature claims above were verified against the actual source, not assumed.*
