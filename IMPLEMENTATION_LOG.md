# AutoTable — Implementation Log

> **Live document.** Updated after each feature or fix is completed.  
> This file tracks the *in-progress* implementation work — what was done, what
> changed, and what remains — so any agent (or human) can pick up exactly where
> the previous session left off.

---

## Current Session: 26 Aug 2026

**Branch:** `sql_rec`  
**Build:** 0 errors, 2238 warnings (all pre-existing CA1416 platform warnings)  
**Working tree:** 21 files with uncommitted changes (grading/analytics/fee/dashboard work) + new features

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
| P5.2 | Promotion/repeat flow | ✅ **DONE** | Full UI with promote/repeat/shift/reset per student + batch "Process All"; uses `GradeFromBands` + `CheckPromotionalPass` |
| P5.3 | Grade resolution via grading systems | ✅ **DONE** | `GradeFromBands` replaces hard-coded `GradeFromAverage` in Gradebook, StudentPerformanceDetail, ReportCards, and PromotionOverview |
| P5.4 | Admin role-gating | 🔴 NOT STARTED | Gate Term/Classes/Budget to admin-only |
| P5.5 | Defaulters / cohort analytics | 🔴 NOT STARTED | Top-N defaulters, % paid within window |
| P5.6 | Mid-term slips | 🔴 NOT STARTED | 3–4 per A4 slip layout |
| P5.7 | Active-term enforcement | 🔴 NOT STARTED | Block writes when no active term |

---

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

1. **Commit current work** — all uncommitted changes (grading/analytics/fees/P5.1/P5.2/P5.3/TermFees)
2. **P5.4: Admin role-gating** — extend Moderation's gate pattern to Term/Classes/Budget
3. **P5.5: Defaulters / cohort analytics** — top-N defaulters, % paid within window
4. **P5.6: Mid-term slips** — 3–4 per A4 slip layout
5. **P5.7: Active-term enforcement** — block writes when no active term

---

*Last updated: 26 Aug 2026 — Buffy (Codebuff agent)*
