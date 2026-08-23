# AutoTable — Way-Forward Plan

Generated 22 Aug 2026 from ground-truth exploration of the working tree plus a live
`dotnet build AutoTable.csproj -p:Platform=x64` run. Where this document conflicts with
`CONTEXT_REPORT.md` (§3–§5) or `AGENT_CONTEXT.md`, **this document reflects the actual
code/build state** and should be treated as authoritative.

---

## 1. Executive Summary

The operational goal — *single-source-of-truth SQLite persistence for every CRUD operation* — is
roughly **60% implemented at the data/service layer**, but the working tree **does not currently
compile**, and most of the Phase 2/4/5 UI wiring that would make the app actually persist user
actions is still missing.

| Area | Status | Evidence |
|---|---|---|
| Persistent DB (`%LOCALAPPDATA%\AutoTable\autotable.db`) + dir creation | ✅ DONE | `App.xaml.cs:62-67` |
| `AUTOTABLE_DEV_EPHEMERAL_DB` / `AUTOTABLE_DEMO_MODE` flags | ✅ DONE in code | `App.xaml.cs:54-76` |
| Fail-loud startup, diagnostics log, no silent mock fallback | ✅ DONE | `App.xaml.cs:239-266` |
| Teacher CRUD (service layer, `UserEntity role="Teacher"`) | ✅ DONE | `IDataService.cs:74-78`, `DatabaseDataService.cs:22-58` |
| Marks CRUD service methods (`UpdateMarkAsync` upserts, deletes, completion recompute) | ✅ DONE | `DatabaseDataService.cs:99-140` |
| Fee payment create (`CreateFeePaymentAsync`) | ✅ DONE | `DatabaseDataService.cs:71-97` |
| Students ordered by `CreatedAt desc` | ✅ DONE | `DatabaseDataService.cs:613` |
| Print / report cards read from DB (`GetGradebookAsync`) | ✅ DONE | per CONTEXT_REPORT §3 |
| Term lifecycle (create→activate, deactivate others; startup housekeeping) | ✅ DONE | `DatabaseDataService.cs:164-199`, `App.xaml.cs:194-233` |
| **Build** | ❌ **BROKEN — 9 errors** | live `dotnet build` output |
| Assessment creation from the Assessments UI | ❌ stub only | `AssessmentsViewModel.cs:48-49` |
| Marks save/submit persistence from Marks Entry UI | ❌ UI-only | `MarksEntryViewModel.cs:75-94` |
| Moderation approve/publish persistence | ❌ in-memory / empty stub | `ModerationViewModel.cs:71-81` |
| AI Insights derived from DB | ❌ hardcoded | `AiInsightsViewModel.cs:29-48` |
| Financial dashboard KPIs from DB | ❌ hardcoded | `FinancialsDashboardViewModel.cs:9-23` |
| Budget lines from DB | ❌ hardcoded | `BudgetViewModel.cs:38-53` |
| Fee collection list from real payments | ❌ fabricated (RNG) | `FeeCollectionViewModel.cs:93-114` |
| `GetFeePaymentsAsync` read API | ❌ missing | full read of `IDataService.cs` / `DatabaseDataService.cs` |
| Integration tests (SQLite in-memory) | ❌ none | `Tests/` has only `verify_startup.ps1` |

**Priority order:** P0 green the build → P1 Phase 2 (marks/assessments wiring) → P2 Phase 4
(moderation + AI) → P3 Phase 5 (financials) → P4 cleanup/tests/docs.

---

## 2. Ground-truth status vs OPERATIONAL_PLAN.md concrete steps

| # | Step | Status | Notes |
|---|---|---|---|
| 1 | Persist DB to LocalApplicationData, ensure dir | ✅ DONE | `App.xaml.cs:62-67` |
| 2 | `DEV_EPHEMERAL_DB` flag for ephemeral dev mode | ✅ DONE | env var `AUTOTABLE_DEV_EPHEMERAL_DB` → `%TEMP%\autotable_ephemeral.db`. CONTEXT_REPORT wrongly lists this as NOT DONE. |
| 3 | Fail loudly on DB init error; explicit demo mode only | ✅ DONE | error dialog + `%TEMP%\autotable_init_error.txt` + abort; mock adapter registered only when `AUTOTABLE_DEMO_MODE=true` |
| 4 | `CreateAssessmentAsync` returns created model | ✅ DONE | returns `Task<AssessmentItem>` with Id/metadata (`DatabaseDataService.cs:1039-1080`) |
| 5 | ViewModels consume return values, insert at index 0 | ⚠️ PARTIAL | Enrollment/Students wired; **AssessmentsViewModel.NewAssessment is a stub that never calls Create** |
| 6 | Teacher entity + IDataService methods + migration | ✅ DONE (service) | backed by `UserEntity role="Teacher"`; **View page still missing (see P0)** |
| 7 | Printing reads from IDataService queries | ✅ DONE | ReportCards uses `GetGradebookAsync` |
| 8 | Missing CRUD endpoints (marks, fees) | ⚠️ PARTIAL | service side done; **ViewModel wiring missing** (MarksEntry), fee *read* API missing |
| 9 | Integration tests (SQLite in-memory) | ❌ NOT DONE | |
| 10 | Diagnostics logging for DB init errors | ✅ DONE | temp log on failure |
| 11 | Clean up MockDataServiceAdapter | ⚠️ PARTIAL | not wired except explicit demo mode; dead files remain at root `Services/MockDataService.cs` and `AutoTable/Services/MockDataServiceAdapter.cs` |
| 12 | Manual QA checklist / UI wiring audit | ❌ NOT DONE | blocked by broken build |

