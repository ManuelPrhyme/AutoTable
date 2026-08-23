# AutoTable — Comprehensive Assessment & Way-Forward Plan
**Date:** 23 August 2026  
**Branch:** sql_rec  
**Status:** Build fails (8 errors); ~60% data/service layer done; ~40% UI wiring done

---

## 1. EXECUTIVE SUMMARY

AutoTable is a **WinUI 3 / .NET 8** desktop application for marks and performance management in primary schools. It uses **SQLite + EF Core** for persistence, **MVVM Toolkit** for architecture, and has a rich feature set covering assessments, marks entry, gradebook, report cards, moderation, AI insights, financials, student enrollment, and teacher management.

**Current state:** The data/service layer is substantially complete — the `IDataService` interface has ~50 async methods, and `DatabaseDataService` implements the vast majority. However, the **build is currently broken** (8 errors), and several ViewModels still have stub implementations or hardcoded data that hasn't been wired to the database.

**Critical finding:** The build fails due to one C# error (`CS1061` in `ClassesView.xaml.cs:35` — missing `using System.Linq`) which cascades into 7 XAML compiler errors (`WMC0001` unknown converter types). The converters themselves exist and are correct; the XAML compiler cannot resolve them because C# compilation failed first. This is a single-line fix.

---

## 2. BUILD STATUS — P0 BLOCKER

### Current Errors (8 total, 1 root cause)

| Error | File | Root Cause |
|-------|------|------------|
| **CS1061**: `'IReadOnlyList<Teacher>' does not contain a definition for 'Where'` | `Views/ClassesView.xaml.cs:35` | Missing `using System.Linq;` — LINQ extension methods unavailable |
| WMC0001: Unknown type 'DateFormatConverter' | `App.xaml:14` | **Cascade** — C# build failed → no assembly → XAML can't resolve types |
| WMC0001: Unknown type 'CurrencyConverter' | `App.xaml:15` | Cascade |
| WMC0001: Unknown type 'PercentConverter' | `App.xaml:16` | Cascade |
| WMC0001: Unknown type 'DecimalConverter' | `App.xaml:17` | Cascade |
| WMC0001: Unknown type 'GradeColorConverter' | `App.xaml:18` | Cascade |
| WMC0001: Unknown type 'StatusColorConverter' | `App.xaml:19` | Cascade |
| WMC9999: Object reference not set to instance | XAML Internal Error | Cascade |

### Fix
Add `using System.Linq;` to `Views/ClassesView.xaml.cs`. That's it. All 7 XAML errors are cascades.

---

## 3. WHAT'S DONE (Operational Plan Steps)

| # | Step | Status | Evidence |
|---|------|--------|----------|
| 1 | Persistent DB path (`%LOCALAPPDATA%\AutoTable\autotable.db`) | ✅ DONE | `App.xaml.cs:62-67` |
| 2 | `AUTOTABLE_DEV_EPHEMERAL_DB` / `AUTOTABLE_DEMO_MODE` flags | ✅ DONE | `App.xaml.cs:54-76` |
| 3 | Fail-loud startup, diagnostics log, no silent mock fallback | ✅ DONE | Error dialog + temp log + abort |
| 4 | `CreateAssessmentAsync` returns created model with Id | ✅ DONE | `DatabaseDataService.cs` |
| 5 | ViewModels consume return values, insert at index 0 | ⚠️ PARTIAL | StudentsView + AssessmentsView done; others pending |
| 6 | Teacher entity + CRUD (service layer) | ✅ DONE | `IDataService.cs:74-78`, `DatabaseDataService.cs` |
| 7 | Print/report cards read from DB (`GetGradebookAsync`) | ✅ DONE | `ReportCardsViewModel` |
| 8 | Marks CRUD service methods (`UpdateMarkAsync` upserts, deletes) | ✅ DONE | `DatabaseDataService.cs:99-140` |
| 9 | Fee payment create (`CreateFeePaymentAsync`) | ✅ DONE | `DatabaseDataService.cs:71-97` |
| 10 | Fee payment reads (`GetFeePaymentsAsync`) | ✅ DONE | `DatabaseDataService.cs` + `FinancialModels.cs` |
| 11 | Students ordered by `CreatedAt desc` | ✅ DONE | `DatabaseDataService.cs:613` |
| 12 | Moderation verify/publish service methods | ✅ DONE | `VerifyAssessmentAsync`, `PublishAssessmentAsync` |
| 13 | Term lifecycle (create→activate, deactivate others) | ✅ DONE | `DatabaseDataService.cs:164-199` |
| 14 | Diagnostics logging for DB init errors | ✅ DONE | Temp log on failure |
| 15 | Navigation shell with all page routes | ✅ DONE | `ShellView.xaml.cs` Routes dictionary |
| 16 | Student termination + audit log | ✅ DONE | `TerminateStudentAsync` transactional |
| 17 | Class/Subject management | ✅ DONE | Create, delete, assign, remove |

