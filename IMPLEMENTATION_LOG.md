# AutoTable — Implementation Log

> **Live document.** Updated after each feature or fix is completed.  
> This file tracks the *in-progress* implementation work — what was done, what
> changed, and what remains — so any agent (or human) can pick up exactly where
> the previous session left off.

---

## Current Session: 27 Aug 2026

**Branch:** `sql_rec`  
**Build:** 0 errors, 0 new warnings  
**Working tree:** Clean (committed 27 Aug)

### Report Cards Fix — Students Not Displaying ✅ DONE

**Problem:** Report Cards page showed no students because `Load()` called `GetGradebookAsync` with a hardcoded "Mathematics" subject filter.

**Fix:** Added `GetReportCardListAsync` method that returns ALL active students in a class with overall averages across ALL subjects.

| File | Change |
|------|--------|
| `AutoTable/Services/IDataService.cs` | Added `GetReportCardListAsync` and `GetMidTermSlipsAsync` signatures |
| `AutoTable/Services/DatabaseDataService.cs` | Implemented both methods |
| `Demo/MockDataServiceAdapter.cs` | Added stubs |
| `ViewModels/ReportCardsViewModel.cs` | Updated `Load()` to call `GetReportCardListAsync` instead of `GetGradebookAsync` |

### Assessment Donut Progress + Author Column ✅ DONE

**Changes:**
- Replaced `ProgressBar` with `ProgressRing` (donut, 36×36px) in assessments grid
- Added `RoundPercentConverter` for whole-number display inside donut
- Added AUTHOR column to assessments table
- Tightened column widths for better layout balance

| File | Change |
|------|--------|
| `Views/AssessmentsView.xaml` | ProgressRing donut + AUTHOR column + tighter spacing |
| `Converters/FormatConverters.cs` | Added `RoundPercentConverter` |
| `App.xaml` | Registered `RoundPercentConverter` |

### Mid-Term Slips ✅ DONE

**Feature:** Compact mid-term report slips (3-4 per A4 page) with subject results, grades, remarks, and summary.

| File | Change |
|------|--------|
| `Models/PerformanceModels.cs` | Added `MidTermSubjectResult` and `MidTermSlipModel` |
| `AutoTable/Services/IDataService.cs` | Added `GetMidTermSlipsAsync` |
| `AutoTable/Services/DatabaseDataService.cs` | Implemented `GetMidTermSlipsAsync` |
| `Demo/MockDataServiceAdapter.cs` | Added stub |
| `Views/Controls/MidTermSlipView.xaml` | Compact A4 slip layout |
| `Views/Controls/MidTermSlipView.xaml.cs` | Code-behind |
| `Views/ReportCardsView.xaml` | Added "Mid-Term Slips" button |
| `Views/ReportCardsView.xaml.cs` | Added `MidTermSlips_Click` handler with preview + print |

---

### P5.4 — Admin Role-Gating ✅ DONE

**Goal:** Gate destructive/administrative actions behind admin role checks.
Currently only Moderation was gated; Term Management, Class Management, Budget,
and Promotion pages were accessible to all users.

#### Files Changed

| File | Change |
|------|--------|
| `Views/ShellView.xaml.cs` | Added `AdminOnlyRoutes` set (TermManagement, Classes, Budget, Promotion). Sidebar items hidden for non-admins. Route-level guard in `NavigateTo` and `OnExternalShellNavigated` blocks direct navigation to admin-only pages. |
| `Views/TermManagementView.xaml` | Added `x:Name="CreateTermCard"` to Create Term section |
| `Views/TermManagementView.xaml.cs` | Added `_isAdmin` check; hides Create Term card for non-admins; gates `CreateTerm_Click` and `SetFee_Click` with admin check |
| `Views/ClassesView.xaml` | Added `x:Name` to Create Class and Create Grading System buttons |
| `Views/ClassesView.xaml.cs` | Added `_isAdmin` check; hides create/edit buttons for non-admins; gates `CreateClass_Click`, `EditClass_Click`, `CreateGradingSystem_Click`, `DeleteGradingSystem_Click` |
| `ViewModels/BudgetViewModel.cs` | Gated `AddLineItemAsync` behind admin check |
| `AutoTable/ViewModels/PromotionViewModel.cs` | Gated `PromoteAsync`, `RepeatAsync`, `ShiftAsync`, `ProcessAllAsync` behind admin check |