### Agreed roadmap phase status (CONTEXT_REPORT §5)

- **Phase 2** — Assessments create: service ✅ / VM ❌ · Marks persistence: service ✅ / VM ❌ · Students sort ✅
- **Phase 4** — Moderation verify/publish: service methods ❌ don't exist yet, VM ❌ · AI Insights ❌
- **Phase 5** — `GetFeePaymentsAsync` ❌ · FinancialsDashboard ❌ · FeeCollection list ❌ (RecordPayment ✅) · Budget ❌
- **Then** — Teacher CRUD service ✅ (View pending) · Migrations verification ⚠️ · Mock cleanup ⚠️ · Tests ❌

---

## 3. Discrepancies between the reports and reality

1. **“dotnet build … Passed” claims are false for the current tree.** A fresh build fails with
   9 errors (see §4). Both `build_final.txt` and the live rebuild confirm failure.
2. **AGENT_CONTEXT.md is stale on two counts:** it says the DB is deleted/recreated each start in
   Temp (actually persistent LocalApplicationData now), and that “many ViewModels still use
   `MockDataService.Instance`” (13 ViewModels verified to use `AppServices.DataService`; none use mock).
3. **CONTEXT_REPORT §3 says Teacher CRUD is NOT DONE** while its own iteration log (§8,
   step-2 entry) says it was added. Code confirms: **it IS done** at the service layer.
4. **CONTEXT_REPORT says DEV_EPHEMERAL_DB is NOT DONE**; code shows it IS implemented.
5. **Teachers feature is half-scaffolded and actively breaking the build**: `TeachersViewModel`
   exists, ShellView routes to `typeof(TeachersView)`, but **no TeachersView page exists anywhere**
   (checked root `Views/` and `AutoTable/Views/`).

---

## 4. Build diagnosis (P0 BLOCKER — fix first)

### Live build symptoms (9 errors)

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

### P4 — Remaining operational-plan items

1. **Teacher UI completion** — covered by P0 step 1 (page creation); optionally extend
   `ClassesViewModel` to assign teachers to classes later.
2. **EF Migrations decision** — startup currently hand-patches schema with `ALTER TABLE` blocks
   (`App.xaml.cs:90-191`). Either (a) accept `EnsureCreated` + these compat patches for now and
   document them, or (b) move to real EF migrations and delete the patches. Recommend (a) short-term,
   revisit before any production deploy.
3. **Mock cleanup** — move `Services/MockDataService.cs` and `AutoTable/Services/MockDataServiceAdapter.cs`
   under a `Demo/` folder/namespace (or delete if demo mode is dropped). They are currently dead code
   outside `AUTOTABLE_DEMO_MODE=true`.
4. **Integration tests (Step 9)** — new `Tests/AutoTable.IntegrationTests` project using SQLite
   `:memory:` (keep connection open, `PRAGMA foreign_keys=ON`, `EnsureCreated`). Flows:
   create student → appears in list; create assessment → appears; enter mark → gradebook/report reflect it;
   create term → exactly one active term.
5. **Docs sync** — update `CONTEXT_REPORT.md` §3/§5 statuses (DEV_EPHEMERAL_DB done, teacher CRUD done)
   and rewrite stale sections of `AGENT_CONTEXT.md` (persistent DB, no mock usage) so future agents
   don't chase phantom gaps.

---

## 6. Risks & open questions

- **Build-first discipline:** every phase's "verify" depends on a green build; do not stack features
  on top of the current red state.
- **Empty-database UX:** by design there is no seeding; assessment creation throws when
  class/subject/year/term are missing. The UI must translate that into guidance ("configure Classes &
  Terms first") rather than a raw exception.
- **Demo vs production data:** default DB is persistent; developers testing destructive flows should
  set `AUTOTABLE_DEV_EPHEMERAL_DB=true` or `AUTOTABLE_DEMO_MODE=true` to avoid polluting real data.
- **MVVMTK0007 exact trigger unconfirmed** — try the non-nullable param fix first; fall back to the
  parameterless-command pattern (guaranteed compatible).
- **Dual tree layout** (root `Views/`,`ViewModels`,`Services` + `AutoTable/…` subtree, same namespaces)
  compiles today because file sets are disjoint, but it is fragile — consider consolidating in a
  future cleanup pass.
- **Large print exports** (ops-plan risk) still unaddressed; paginate/stream when wiring bulk report cards.

---

## Suggested execution order (atomic commits)

1. P0: add TeachersView page + fix TeachersViewModel command → green build (verify with dotnet build).
2. P1a: assessment create dialog + insert-at-0.
3. P1b: marks save/submit persistence + error dialogs.
4. P2a/b: verify/publish service methods + Moderation wiring.
5. P2c: AI insights from gradebook queries.
6. P3a–d: fee read API + dashboard/fee-collection/budget rewiring.
7. P4: tests, mock cleanup, docs sync.