---

## 4. WHAT'S NOT DONE (Gaps)

### 4.1 Build Blockers
- [ ] **Add `using System.Linq;` to `ClassesView.xaml.cs`** — restores green build

### 4.2 Assessment Creation from UI (Phase 2)
- `AssessmentsViewModel.NewAssessment()` is a **stub** — just sets a status message string, never calls `CreateAssessmentAsync`
- Need: ContentDialog with name/class/subject/weight/due-date fields → call `_dataService.CreateAssessmentAsync(item)` → insert returned item at index 0

### 4.3 Dashboard (Phase 1 leftover)
- `DashboardViewModel` has **hardcoded** AI insights and recent activity
- KPIs partially wired (student count + assessment count from DB; attendance/revenue/avg score are "—")
- `SearchText` filtering logic is implemented but collection observer pattern is inverted (`FilteredAiInsights`/`FilteredRecentActivity` are `ObservableCollection` but `OnPropertyChanged` fires instead of `OnCollectionChanged` — cosmetic issue)

### 4.4 Budget (Phase 5)
- `BudgetViewModel` is entirely **hardcoded sample data** — no DB entity for budget lines
- Export and AddLineItem are stubs

### 4.5 Integration Tests
- `Tests/` contains only `verify_startup.ps1` — no xUnit/NUnit tests

### 4.6 MockDataService Cleanup
- `Services/MockDataService.cs` and `AutoTable/Services/MockDataServiceAdapter.cs` exist as dead code — only used when `AUTOTABLE_DEMO_MODE=true` env var is set

---

## 5. FULL APPLICATION ARCHITECTURE

```
AutoTable/
├── App.xaml(.cs)                    — Startup, DB init, service registration
├── AppServices.cs                   — Static IDataService holder
├── AutoTable.csproj                 — .NET 8, WinUI 3, EF Core SQLite
│
├── AutoTable/Data/
│   ├── AppDbContext.cs              — EF Core DbContext (17 DbSets)
│   ├── SeedData.cs                  — Seed logic (NOT called by design)
│   ├── Entities/StudentEntity.cs    — ALL EF entities (16 entity classes)
│   └── Migrations/                  — 1 migration present
│
├── AutoTable/Services/
│   ├── IDataService.cs              — 50+ async methods interface
│   ├── DatabaseDataService.cs       — EF-backed implementation
│   └── MockDataServiceAdapter.cs    — In-memory demo adapter
│
├── AutoTable/Models/                — DTOs: Student, Teacher, ClassInfo, TermFee, etc.
├── AutoTable/ViewModels/            — ClassesViewModel, StudentsViewModel
├── AutoTable/Views/                 — StudentsView, EnrollmentView, AuditLogView, EnrollmentFormView
│
├── Models/                          — AssessmentItem, FinancialModels, PerformanceModels, etc.
├── ViewModels/                      — 21 ViewModels covering all pages
├── Views/                           — 19 Views (pages) + Controls/
├── Services/                        — Auth, Navigation, Session, Theme, MockDataService
├── Commands/RelayCommand.cs         — Custom relay command
├── Converters/                      — 6 value converters (DateFormat, Currency, Percent, etc.)
├── Resources/DesignTokens.xaml      — Full theme system (Light/Dark)
└── Tests/                           — verify_startup.ps1 only
```

