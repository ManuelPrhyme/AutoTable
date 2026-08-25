# AutoTable — Way-Forward Plan

Generated 22 Aug 2026; updated 24 Aug 2026. Where this document conflicts with
`CONTEXT_REPORT.md` (§3–§5) or `AGENT_CONTEXT.md`, **this document reflects the actual
code/build state** and should be treated as authoritative.

---

## 1. Executive Summary

The operational goal — *single-source-of-truth SQLite persistence for every CRUD operation* — is
**fully implemented**, the working tree **compiles cleanly (0 errors)**,
and all Phase 2/4/5 UI wiring is complete. Integration tests (9 passing) provide regression coverage.

Since 23 Aug the app gained two major features: **native Windows A4 report-card printing**
(promotional + contributory assessments, bio data, teacher comments, batch print via PrintManager)
and a complete **grading-systems domain** (named scales with promotion/repeat bands), integrated
into a fully reworked Class Management page where class creation happens in one modal.

| Area | Status | Evidence |
|---|---|---|
| Persistent DB (`%LOCALAPPDATA%\AutoTable\autotable.db`) + dir creation | ✅ DONE | `App.xaml.cs:62-67` |
| `AUTOTABLE_DEV_EPHEMERAL_DB` / `AUTOTABLE_DEMO_MODE` flags | ✅ DONE in code | `App.xaml.cs:54-76` |
| Fail-loud startup, diagnostics log, no silent mock fallback | ✅ DONE | `App.xaml.cs:239-266` |
| Teacher CRUD (service layer, `UserEntity role="Teacher"`) | ✅ DONE | `IDataService.cs:74-78`, `DatabaseDataService.cs:22-58` |
| Marks CRUD service methods (`UpdateMarkAsync` upserts, deletes, completion recompute) | ✅ DONE | `DatabaseDataService.cs:99-140` |
| Fee payment create (`CreateFeePaymentAsync`) | ✅ DONE | `DatabaseDataService.cs:71-97` |
| Students ordered by `CreatedAt desc` | ✅ DONE | `DatabaseDataService.cs:613` |
| Term lifecycle (create→activate, deactivate others; startup housekeeping) | ✅ DONE | `DatabaseDataService.cs:164-199`, `App.xaml.cs:194-233` |
| Assessment creation from the Assessments UI | ✅ DONE | `AssessmentsView.xaml.cs` code-behind creates dialog + calls `CreateAssessmentAsync` |
| Marks save/submit persistence from Marks Entry UI | ✅ DONE | `MarksEntryViewModel.cs` calls `UpdateMarkAsync` |
| Moderation approve/publish persistence | ✅ DONE | `ModerationViewModel.cs` — persists via `VerifyAssessmentAsync`/`PublishAssessmentAsync` |
| AI Insights derived from DB | ✅ DONE | `AiInsightsViewModel.cs` — gradebook + assessment derived |
| Financial dashboard KPIs from DB | ✅ DONE | `FinancialsDashboardViewModel.cs` — real FeePayments/TermFees |
| Budget lines from DB | ✅ DONE | `BudgetViewModel.cs` — BudgetLineEntity in schema, AddLineItem + Export |
| Fee collection list from real payments | ✅ DONE | `FeeCollectionViewModel.cs` — real payments, no RNG |
| Integration tests (SQLite in-memory) | ✅ DONE | `Tests/AutoTable.IntegrationTests/` — 9 tests, all passing |
| Mock cleanup | ✅ DONE | Moved to `Demo/` folder, namespace `AutoTable.Demo` |
| **A4 report-card sheet + native Windows printing** | ✅ DONE (24 Aug) | `Views/Controls/ReportCardSheetView.xaml`, `ReportCardsView.xaml.cs` PrintManager pipeline |
| **Grading systems (entities, service CRUD, schema patches)** | ✅ DONE (24 Aug) | `AutoTable/Models/GradingSystemModels.cs`, `IDataService.cs`, `DatabaseDataService.cs`, `App.xaml.cs` startup patches |
| **Class creation single-modal rework** (name + any teacher incl. student teachers + grading system pick-or-create + streams/subjects assign-or-create) | ✅ DONE (24 Aug) | `Views/ClassesView.xaml(.cs)`, `AutoTable/ViewModels/ClassesViewModel.cs`; select-class detail card removed |