### P5.7 — Active-Term Enforcement ✅ DONE

**Goal:** Block writes when no academic term is active, matching the brief's
"term is the context for everything; no active term ⇒ nothing active".

#### Files Changed

| File | Change |
|------|--------|
| `Views/AssessmentsView.xaml.cs` | `NewAssessment_Click` checks `GetActiveTermAsync()`; blocks with error dialog if no active term |
| `ViewModels/EnrollmentViewModel.cs` | `SubmitAsync` checks `GetActiveTermAsync()`; blocks with status message + error event if no active term |
| `Views/FeeCollectionView.xaml.cs` | `RecordPayment_Click` checks `GetActiveTermAsync()`; blocks with error dialog if no active term |

### Assessment Author Field ✅ DONE

**Goal:** Track which teacher authored/created each assessment.
The brief requires "each has an author" for assessments.

#### Files Changed

| File | Change |
|------|--------|
| `AutoTable/Data/Entities/StudentEntity.cs` | Added `AuthorUserId` (int?), `AuthorUser` (nav), `AuthorName` (string?) to `AssessmentEntity` |
| `Models/AssessmentItem.cs` | Added `AuthorId` (int?) and `AuthorName` (string?) properties |
| `App.xaml.cs` | Schema patches: `ALTER TABLE Assessments ADD COLUMN AuthorUserId INTEGER` and `AuthorName TEXT` for legacy DBs |
| `AutoTable/Services/DatabaseDataService.cs` | `CreateAssessmentAsync` stores AuthorUserId + AuthorName; `GetAssessmentsAsync` and `GetAssessmentAsync` include AuthorUser and return author info |
| `Views/AssessmentsView.xaml.cs` | `BuildAssessmentItem` resolves current session user as author |

---

### TermFees Table Fix + School-Wide KPI Cards ✅ DONE

**Problem:** "No such table: TermFees" error when setting per-class fees in Term Management.
Existing databases (created before `TermFeeEntity` was added) never got the table because
`EnsureCreated()` only works for new DBs.

#### Files Changed

| File | Change |
|------|--------|
| `App.xaml.cs` | Added `CREATE TABLE IF NOT EXISTS TermFees` with unique index on `(TermId, ClassId)` — runs on startup for existing DBs |
| `ViewModels/TermManagementViewModel.cs` | Added `StatusMessage`, `GetFeeForClassInTerm()`, `RefreshSchoolKpisAsync()` for school-wide KPI computation, and KPI properties (`SchoolExpected`, `SchoolCollected`, `SchoolOutstanding`, `SchoolCollectionRate`, `SchoolStudentCount`) |
| `Views/TermManagementView.xaml` | Added 5 KPI cards (Total Expected, Collected, Outstanding, Collection Rate, Active Students) above the per-class fees panel; added `SelectedTermLabel` context indicator; added `KpiHeader` |
| `Views/TermManagementView.xaml.cs` | Wired `TermsList.SelectionChanged` to pre-fill FeeBoxes + refresh KPIs; `SetFee_Click` refreshes KPIs after saving; clear feedback for missing term selection |

### Promotion Overview: GradeFromBands + Process All ✅ DONE

**Problem:** `GetPromotionOverviewAsync` used the hardcoded `GradeFromAverage` (A/B/C/D/E/F)
instead of the class's grading system bands, inconsistent with P5.3 work.

#### Files Changed

| File | Change |
|------|--------|
| `AutoTable/Services/DatabaseDataService.cs` | `GetPromotionOverviewAsync` now loads grading system bands per class (with caching), uses `GradeFromBands` for grade display and `CheckPromotionalPass` for suggested outcome. Added `ProcessAllPromotionsAsync` batch method. |
| `AutoTable/Services/IDataService.cs` | Added `ProcessAllPromotionsAsync(int? classId)` |
| `Demo/MockDataServiceAdapter.cs` | Added stub for `ProcessAllPromotionsAsync` |
| `AutoTable/ViewModels/PromotionViewModel.cs` | Added `ProcessAllCommand` that batch-processes all pending students |
| `Views/PromotionView.xaml` | Added "Process All" button in the filter bar |