### Entity Model (16 entities in `StudentEntity.cs`)
Student, Class, Stream, Subject, ClassSubject, ClassStream, Term, AcademicYear, Assessment, Mark, FeePayment, User, TerminationLog, Enrollment, TermFee + ClassTeacher FK on Class

### Key Service Methods (IDataService)
| Domain | Methods |
|--------|---------|
| Students | CRUD, terminate, enrollment, stream assignment |
| Assessments | CRUD, get by name/class/subject, verify, publish |
| Marks | Update (upsert), delete, completion recompute |
| Gradebook | Full gradebook query with class/subject/year/term/stream filters |
| Classes | Create, delete, assign/remove subjects and streams |
| Subjects | Create, delete, assign to class |
| Terms | Create, update, delete (lifecycle management) |
| Fees | Create payment, get payments, get/set term fees |
| Teachers | Full CRUD (backed by UserEntity role="Teacher") |
| Lookups | Terms, years, streams, all students |

### Pages & Their Wiring Status

| Page | View | ViewModel | DB Wired? | Notes |
|------|------|-----------|-----------|-------|
| Login | ✅ | ✅ | N/A | In-memory auth |
| SignUp | ✅ | ✅ | N/A | Creates in-memory user |
| Dashboard | ✅ | ⚠️ Partial | KPIs partial, insights hardcoded | Needs full DB wiring |
| Assessments | ✅ | ⚠️ Stub create | Read ✅, Create ❌ | NewAssessment is a stub |
| Marks Entry | ✅ | ✅ | Full DB | SaveDraft/SubmitMarks persist |
| Gradebook | ✅ | ✅ | Full DB | Reads from GetGradebookAsync |
| Student Performance | ✅ | ✅ | Full DB | Uses GetStudentPerformanceDetailAsync |
| Analytics | ✅ | ✅ | Full DB | Class breakdowns from gradebook |
| Moderation | ✅ | ✅ | Full DB | ApproveAll/Publish persist via service |
| Report Cards | ✅ | ✅ | Full DB | Reads from GetGradebookAsync |
| AI Insights | ✅ | ✅ | Full DB | Alerts + recommendations from live data |
| Teachers | ✅ | ✅ | Full DB | Add/Delete work; no edit dialog |
| Students | ✅ | ✅ | Full DB | CRUD + termination |
| Classes | ✅ | ✅ | Full DB | Create/delete classes, subjects, streams |
| Enrollment | ✅ | ✅ | Full DB | Multi-step form |
| Audit Log | ✅ | ✅ | Full DB | Reads termination log |
| Term Management | ✅ | ✅ | Full DB | Create/update/delete terms |
| Financial Dashboard | ✅ | ✅ | Full DB | KPIs + recent transactions from DB |
| Fee Collection | ✅ | ✅ | Full DB | Real payments, RecordPayment works |
| Budget | ✅ | ❌ Hardcoded | Sample data | No budget entity |

---

## 6. WAY FORWARD — PRIORITIZED PLAN

### Phase 0: Restore Build (P0 — do first)
**Goal:** Green build  
**Effort:** 1 line change

1. Add `using System.Linq;` to `Views/ClassesView.xaml.cs`
2. Verify build: `dotnet build AutoTable.csproj -p:Platform=x64 --no-restore`

### Phase 1: Complete Assessment Creation (P0 — core functionality)
**Goal:** Admins can create assessments from the UI  
**Effort:** ~2 hours

1. Make `AssessmentsViewModel.NewAssessment()` async and implement ContentDialog:
   - Fields: Name, Class (picker), Subject (picker), Weight%, DueDate, IsClassWide toggle
   - Resolve ClassId/SubjectId/TermId/AcademicYearId from lookups
   - Call `_dataService.CreateAssessmentAsync(item)` 
   - Insert returned item at index 0 of `Assessments` collection
   - Call `RefreshCounts()`
2. Add error handling dialog for missing class/subject/year/term prerequisite
3. Update `AssessmentsView.xaml` to bind `NewAssessmentCommand` to the button

### Phase 2: Dashboard DB Wiring (P1 — polish)
**Goal:** Dashboard KPIs and activity feed from live data  
**Effort:** ~1 hour