**Remaining gap:** EF migrations verification (startup uses EnsureCreated + ALTER TABLE patches; acceptable for now).

**Next feature backlog** (documented in `OPERATIONAL_PLAN.md`): promotion/repeat flow with Term-3 promotional exams; per-assessment promotion-role checkboxes (none / contributory / promotional); admin role-gating for Term/Class/Budget pages; defaulters/cohort finance analytics; mid-term slips; global active-term enforcement.

---

## 2. Ground-truth status vs OPERATIONAL_PLAN.md concrete steps

| # | Step | Status | Notes |
|---|---|---|---|
| 1 | Persist DB to LocalApplicationData, ensure dir | ✅ DONE | `App.xaml.cs:62-67` |
| 2 | `DEV_EPHEMERAL_DB` flag for ephemeral dev mode | ✅ DONE | env var `AUTOTABLE_DEV_EPHEMERAL_DB` → `%TEMP%\autotable_ephemeral.db`. CONTEXT_REPORT wrongly lists this as NOT DONE. |
| 3 | Fail loudly on DB init error; explicit demo mode only | ✅ DONE | error dialog + `%TEMP%\autotable_init_error.txt` + abort; mock adapter registered only when `AUTOTABLE_DEMO_MODE=true` |
| 4 | `CreateAssessmentAsync` returns created model | ✅ DONE | returns `Task<AssessmentItem>` with Id/metadata (`DatabaseDataService.cs:1039-1080`) |
| 5 | ViewModels consume return values, insert at index 0 | ✅ DONE | Enrollment/Students/Teachers/Assessments all wired |
| 6 | Teacher entity + IDataService methods + migration | ✅ DONE | Full CRUD with Add/Edit/Delete UI and two-column modal |
| 7 | Printing reads from IDataService queries | ✅ DONE | ReportCards uses `GetGradebookAsync` |
| 8 | Missing CRUD endpoints (marks, fees) | ✅ DONE | MarksEntryViewModel persists via UpdateMarkAsync; FeeCollection uses real payments |
| 9 | Integration tests (SQLite in-memory) | ✅ DONE | 9 tests in Tests/AutoTable.IntegrationTests |
| 10 | Diagnostics logging for DB init errors | ✅ DONE | temp log on failure |
| 11 | Clean up MockDataServiceAdapter | ✅ DONE | moved to Demo/ folder, namespace AutoTable.Demo |
| 12 | Manual QA checklist / UI wiring audit | ✅ DONE | build green, all CRUD paths wired |
| 13 | Budget entity + wiring | ✅ DONE | BudgetLineEntity, service CRUD, BudgetViewModel wired, AddLineItem + Export |

### Agreed roadmap phase status (CONTEXT_REPORT §5)

- **Phase 2** — Assessments create: service ✅ / UI ✅ · Marks persistence: service ✅ / VM ✅ · Students sort ✅
- **Phase 4** — Moderation verify/publish: service ✅ / VM ✅ · AI Insights ✅
- **Phase 5** — `GetFeePaymentsAsync` ✅ · FinancialsDashboard ✅ · FeeCollection ✅ · Budget ✅ (entity + wiring done)
- **Then** — Teacher CRUD ✅ (service + UI) · Migrations verification ⚠️ · Mock cleanup ✅ (moved to Demo/) · Tests ✅ (9 passing)

---

## 3. Discrepancies between the reports and reality

> **UPDATE (24 Aug 2026):** every discrepancy listed below was subsequently verified and fixed.
> The build is green; the Teachers page exists and routes; all ViewModels use `AppServices.DataService`.
> Items kept for historical context.

