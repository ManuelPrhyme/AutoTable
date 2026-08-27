# AutoTable — Implementation Log

> **Live document.** Updated after each feature or fix is completed.  
> This file tracks the *in-progress* implementation work — what was done, what
> changed, and what remains — so any agent (or human) can pick up exactly where
> the previous session left off.

---

## Current Session: 27 Aug 2026

**Branch:** `sql_rec`  
**Build:** 0 errors  
**Tests:** 9/9 passing  
**Working tree:** Active session

---

### Report Cards — Full Crash Fix (3 root causes) ✅ DONE

The Report Cards page crashed the entire app when clicked. Three separate issues were identified and fixed:

**Root Cause 1: XamlParseException (Foreground type mismatch)**
- Line 155 of `ReportCardsView.xaml` had `Foreground="{ThemeResource MutedTextStyle}"` 
- `MutedTextStyle` is a TextBlock Style, not a brush — WinUI 3 threw `COMException (0x802B000A)` at parse time
- Fixed by replacing with `{ThemeResource TextSecondaryBrush}`

**Root Cause 2: ViewModel crash on construction**
- `ReportCardsViewModel` constructor threw `InvalidOperationException` when `AppServices.DataService` was null, crashing before XAML loaded
- Fixed by making `_dataService` nullable, adding `Task.Delay(200)` to defer DB queries past page layout, and adding null-safety checks

**Root Cause 3: Fire-and-forget async exceptions**
- `_ = SafeInitializeAsync()` and `_ = SafeLoadAsync()` had no try-catch — unobserved task exceptions terminated the WinUI 3 process
- Fixed by wrapping all async operations in `SafeInitializeAsync`/`SafeLoadAsync` with try-catch and `StatusMessage` for error display

| File | Change |
|------|--------|
| `Views/ReportCardsView.xaml` | Fixed `Foreground` type mismatch (`MutedTextStyle` → `TextSecondaryBrush`) |
| `ViewModels/ReportCardsViewModel.cs` | Nullable DataService, `_initialized` guard, `SafeInitializeAsync`/`SafeLoadAsync` wrappers, `Task.Delay(200)` |
| `App.xaml.cs` | Enhanced `UnhandledException` handler with full diagnostics (timestamp, HRESULT, inner exception) |

---

### Schema Patches Consolidation ✅ DONE

Extracted ~300 lines of repetitive inline PRAGMA/ALTER logic from `App.xaml.cs` into a dedicated helper class.

| File | Change |
|------|--------|
| `AutoTable/Data/SchemaPatches.cs` | **New** — 13 focused, idempotent methods for schema migration (one per table/column) |
| `App.xaml.cs` | Replaced all inline patches with single `SchemaPatches.ApplyAll(sqliteConnection)` call |

---

### Head Teacher Comment — TermId Resolution ✅ DONE

The head teacher comment dialog always saved with `termId=null`, losing per-term context.

| File | Change |
|------|--------|
| `Views/ReportCardsView.xaml.cs` | `PromptHeadTeacherCommentAsync` now resolves `SelectedTerm` name to DB ID via `GetTermLookupsAsync()` |

---

### TermManagementView — DataTrigger Fix ✅ DONE

WinUI 3 doesn't support `Style.Triggers`/`DataTrigger` (WPF-only feature).

| File | Change |
|------|--------|
| `Views/TermManagementView.xaml` | Removed `DataTrigger` block, added `x:Name="ActiveIndicator"` on the dot |
| `Views/TermManagementView.xaml.cs` | Added `UpdateActiveIndicators()` method that walks the visual tree to color the active term dot green |

---

### School Name — Dynamic Sidebar Loading ✅ DONE

The sidebar header showed hardcoded "Bright Future Primary". Now loads from SchoolSettings DB.

| File | Change |
|------|--------|
| `Views/ShellView.xaml` | Renamed hardcoded TextBlock to `x:Name="SchoolNameText"` with default "AutoTable Academy" |
| `Views/ShellView.xaml.cs` | Loads school name from `GetSchoolSettingsAsync()` on `ShellView_Loaded` |

---

### Batch Progress Indicator ✅ DONE

Added a progress bar that appears during batch report card generation (Generate All, Print All, Export PDF).