1. `DashboardViewModel.LoadKpiMetricsAsync()` — add:
   - Average score: `GetGradebookAsync` across all classes → average of averages
   - Attendance: placeholder until attendance tracking entity exists
   - Revenue: `GetFeePaymentsAsync` → sum
2. `DashboardViewModel.LoadAiInsights()` — replace hardcoded strings:
   - Query `GetGradebookAsync` for at-risk students
   - Query `GetAssessmentsAsync` for pending/published status
3. `DashboardViewModel.LoadRecentActivity()` — replace hardcoded strings:
   - Query `GetTerminationLogAsync` for recent terminations
   - Query `GetFeePaymentsAsync` for recent payments
   - Query `GetAssessmentsAsync` for recently created assessments

### Phase 3: Budget Entity + Wiring (P2 — new feature)
**Goal:** Persist budget lines in DB  
**Effort:** ~3 hours

1. Add `BudgetEntity` to EF model (Category, Budgeted, Spent, Year)
2. Add `DbSet<BudgetEntity>` to `AppDbContext`
3. Add `GetBudgetLinesAsync` / `CreateBudgetLineAsync` / `UpdateBudgetLineAsync` to `IDataService` + `DatabaseDataService`
4. Wire `BudgetViewModel` to service instead of hardcoded data
5. Implement Export (CSV/Excel) and AddLineItem ContentDialog

### Phase 4: Teachers Edit Dialog (P1 — feature completeness)
**Goal:** Edit existing teachers  
**Effort:** ~1 hour

1. Add `UpdateTeacherAsync` to `IDataService` (already exists — verify implementation)
2. Add Edit button to `TeachersView.xaml` grid
3. Implement `TeachersViewModel.EditTeacherAsync(Teacher)` 
4. Add ContentDialog pre-filled with existing teacher data

### Phase 5: Integration Tests (P3 — quality)
**Goal:** Basic CRUD integration tests  
**Effort:** ~3 hours

1. Create `Tests/AutoTable.IntegrationTests/AutoTable.IntegrationTests.csproj`
2. Use SQLite `:memory:` with connection kept open + `PRAGMA foreign_keys=ON`
3. Test cases:
   - Create student → appears in list
   - Create assessment → appears, has Id
   - Enter mark → gradebook reflects it
   - Create term → exactly one active term
   - Terminate student → appears in audit log

### Phase 6: Mock Cleanup + Documentation (P4 — housekeeping)
**Effort:** ~1 hour

1. Move `MockDataService.cs` and `MockDataServiceAdapter.cs` under `Demo/` folder
2. Update `CONTEXT_REPORT.md` and `AGENT_CONTEXT.md` with accurate status
3. Remove stale build output files (`build_current.txt`, `build_enrollment.txt`, etc.)

---

## 7. RISKS & OPEN QUESTIONS

| Risk | Mitigation |
|------|------------|
| **Empty database UX** — Assessment creation throws if no class/subject/year/term exist | Surface a friendly error dialog: "Configure Classes & Terms first" |
| **Cascading build failures** — one C# error kills all XAML compilation | Fix P0 immediately; keep build green as discipline |
| **Dual tree layout** — root `Views/` + `AutoTable/Views/` with same namespaces | Consolidation recommended in future cleanup |
| **No seed data** — DB starts empty; features throw on empty DB | Consider optional seed for onboarding (or make all features graceful on empty) |
| **Budget is entirely fake** — no entity, no persistence | Phase 3 adds entity; mark as "coming soon" in UI until then |
| **Large print exports** — ReportCards could be large | Add pagination or streaming for production use |
| **Disk space** — WinUI XAML compiler fails confusingly when disk is full | Keep ≥1 GB free on C: drive |

---

## 8. RECOMMENDED IMMEDIATE NEXT STEPS

1. **Fix the build** — add `using System.Linq;` to `ClassesView.xaml.cs`
2. **Implement assessment creation** — wire `NewAssessment` to `CreateAssessmentAsync`
3. **Wire dashboard to DB** — replace hardcoded insights/activity with live queries
4. **Verify build + run** — `dotnet build -p:Platform=x64 --no-restore` then launch app

---

*Generated 23 Aug 2026 by Buffy (Codebuff agent)*