1. **“dotnet build … Passed” claims are false for the current tree.** ✅ RESOLVED — fresh builds now pass with 0 errors (verified repeatedly, most recently 24 Aug).
2. **AGENT_CONTEXT.md is stale on two counts:** it says the DB is deleted/recreated each start in
   Temp (actually persistent LocalApplicationData now), and that “many ViewModels still use
   `MockDataService.Instance`” (13 ViewModels verified to use `AppServices.DataService`; none use mock).
3. **CONTEXT_REPORT §3 said Teacher CRUD is NOT DONE** while its own iteration log says it was added. Code confirmed: **it IS done** at the service layer. Report has since been corrected.
4. **CONTEXT_REPORT said DEV_EPHEMERAL_DB is NOT DONE**; code shows it IS implemented. Corrected.
5. **Teachers feature half-scaffolded / breaking the build**: ✅ RESOLVED — `Views/TeachersView.xaml(.cs)` exists, route compiles, full Add/Edit/Delete modal shipped.

---

## 4. Build diagnosis (P0 BLOCKER — ~~fix first~~ RESOLVED)

> **UPDATE (24 Aug 2026):** the MVVMTK0007 command-generation error and the missing TeachersView
> were fixed; the six `WMC0001 Unknown type '<Converter>'` errors cleared as a cascade once the
> C# compile succeeded again. A later corruption (a literal `\n` at `AppDbContext.cs:24` plus a
> duplicated `GradeBandEntity`) was also repaired. Current state: **0 errors** on
> `dotnet build AutoTable.csproj -p:Platform=x64`.

```
ViewModels/TeachersViewModel.cs(30,27): error MVVMTK0007: CreateTeacherAsync(string, string?)
    cannot be used to generate a command property …
Views/ShellView.xaml.cs(50,45): error CS0246: 'TeachersView' could not be found
App.xaml(14..19): XamlCompiler error WMC0001: Unknown type '<DateFormatConverter, CurrencyConverter,
    PercentConverter, DecimalConverter, GradeColorConverter, StatusColorConverter>'
    in XML namespace 'using:AutoTable.Converters'
Xaml Internal Error WMC9999: Object reference not set…
warning WMC1509: No LocalAssembly parameter given during MarkupCompilePass2
```

### Root cause analysis

- The 6 converters referenced by `App.xaml` **all exist** with correct namespace and class names
  (`Converters/FormatConverters.cs`, `Converters/DateFormatConverter.cs`). They are not really missing.
- `WMC1509 "No LocalAssembly"` means the XAML compiler ran without the project assembly — which
  happens because **C# compilation failed**, so no assembly was produced for MarkupCompilePass2 to
  resolve local types.
- Cross-check: the older `build_final.txt` shows the identical converter cascade alongside a
  *different* set of C# errors (CS4036 in ClassesView — since fixed by adding
  `using System.Runtime.InteropServices.WindowsRuntime;`). The cascade follows whatever C# errors exist.
- **Conclusion: the true blockers are exactly two C# errors.** Fixing them should clear all 7 XAML errors.

### P0 fix plan

1. **Create the missing Teachers page** — add `Views/TeachersView.xaml` +
   `Views/TeachersView.xaml.cs` (namespace `AutoTable.Views`, a `Page` bound to
   `TeachersViewModel`) so `ShellView`'s route compiles. This also completes the half-built
   Teachers feature (VM + route already exist).
2. **Fix MVVMTK0007** on `[RelayCommand] public async Task CreateTeacherAsync(string fullName, string? email)`:
   - Preferred robust fix: make the command parameterless and bind inputs to
     `[ObservableProperty] NewTeacherName` / `NewTeacherEmail`; guard inside the method.
     (`Task Method()` is trivially compatible.)
   - Alternative one-line attempt first: change `string? email` → non-nullable `string email`.
   - Rebuild after either change to confirm which resolves it.
3. **Rebuild and verify** the 6 converter errors clear as a cascade:
   `dotnet build AutoTable.csproj -p:Platform=x64` → expect 0 errors.
   If they persist, investigate XAML `LocalAssembly` wiring separately (unlikely).

---

## 5. Implementation roadmap (what & how)

### P1 — Phase 2: complete marks & assessments write paths