| File | Change |
|------|--------|
| `Views/ReportCardsView.xaml` | Added `ProgressCard` border with `ProgressBar`, `ProgressText`, `ProgressDetail` (hidden by default) |
| `Views/ReportCardsView.xaml.cs` | `BuildSheetsAsync` now reports per-student progress; `HideProgress()` in `try/finally` after operations |

**Behavior:** Shows only for batch operations (2+ students). Displays progress percentage, count (e.g., "28 / 42 students — John Smith"), and completion message.

---

### Report Cards — Filter Bar Layout Redesign ✅ DONE

Consolidated from 3 rows to 2 rows for a cleaner, more compact filter bar.

**Before:**
```
│ [Search____________] ☐ Fee Cleared      │  Row 0
│ [Class▼] [Term▼] [Stream▼]              │  Row 1
│ [Generate] [Print] [Export] [Mid-Term]   │  Row 2
```

**After:**
```
│ [Search__] ☐ Fee [Class▼] [Term▼] [Stream▼]  │  Row 0 (one line)
│ [Generate] [Print] [Export] [Mid-Term]         │  Row 1 (buttons)
```

| File | Change |
|------|--------|
| `Views/ReportCardsView.xaml` | Search bar (400px), checkbox, and filter dropdowns on one line; buttons below; `RowSpacing="10"` |

---

### Assessment Column Balancing ✅ DONE

Rebalanced the assessment table columns on the A4 report card sheet.

**Before:** `SUBJECT(2*) | ASSESSMENT(*) | MARK(*) | GRADE(*) | VERDICT(*)`  
**After:** `SUBJECT(3*) | ASSESSMENT(2*) | MARK(*) | GRADE(*) | VERDICT(1.2*)`

| File | Change |
|------|--------|
| `Views/Controls/ReportCardSheetView.xaml` | Updated column ratios in both Promotional and Contributory assessment tables |

---

### Term Management — Layout Balancing ✅ DONE

Balanced the Existing Terms and Per-Class Term Fees cards to equal size with aligned Set buttons.

| File | Change |
|------|--------|
| `Views/TermManagementView.xaml` | Both columns `*` + `*` (equal width), both use `TableCardStyle` with `CardHeaderBorderStyle`; fee list rows use 3-column Grid for aligned TextBox + Button |

---

### Classes & Subjects — Layout Fixes ✅ DONE

Three fixes to the Classes & Subjects page:

1. **All Classes expanded** — column ratio changed from `2*` + `*` to `3*` + `Auto`
2. **Grading Systems compact** — `MinWidth="280" MaxWidth="340"`, button below heading
3. **Delete buttons borderless** — Changed from `IconButtonStyle` to `GhostButtonStyle` + `BorderThickness="0"` + red foreground
4. **Edit button no longer clipped** — ACTIONS column changed from `Width="60"` to `Width="Auto"`

| File | Change |
|------|--------|
| `Views/ClassesView.xaml` | Column ratios, Delete button style, ACTIONS column width |

---

## Roadmap Status (P5 Feature Backlog)

| # | Feature | Status |
|---|---------|--------|
| P5.1 | Assessment promotion role | ✅ **DONE** |
| P5.2 | Promotion/repeat flow | ✅ **DONE** |
| P5.3 | Grade resolution via grading systems | ✅ **DONE** |
| P5.4 | Admin role-gating | ✅ **DONE** (dormant — DEV MODE) |
| P5.5 | Defaulters / cohort analytics | ✅ **DONE** |
| P5.6 | Mid-term slips | ✅ **DONE** |
| P5.7 | Active-term enforcement | ✅ **DONE** |
| — | Report Cards: students display fix | ✅ **DONE** |
| — | Report Cards: "All" filter option | ✅ **DONE** |
| — | Report Cards: A4 preview + print dialog | ✅ **DONE** |
| — | Report Cards: Generate All | ✅ **DONE** |
| — | Report Cards: crash fix (3 root causes) | ✅ **DONE** |
| — | Report Cards: batch progress indicator | ✅ **DONE** |
| — | Report Cards: filter bar redesign (2-row) | ✅ **DONE** |
| — | Report Cards: assessment column balancing | ✅ **DONE** |
| — | Gradebook: "All" filter option | ✅ **DONE** |
| — | Assessment donut progress | ✅ **DONE** |
| — | Assessment author column | ✅ **DONE** |
| — | Class edit modal + streams/subjects UI | ✅ **DONE** |
| — | Financial Dashboard KPI merge | ✅ **DONE** |
| — | Fee Collection per-student amounts | ✅ **DONE** |
| — | LIN uniqueness gap fix | ✅ **DONE** |
| — | Head teacher comment on A4 sheet | ✅ **DONE** |
| — | School Settings page | ✅ **DONE** |
| — | Schema patches consolidation | ✅ **DONE** |
| — | Head teacher comment termId fix | ✅ **DONE** |
| — | School name dynamic loading | ✅ **DONE** |
| — | TermManagement DataTrigger fix | ✅ **DONE** |
| — | Term Management layout balancing | ✅ **DONE** |
| — | Classes & Subjects layout fixes | ✅ **DONE** |
| — | Delete buttons borderless | ✅ **DONE** |
| — | Edit button clipping fix | ✅ **DONE** |