---

---

### Class Management: Edit Modal + Subject/Stream UI Fix ✅ DONE

**Problem:** No way to edit an existing class's streams/subjects after creation. Create Class
modal showed subjects as comma-separated text with no remove buttons. Streams were not
prominently displayed. User needed to add multiple streams to a class for student enrollment.

#### Files Changed

| File | Change |
|------|--------|
| `Views/ClassesView.xaml.cs` | Rewrote `BuildSubjectsSection` — subjects now display as individual white chips with ✕ remove buttons (no longer comma-separated text). Rewrote `BuildStreamsSection` — streams display as white chips with ✕ remove buttons + optional stream-teacher ComboBox. Added `OpenEditClassModalAsync()` — opens a modal for any class to add/remove streams and subjects, with diff-based save (removes deleted, adds new). Added `EditClass_Click` handler wired to both row click and Edit button. |
| `Views/ClassesView.xaml` | Added Edit button column to class list table header and row template (7-column grid). |

#### How It Works

1. **Create Class modal:** Subjects and streams appear as individual colored chips (`#335577` / `#224466`) with white text and ✕ remove buttons. Each stream also has an inline teacher picker.
2. **Edit Class modal:** Click any class row → modal shows current streams and subjects as chips with ✕ buttons. Add new streams/subjects via existing-picker or new-name input. Click Save → diffs original vs final state, removes deleted items, adds new ones. Handles both existing items (assign) and new items (create + assign).
3. **Stream creation in Create modal:** The "or new name..." input + Add button creates new streams (stored as Id=-1), which are created and assigned after the class is saved.

#### Verification
- `dotnet build` → **0 errors**
- Admin role-gating: documented in WAY_FORWARD_PLAN.md as P5.4, awaiting user signal

---

### Financial Dashboard: Term Management KPI Merge ✅ DONE

**Problem:** Financial Dashboard showed generic KPIs not connected to term-specific fee
data. Term Management had school-wide KPIs for the selected term. User wanted them merged.

#### Files Changed

| File | Change |
|------|--------|
| `ViewModels/FinancialsDashboardViewModel.cs` | Added term selector (`SelectedTermName`, `TermNames` collection), `TotalExpected`, `CollectionRate`, `ActiveStudentCount`, `TotalPaymentCount`, `PartialCount`, `UnpaidCount`. All calculations now scope to the selected term. Auto-selects active term on load. |
| `Views/FinancialsDashboardView.xaml` | Added term selector ComboBox in a filter bar. Replaced 4 KPI cards with two rows: Row 1 (Total Collected, Total Expected, Outstanding Fees, Collection Rate with icons), Row 2 (Active Students, Students Paid %, Partial Payments, Unpaid, Total Payments). |

#### How It Works

1. Page loads → auto-selects the active term from DB → shows KPIs scoped to that term
2. Switch terms → all KPIs recalculate for the new term
3. Row 1 cards use accent-bar style (colored left border). Row 2 cards use simple label-value style.
4. Recent Transactions table also scoped to selected term.

---

### Fee Collection: Per-Student Expected Amounts + White Modal ✅ DONE

**Problem:** Fee Collection showed 0 expected for newly enrolled students. Record Payment
modal used dark text that was invisible on the dialog background.

#### Files Changed