**(a) Wire assessment creation from the UI**
- File: `ViewModels/AssessmentsViewModel.cs` (+ `Views/AssessmentsView.xaml(.cs)`).
- Today `NewAssessment()` only sets a StatusMessage string; it never calls the service.
- Implement a real create dialog (name, class, subject, weight, due date, stream/class-wide),
  call `_dataService.CreateAssessmentAsync(item)`, then **insert the returned item at index 0**
  of `Assessments` and call `RefreshCounts()`. Show an error dialog on failure
  (note: creation throws if class/subject/year/term are absent — surface that message).

**(b) Wire marks save/submit to the database**
- Files: `ViewModels/MarksEntryViewModel.cs`.
- Convert `SaveDraft` / `SubmitMarks` from `void` to async commands.
- For each edited row call `_dataService.UpdateMarkAsync(assessmentId, studentId, mark, grade, remarks)`
  (already upserts + recomputes `MarksEnteredPercent` via `UpdateAssessmentCompletionAsync`).
- Resolve the numeric `assessmentId` via `_dataService.GetAssessmentAsync(name, class, subject)`
  before saving. On submit, require 100% completion (existing check) and refresh the row set /
  assessments list afterwards. Add try/catch with a ContentDialog error surface.

**(c) Students ordering** — already done (`OrderByDescending(s => s.CreatedAt)`). No work.

### P2 — Phase 4: moderation + AI insights

**(a) Add moderation service APIs** (missing today)
- `IDataService` + `DatabaseDataService`: add
  - `Task VerifyAssessmentAsync(int assessmentId, bool verified);`
  - `Task PublishAssessmentAsync(int assessmentId, bool published);`
  (simple `FindAsync` + set `IsVerified`/`IsPublished` + SaveChanges; optionally record an audit note).

**(b) Wire ModerationViewModel**
- `ApproveAll`: make async; loop pending items, resolve ids, call `VerifyAssessmentAsync(id, true)`,
  update item status, refresh counters; wrap in try/catch with dialog.
- `Publish`: currently an empty `{}` — implement as above with `PublishAssessmentAsync(id, true)`,
  gated on admin role like other admin actions.

**(c) Wire AiInsightsViewModel to real data**
- Replace hardcoded alerts: iterate classes × subjects via `GetGradebookAsync(...)`, flag students
  with average < 40 as at-risk alerts; derive recommendations from unpublished/incomplete
  assessments (`GetAssessmentsAsync`). Keep the Automations catalog as-is (feature list, not data).

### P3 — Phase 5: financials from the database

**(a) Add the missing fee READ API**
- `IDataService` + `DatabaseDataService`: add
  `Task<IReadOnlyList<FeePaymentSummary>> GetFeePaymentsAsync(int? classId = null, int? termId = null);`
  returning student name/LIN, amount, date, term, description (join FeePayments↔Students↔Terms).
  Add a small `FeePaymentSummary` model alongside existing `FinancialModels`.

**(b) FinancialsDashboardViewModel**
- Inject `AppServices.DataService`; compute TotalCollected = Σ payments, OutstandingFees =
  Σ(TermFee per class×term) − collected, StudentsPaidPercent = distinct payers ÷ active students;
  populate RecentTransactions from the latest N payments (format dates client-side).

**(c) FeeCollectionViewModel.Load — remove fabricated data**
- Today it builds rows from `GetStudentMarksAsync(...)` and assigns paid amounts via
  `new Random(...).Next(0,3)` — pure fiction. Replace with: students of the selected class
  (`GetStudentsAsync` filtered) × expected amount from `GetTermFeesAsync` (per class+term),
  actual paid from `GetFeePaymentsAsync(classId, termId)`; compute Balance = Expected − Paid.

**(d) BudgetViewModel**
- Lower priority (config-driven): source budget lines from `TermFees`/a new budget table or keep
  static but clearly labelled as sample data until a budget entity exists. Implement or safely
  disable `Export` / `AddLineItem` stubs.

### P4 — Remaining operational-plan items (all done)