---

## Key Patterns & Conventions

### Schema Patches (AutoTable/Data/SchemaPatches.cs)
```csharp
// Centralized helper: SchemaPatches.ApplyAll(sqliteConnection)
// Each method is idempotent: checks PRAGMA table_info, adds column/table if missing
```

### Report Cards — Safe Async Pattern
```csharp
private async Task SafeInitializeAsync() {
    try { await Task.Delay(200); /* ... DB queries ... */ }
    catch (Exception ex) { StatusMessage = $"Failed: {ex.Message}"; }
}
```

### PromotionRole Mapping
```
AssessmentPromotionRole.None            → int 0
AssessmentPromotionRole.CountsTowardPromotion → int 1
AssessmentPromotionRole.PromotionExam   → int 2
```

### Grading System Resolution
```
Class.GradingSystem → GradingSystems.FirstOrDefault(IsDefault) → fallback to first system → hard-coded 50% passmark
```

### Role Gating Pattern (dormant)
```csharp
// ── ROLE-BASED GATING (dormant during development) ──────
// if (!SessionService.Instance.IsAdministrator) { ... }
// ── END ROLE-BASED GATING ──────
// DEV MODE: All users have full access during development.
```

---

## Next Actions — Remaining Gaps

| # | Task | Priority |
|---|------|----------|
| 1 | **Print preview pagination** — page navigation for multi-student prints | P3 |

---

## Session History

| Date | Session | Key Deliverables |
|------|---------|------------------|
| 24 Aug | P5.1 + P5.3 + P5.2 + Grading + Report Cards | Assessment promotion role, grade-from-bands, promotion/repeat flow |
| 24 Aug | TermFees fix + KPI cards | Fixed missing TermFees table migration, school-wide KPI cards |
| 26 Aug | Class edit modal + UI | EditClass modal, white chip UI, streams/subjects |
| 26 Aug | Financial Dashboard merge | Merged Term Management KPIs into Financial Dashboard |
| 26 Aug | Fee Collection fix | Per-student expected amounts, Record Payment modal white text |
| 26 Aug | LIN uniqueness gap | 3-layer defense against orphaned enrollment records |
| 26 Aug | P5.5 Defaulters Analytics | Full page with defaulter list, cohort summary, KPI cards |
| 26 Aug | Credit carry-forward + defaulters enhancements | Overpayment auto-credit, top-10 highlight, CSV export |
| 27 Aug | P5.4 + P5.7 + Assessment author | Admin role-gating, term enforcement, author field |
| 27 Aug | Report Cards fix + Donut + Mid-Term + "All" filters | Students display fix, donut progress, mid-term slips, Gradebook "All" |
| 27 Aug | Print dialog + Generate All + Role gating dormancy | Custom print dialog (left/right), Generate All, role gating commented out |
| 27 Aug | Finance filter + PDF export + Head teacher comment | Fee Cleared Only filter, Export PDF, head teacher comment on A4 sheet |
| 27 Aug | School Settings + student ID fix + mid-term wiring | School Settings page, fixed head teacher comment student ID, mid-term slips read from settings |
| 27 Aug | **Crash fixes + UI layout + schema consolidation** | Report Cards crash fix (3 root causes), schema patches consolidation, head teacher termId fix, batch progress indicator, filter bar redesign, assessment column balancing, term management layout, classes & subjects layout fixes |

---

*Last updated: 27 Aug 2026 — Buffy (Codebuff agent)*