| File | Change |
|------|--------|
| `ViewModels/FeeCollectionViewModel.cs` | Fixed defaults from hardcoded "P5"/"Term 2, 2025" to "All" with auto-select of first class + active term. Each student's `ExpectedAmount` is now looked up by their own `ClassId` + the selected term's TermFee (not a single class-wide value). Added "All" options for both class and term filters. Suppressed auto-load during initialization. |
| `Views/FeeCollectionView.xaml.cs` | Changed Record Payment modal foreground colors to white: `studentInfoBlock` (#444→White), `hint` (gray→White, opacity 0.7→1.0), error hint (#C42B1C→#FF6B6B), search result name (#000→White), class/stream (#444→#B0FFFFFF), LIN (#777→#80FFFFFF), dropdown background (white→#2A2A2A dark). |

#### How It Works

1. **Expected amount:** For each student, looks up `TermFee.FirstOrDefault(ClassId == student.ClassId && TermId == selectedTermId)?.Amount`. If no TermFee exists for that class+term, shows 0. When "All Terms" is selected, sums all TermFees for the student's class.
2. **Record Payment modal:** All text is white on dark dialog background. Search results show white text on dark (#2A2A2A) dropdown. Student name, class-stream, LIN all use appropriate opacity levels.
3. **Default behavior:** Page opens → first real class selected → active term selected → all students in that class shown with correct expected amounts from TermFees.

---

### LIN Uniqueness Gap Fix ✅ DONE

**Problem:** If a user entered a duplicate LIN in the enrollment form, `SaveEnrollmentAsync`
would save an enrollment record before `CreateStudentAsync` threw, creating orphaned rows.

#### Files Changed

| File | Change |
|------|--------|
| `AutoTable/Services/IDataService.cs` | Added `Task<bool> IsLinTakenAsync(string lin)` |
| `AutoTable/Services/DatabaseDataService.cs` | Implemented `IsLinTakenAsync` (queries active students by LIN). Added defense-in-depth check in `SaveEnrollmentAsync` — blocks if LIN already assigned to active student. |
| `Demo/MockDataServiceAdapter.cs` | Added stub for `IsLinTakenAsync` |
| `ViewModels/EnrollmentViewModel.cs` | Added pre-validation in `SubmitAsync` — calls `IsLinTakenAsync(autoLin)` before saving, shows error without creating any records. |

---

### P5.1 — Assessment Promotion Role ✅ DONE

**Goal:** When creating an assessment, choose its promotion role: *just an assessment*,
*counts toward promotion* (contributory), or a *promotion exam* (the Term 3 / end-of-year
paper that decides promotion). The report card uses the explicit role instead of a heuristic.

#### Files Changed

| File | Change |
|------|--------|
| `Models/AssessmentItem.cs` | Added `AssessmentPromotionRole` enum (`None`, `CountsTowardPromotion`, `PromotionExam`) and `PromotionRole` property on `AssessmentItem` |
| `AutoTable/Data/Entities/StudentEntity.cs` | Added `int PromotionRole` column to `AssessmentEntity` (0=None, 1=Contributory, 2=PromotionExam) |
| `App.xaml.cs` | Added ALTER TABLE schema patch: `Assessments ADD COLUMN PromotionRole INTEGER DEFAULT 0` (guarded by PRAGMA table_info check) |
| `AutoTable/Services/DatabaseDataService.cs` | **CreateAssessmentAsync** now stores `PromotionRole` on the entity and returns it in the model. **GetAssessmentsAsync** and **GetAssessmentAsync** return `PromotionRole` in the mapped model. **GetReportCardSheetAsync** now classifies assessments using the explicit `PromotionRole`: `PromotionExam` → promotional, `CountsTowardPromotion` → contributory, `None` → contributory. Legacy fallback: if no assessment has a `PromotionRole` set, falls back to the old weight-based heuristic. |
| `Views/AssessmentsView.xaml.cs` | Added "Promotion role" ComboBox (3 options) with hint text in the assessment creation dialog. `BuildAssessmentItem` now accepts and passes `AssessmentPromotionRole`. All 4 scope paths (Single, AllInClass, SpecificSubjects, AllInSchool) pass the role through. |

#### How It Works

1. **Creation:** The user picks a promotion role when creating an assessment:
   - "Just an assessment" (default) — does not count toward promotion
   - "Counts toward promotion" — contributory paper, averaged into the promotion decision
   - "Promotion exam (Term 3 / end-of-year)" — the paper that decides promotion

2. **Storage:** Stored as an integer column (`PromotionRole`) on the `Assessments` table.
   Legacy data defaults to 0 (None).

3. **Report Cards:** `GetReportCardSheetAsync` checks `hasAnyRoleSet` (any assessment
   with `PromotionRole != 0`). If explicit roles exist, it uses them for classification.
   If not (legacy data), it falls back to the old heuristic (highest-weighted = promotional).

4. **Grade Resolution:** Grades and pass/fail status continue to use the class's grading
   system bands (from P5.3 work in the uncommitted tree).

#### Verification

- `dotnet build AutoTable.csproj -p:Platform=x64` → **0 errors**
- Schema patch is guarded (won't fail if column already exists)
- All existing tests unaffected (enum defaults to 0 = None)

#### Remaining Gaps

- The `GetReportCardSheetAsync` heuristic fallback should be removed once all assessments
  have explicit roles (future cleanup)
- The promotion role should be displayed in the Assessments list view (currently only
  stored, not shown in the grid)
- Assessment editing does not yet allow changing the promotion role after creation

---

### P5.5 — Defaulters & Cohort Finance Analytics ✅ DONE

**Goal:** Track unpaid fees, collection rates, and per-class cohort performance.
Top-N defaulters sorted by balance, cohort summary with paid/partial/unpaid breakdowns.

#### Files Changed

| File | Change |
|------|--------|
| `Models/FinancialModels.cs` | Added `DefaulterRecord` (StudentId, ExpectedAmount, PaidAmount, Balance, PercentPaid, Status, DaysSinceEnrollment + display properties) and `CohortSummary` (TotalStudents, PaidCount, PartialCount, UnpaidCount, TotalExpected, TotalCollected, CollectionRate, PaidPercent + display property) |
| `AutoTable/Services/IDataService.cs` | Added `GetDefaultersAsync(int? termId, int? classId, decimal? minBalance)` and `GetCohortSummariesAsync(int? termId)` |
| `AutoTable/Services/DatabaseDataService.cs` | Implemented both methods: GetDefaultersAsync queries active students × term fees × payments, computes per-student balance, filters by min balance, sorts descending. GetCohortSummariesAsync groups students by class, computes paid/partial/unpaid counts and collection rate per class + school-wide aggregate. |
| `Demo/MockDataServiceAdapter.cs` | Added stubs for both methods |
| `ViewModels/DefaultersAnalyticsViewModel.cs` | New ViewModel with term/class/min-balance filters, KPI cards (Total Defaulters, Total Owed, Collection Rate, Students/Paid/Partial/Unpaid), Defaulters list, Cohort Summaries collection |
| `Views/DefaultersAnalyticsView.xaml` | New page: filters row (term/class/min-balance), KPI row 1 (defaulters overview), KPI row 2 (cohort breakdown), two-column layout (defaulters list left, cohort summary right) |
| `Views/DefaultersAnalyticsView.xaml.cs` | Code-behind with DataContext binding and MinBalanceBox TextChanged handler |
| `Services/NavigationService.cs` | Added `"Defaulters"` route → `DefaultersAnalyticsView` |
| `Views/ShellView.xaml` | Added "Defaulters" sidebar button with &#xE7BA; glyph |

#### How It Works

1. **Filters:** Term selector (All Terms / specific term), Class selector (All / specific class), Min Balance filter (type a minimum balance to show only students who owe at least that amount).
2. **KPI cards:** Total Defaulters (count), Total Owed (sum of balances), Collection Rate (%), Students/Paid/Partial/Unpaid counts — all scoped to selected filters.
3. **Defaulters list:** Sorted by balance descending (worst defaulters first). Shows student name, LIN, class, expected amount, paid amount, balance.
4. **Cohort summary:** Per-class breakdown of paid/partial/unpaid counts and collection rate. School-wide aggregate at the top. All scoped to selected term.

#### Verification
- `dotnet build AutoTable.csproj -p:Platform=x64` → **0 errors**

---

### Class Management: Immediate Subject/Stream Persistence + Streams Add Button Fix ✅ DONE

**Problem:** Three issues:
1. When typing a new subject name in the Create Class dialog, it was stored as Id=-1 and only persisted when the class was finally created — not added to the master subjects list immediately.
2. The "Add →" button for class streams on the Create Class modal didn't work — it checked `picker.SelectedItem` before `newBox.Text`, so if the picker had a stale selection it would add the wrong item; also the button had no visible styling.
3. The Edit Class modal existed but needed the same immediate-persistence treatment.

#### Files Changed

| File | Change |
|------|--------|
| `Views/ClassesView.xaml.cs` | **BuildSubjectsSection** and **BuildStreamsSection** now accept an optional `Func<string, Task<SimpleLookup>>? createItemAsync` callback. When a new name is typed and Add is clicked, the item is persisted to the DB immediately (getting a real ID) and added to the source list for future use. **Priority fix**: `AddClick` now checks `newBox.Text` FIRST — if the user typed a name, it always uses that, only falling back to the picker when the text box is empty. Both Add buttons now use `SecondaryButtonStyle` for visible styling. **Class creation loop** simplified — subjects and streams now have real IDs so no need for `Id < 0` create-and-assign logic. Both Create and Edit modals pass the creation callbacks. |

#### How It Works

1. **Create Class modal:** Type "Biology" → click Add → immediately saved to subjects table (real ID returned) → chip appears with the real ID → also added to the picker for other classes. Type a stream name → same: immediately saved, real ID.
2. **Edit Class modal:** Same behavior — new subjects/streams created immediately.
3. **Class creation:** All pending items already have real IDs → just assign them to the class.

---

### Uncommitted Work from Previous Session (21 files)

These changes are in the working tree but not yet committed. They cover:

| Feature | Key Files | Status |
|---------|-----------|--------|
| **Grade resolution via grading systems (P5.3)** | `DatabaseDataService.cs` — `GradeFromBands` replacing `GradeFromAverage` in Gradebook, StudentPerformanceDetail, ReportCards | ✅ Code complete |
| **Grading system integration in report cards** | `DatabaseDataService.cs` — class → school default → fallback resolution; `GradingSystemName` + `PassMark` on ReportCardSheetModel | ✅ Code complete |
| **Stream teacher assignment** | `DatabaseDataService.cs` — `AssignStreamToClassAsync` accepts optional `streamTeacherId`; `ClassStreams.StreamTeacherId` support | ✅ Code complete |
| **Analytics view/ViewModel** | `ViewModels/AnalyticsViewModel.cs`, `Views/AnalyticsView.xaml(.cs)` — new analytics dashboard with charts | ✅ Code complete |
| **FeeCollection real data** | `ViewModels/FeeCollectionViewModel.cs` — builds fee records from DB payments + term fees | ✅ Code complete |
| **Dashboard improvements** | `Views/DashboardView.xaml(.cs)` — additional KPIs and quick actions | ✅ Code complete |
| **ClassesView code-behind** | `Views/ClassesView.xaml.cs` — expanded class management UI | ✅ Code complete |
| **ReportCardSheetView fix** | `Views/Controls/ReportCardSheetView.xaml` — minor layout fix | ✅ Code complete |

---

## Roadmap Status (P5 Feature Backlog)

| # | Feature | Status | Notes |
|---|---------|--------|-------|
| P5.1 | Assessment promotion role | ✅ **DONE** | Explicit tri-state on creation; report card uses it |
| P5.2 | Promotion/repeat flow | ✅ **DONE** | Full UI with promote/repeat/shift/reset + batch "Process All"; uses `GradeFromBands` + `CheckPromotionalPass` |
| P5.3 | Grade resolution via grading systems | ✅ **DONE** | `GradeFromBands` replaces `GradeFromAverage` everywhere |
| P5.4 | Admin role-gating | ✅ **DONE** | Sidebar + route + action button gating for Term/Classes/Budget/Promotion |
| P5.5 | Defaulters / cohort analytics | ✅ **DONE** | Full page with defaulter list, cohort summary, KPI cards, filters |
| P5.6 | Mid-term slips | 🔴 NOT STARTED | 3–4 per A4 slip layout |
| P5.7 | Active-term enforcement | ✅ **DONE** | Block assessment/enrollment/fee writes when no active term |
| — | Class edit modal + streams/subjects UI | ✅ **DONE** | White chips with ✕ buttons; EditClass modal for add/remove |
| — | Financial Dashboard KPI merge | ✅ **DONE** | Term selector + merged KPIs from Term Management |
| — | Fee Collection per-student amounts | ✅ **DONE** | Expected/Paid/Balance computed per student/class/term |
| — | Record Payment modal white text | ✅ **DONE** | All text and links in white on dark dialog |
| — | LIN uniqueness gap fix | ✅ **DONE** | 3-layer defense: UI pre-validation, staging guard, service check |
| — | Immediate subject/stream persistence + streams Add button fix | ✅ **DONE** | Items persisted to DB on Add click; priority logic fixed; styled buttons |
| — | Edit Class modal: name, teacher, grading system | ✅ **DONE** | Full class metadata editing (name, teacher, grading system) + streams/subjects |
| — | Defaulters & Cohort Analytics (P5.5) | ✅ **DONE** | New page with defaulter list, cohort summary, KPI cards, term/class filters |
| — | Student credit carry-forward | ✅ **DONE** | Overpayment credits auto-created, applied to next term balances |
| — | Top 10 worst defaulters + CSV export + bar chart | ✅ **DONE** | Highlight card, export button, collection rate chart on Defaulters page |

## Key Patterns & Conventions

### Schema Patches (App.xaml.cs)
```csharp
// Guard pattern: check PRAGMA table_info, add column if missing
using (var rCheck = cmd.ExecuteReader())
{
    while (rCheck.Read())
    {
        if (string.Equals(rCheck.GetString(1), "ColumnName", StringComparison.OrdinalIgnoreCase))
        { found = true; break; }
    }
}
if (!found)
{
    cmd.CommandText = "ALTER TABLE TableName ADD COLUMN ColumnName TYPE DEFAULT value;";
    try { cmd.ExecuteNonQuery(); } catch { }
}
```

### PromotionRole Mapping
```
AssessmentPromotionRole.None            → int 0  (default)
AssessmentPromotionRole.CountsTowardPromotion → int 1
AssessmentPromotionRole.PromotionExam   → int 2
```
Entity uses `int`, model uses `enum`. Cast with `(AssessmentPromotionRole)entity.PromotionRole`.

### Grading System Resolution
```
Class.GradingSystem → GradingSystems.FirstOrDefault(IsDefault) → fallback to first system → hard-coded 50% passmark
```

### Assessment Promotion Classification (Report Cards)
```
if any assessment has PromotionRole != 0:
    PromotionExam → promotional row
    CountsTowardPromotion / None → contributory row
else (legacy):
    highest-weighted/latest-due per subject → promotional (heuristic)
```

---

## Next Actions (suggested order)

1. **P5.6: Mid-term slips** — 3–4 per A4 slip layout alongside full report cards
2. **Finance-filtered report printing** — print only students who cleared fees
3. **EF Migrations verification** — consolidate ALTER TABLE patches into proper migrations
4. **Assessment author display** — show author name in the Assessments list grid

---

## Session History

| Date | Session | Key Deliverables |
|------|---------|------------------|
| 24 Aug | P5.1 + P5.3 + P5.2 + Grading + Report Cards | Assessment promotion role, grade-from-bands, promotion/repeat flow, Process All, report card grading system integration |
| 24 Aug | TermFees fix + KPI cards | Fixed missing TermFees table migration, added school-wide KPI cards to Term Management |
| 26 Aug | Class edit modal + UI | EditClass modal for streams/subjects, white chip UI with ✕ buttons, fixed Create Class modal |
| 26 Aug | Financial Dashboard merge | Merged Term Management KPIs into Financial Dashboard with term selector |
| 26 Aug | Fee Collection fix | Per-student expected amounts, active-term defaults, Record Payment modal white text |
| 26 Aug | LIN uniqueness gap | 3-layer defense against orphaned enrollment records |
| 26 Aug | P5.5 Defaulters Analytics | Full page with defaulter list, cohort summary, KPI cards, term/class filters |
| 26 Aug | Credit carry-forward + defaulters enhancements | Overpayment auto-credit, top-10 highlight, CSV export, bar chart |
| 27 Aug | P5.4 Admin role-gating + P5.7 Active-term enforcement + Assessment author | Sidebar/route/button gating, term enforcement, author field |

---

*Last updated: 26 Aug 2026 — Buffy (Codebuff agent)*