1. **Teacher UI completion** — ✅ DONE (Add/Edit/Delete with two-column modal).
2. **EF Migrations decision** — startup uses `EnsureCreated` + ALTER TABLE patches. Acceptable for now;
   revisit before production deploy.
3. **Mock cleanup** — ✅ DONE. Moved `MockDataService.cs` and `MockDataServiceAdapter.cs` to `Demo/`
   folder with namespace `AutoTable.Demo`.
4. **Integration tests (Step 9)** — ✅ DONE. `Tests/AutoTable.IntegrationTests` with 9 tests:
   Student CRUD, Term lifecycle, Assessment creation, Marks upsert, Budget CRUD, Teacher CRUD,
   Fee payments, Duplicate LIN validation.
5. **Docs sync** — ✅ DONE. `CONTEXT_REPORT.md` and `WAY_FORWARD_PLAN.md` updated 24 Aug 2026.

### P5 — Next priorities (24 Aug 2026)

Features agreed in `OPERATIONAL_PLAN.md` ("Feature — Assessment promotion role & configurable
grading systems") and the Prototype roadmap, not yet implemented:

1. **Assessment promotion role** — tri-state on assessment creation (`None` / contributory /
   promotional exam); store as a column + migration; restrict "promotional exam" designation to
   Term 3 (`IsPromotionTerm`). Switch `GetReportCardSheetAsync` from its current heuristic
   classification to these explicit flags.
2. **Promotion / repeat flow** — promote/repeat decision per student; Term-3 auto-move-up;
   manual class shift; surface PASS/REPEAT verdicts already present on the report card.
3. **Grade resolution via grading systems** — replace hard-coded `GradeFromAverage`
   (`DatabaseDataService.cs`, `>=80→A … else F`) with data-driven band lookup per class's
   `GradingSystemId` (falls back to school default).
4. **Admin role-gating** — extend the Moderation gate pattern to Term Management, Classes and
   Budget pages.
5. **Defaulters / cohort finance analytics** — top-N defaulters, % paid within window,
   unpaid-above-threshold queries.
6. **Mid-term slips** — 3–4-per-A4 slip layout alongside full report cards.
7. **Active-term enforcement** — block enrollment/assessment/fee writes when no term is active.

---

## 6. Risks & open questions (updated 24 Aug)

- **Empty-database UX:** by design there is no seeding; assessment creation throws when
  class/subject/year/term are missing. The UI must translate that into guidance ("configure Classes &
  Terms first") rather than a raw exception.
- **Demo vs production data:** default DB is persistent; developers testing destructive flows should
  set `AUTOTABLE_DEV_EPHEMERAL_DB=true` or `AUTOTABLE_DEMO_MODE=true` to avoid polluting real data.
- **Dual tree layout** (root `Views/`,`ViewModels`,`Services` + `AutoTable/…` subtree, same namespaces)
  compiles today because file sets are disjoint, but it is fragile — consider consolidating in a
  future cleanup pass.
- **Report-card promotional classification is heuristic** until P5.1 lands: per subject, the
  top-weighted/latest-due paper is treated as the promotional exam. Documented in
  `GetReportCardSheetAsync`; swap to explicit flags when available.
- **EnsureCreated + ALTER TABLE patches** accumulate with each feature (latest: GradingSystems,
  GradeBands, Classes.GradingSystemId). Consolidate into EF migrations before production.

---

## Suggested execution order (atomic commits)

1. Promotion-role column + migration + creation-dialog checkboxes (P5.1) — unlocks explicit report-card classification.
2. Grade-band lookup replacing `GradeFromAverage` (P5.3) — wires the grading-systems feature built 24 Aug into marks/reporting.
3. Promotion/repeat flow (P5.2) — completes the student lifecycle.
4. Role gates (P5.4), then defaulters analytics (P5.5), then slips (P5.6) and active-term enforcement (P5.7).
5. Maintenance: EF migrations verification before any production deploy.

Historical order (all complete): Teachers page → assessment create dialog → marks persistence →
moderation wiring → AI insights → fee read API + financial rewiring → tests/mock cleanup/docs sync →
A4 report-card printing → grading systems + single-modal class creation.