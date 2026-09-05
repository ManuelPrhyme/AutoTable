# AutoTable — Implementation Log

> **Live document.** Updated after each feature or fix is completed.  
> This file tracks the *in-progress* implementation work — what was done, what
> changed, and what remains — so any agent (or human) can pick up exactly where
> the previous session left off.

---

## Current Session: 31 Aug 2026

**Branch:** `sql_rec`
**Build:** 0 errors (full solution)
**Working tree:** Active session

---

### Marks Entry Table Header Update ✅ DONE

Changed "ADM NO." to "LIN" in the Marks Entry table column header.

| File | Change |
|------|--------|
| `Views/MarksEntryView.xaml` | Updated column header text from "ADM NO." to "LIN" |

---

### Previous Session: 30 Aug 2026 (Session 2)

**Branch:** `sql_rec`
**Build:** 0 errors (full solution)
**Working tree:** Active session (30+ files, commit pending)

---

### Print Modal — Configured Printer Detection & Direct Printing ✅ DONE

The Report Cards print modal now detects every configured printer on the machine and lets
the user pick which one to print with — for a single student or a whole batch — without
needing the OS print dialog.

| File | Change |
|------|--------|
| `AutoTable.csproj` | Added `System.Drawing.Common 8.0.11` (GDI printing + printer enumeration) |
| `AutoTable/Services/PrinterService.cs` | **New.** `GetInstalledPrinters()` via `PrinterSettings.InstalledPrinters` (lists local + network printers, incl. "Microsoft Print to PDF"); `GetDefaultPrinterName()` via `new PrinterSettings().PrinterName`; `PrintBitmaps(printerName, copies, pages, out error)` — spools one GDI `PrintDocument` job to the chosen printer (A4 preferred, copies passed to driver); `BitmapFromBgraPixels()` converts WinUI BGRA8 pixels to a GDI bitmap |
| `Views/ReportCardsView.xaml` | Added off-screen `RenderHost` grid (`Opacity=0`, parked at -5000,-5000) so report-card sheets can be rasterized for direct printing |
| `Views/ReportCardsView.xaml.cs` | Modal printer combo populated with detected printers (default marked "(Default)") + a "System print dialog…" fallback; copies NumberBox wired; sheets rasterized at 2x via `RenderTargetBitmap.RenderAsync(element, w, h)` and sent straight to the selected printer; live status text updates during render/send; Print path keeps the modal open while working, closes it on completion |

How it works:
1. User opens the print modal (single row **Print** or **Print All**).
2. Combo shows every configured printer, default preselected — no hard-coded "Default Printer".
3. Choosing a real printer prints **directly to it** (GDI `PrintDocument`, no OS dialog).
4. Choosing "System print dialog…" keeps the old WinUI PrintManager flow.
5. Copies are honored for both paths.

Notes / verification:
- ContentDialog content is capturable-free (Popup) — sheets are parked in the page's
  `RenderHost` before `RenderTargetBitmap.RenderAsync` (which supports scaled rendering).
- Tuning: `renderScale = 2` (≈196 DPI on A4) keeps memory low for big batches.
- Verified on this dev box: enumeration returns "Microsoft XPS Document Writer",
  "Microsoft Print to PDF" (default), "Fax", "ALPDF"; `PrintBitmaps("Microsoft Print to PDF", 1, page)` → success.
- Build: 0 errors. App launches cleanly.

---

### Build & Crash Fixes ✅ DONE

| Fix | Root Cause | Solution |
|-----|-----------|----------|
| WMC9999 XAML compiler error | Stale obj/ folder from ARM64 build | `rm -rf bin obj` + clean rebuild |
| WMC0601 missing App.xaml in obj | Same stale cache | Resolved by clean rebuild |
| `FilteredStudents` binding error | `{x:Bind}` targets code-behind, not ViewModel | Added forwarding property on `StudentsView.xaml.cs`; moved `_vm` before `InitializeComponent()` |
| Students tab crash | `StatusColor` converter in `StackPanel.Resources`; `x:Bind` looks up at Page scope | Moved converters to `Page.Resources` |

### Nullable/Unused Variable Warnings ✅ DONE

| File | Warning | Fix |
|------|---------|-----|
| `IDataService.cs` | CS8604 — nullable `className` mismatch | Made `className` parameter `string?` in `GetReportCardListAsync` / `GetMidTermSlipsAsync` |
| `DatabaseDataService.cs` | Same (implementation) | Updated implementations to match `string?` |
| `MockDataServiceAdapter.cs` | CS8767 — nullability mismatch | Updated to match interface |
| `DatabaseDataService.cs:2625-2626` | CS8602 — possible null dereference | Added null-forgiving operator on `ThenInclude` lambdas |
| `AssessmentsView.xaml.cs:476` | CS0219 — `authorId` unused | Removed unused variable and dead code block |

### Student Status System ✅ DONE

| File | Change |
|------|--------|
| `Converters/FormatConverters.cs` | `StatusLabelConverter` — all terminated students show "Inactive"; `StatusColorConverter` — hardcoded colors (Green #27AE60, Blue #2980B9, Orange #F39C12, Red #E74C3C) instead of ThemeResourceHelper |
| `AutoTable/Models/Student.cs` | `InactiveCauseText` shows specific reason ("Completed course", "Expelled", "Left School", "Other") instead of generic "Terminated" |
| `AutoTable/Views/StudentsView.xaml` | Status column restructured: vertical StackPanel with cause text below "Inactive" word; converters moved to `Page.Resources` |
| `ViewModels/DashboardViewModel.cs` | Student count fixed to filter by `s.IsActive` (was counting ALL students) |

Inactive students are excluded from all active-student operations (marks, fees, report cards, performance, dashboard counts). Historical records remain intact.

### Student Filter Bar Layout ✅ DONE

| File | Change |
|------|--------|
| `AutoTable/Views/StudentsView.xaml` | Search bar between filters and Add Student (24px spacer column 4); Add Student at absolute right (HorizontalAlignment="Right"); Search MinWidth=220 |

### Student Restore ✅ DONE

| File | Change |
|------|--------|
| `AutoTable/Views/StudentsView.xaml` | Action buttons conditionally visible: active → Shift/View/Edit/Terminate; inactive → Restore (blue PrimaryButtonStyle) |
| `AutoTable/Views/StudentsView.xaml.cs` | `BoolToVisibilityConverter` + `BoolToVisibilityNegateConverter` registered; `RestoreStudent_Click` handler: confirmation dialog → sets `IsActive=true`, clears termination, calls `UpdateStudentAsync` |

Restore sets `IsActive=true` on existing record — no new student created. All active-student operations automatically include restored students.

### Term Management Enhancements ✅ DONE

| File | Change |
|------|--------|
| `Views/ShellView.xaml.cs` | Added "TermManagement" to `PageMeta` — header updates on navigation |
| `AutoTable/Models/SimpleLookup.cs` | Added optional `IsActive` and `EndDate` properties |
| `AutoTable/Services/DatabaseDataService.cs` | `GetTermLookupsAsync` populates `IsActive` and `EndDate` from DB |
| `Views/TermManagementView.xaml` | Added `x:Name="ActivateBtn"` and `x:Name="DeactivateBtn"` to buttons |
| `Views/TermManagementView.xaml.cs` | `UpdateActiveIndicators()` controls button visibility: Active → Deactivate only; Inactive (not ended) → Set Active only; Ended → both hidden |

### Shift Modal — Stream Filtering ✅ DONE

| File | Change |
|------|--------|
| `AutoTable/ViewModels/StudentsViewModel.cs` | Split `LoadClassAndStreamOptionsAsync()` into `LoadClassOptionsAsync()` + `LoadStreamsForClassAsync(int classId)` |
| `AutoTable/Views/StudentsView.xaml.cs` | `ShiftEnrollment_Click` loads streams for current class; `classBox.SelectionChanged` dynamically reloads streams; stream ComboBox resets on class change |

### Teachers Table — Table.md Layout ✅ DONE

| File | Change |
|------|--------|
| `Views/TeachersView.xaml` | Column widths (2*/1.5*/1.5*/1.5*/2*/1.2*), header padding 16,10, row padding 16,8, headers left-aligned, MaxHeight=480 |

### Assessments — Independent Stream/Subject Filters ✅ DONE

| File | Change |
|------|--------|
| `ViewModels/AssessmentsViewModel.cs` | `OnSelectedSubjectChanged` and `OnSelectedStreamChanged` only call `ApplyFilterAsync()` (no option reload); `OnClassChangedAsync` preserves previous selections when still valid |

### Report Cards — PDF Export + Print ✅ DONE

| File | Change |
|------|--------|
| `AutoTable.csproj` | QuestPDF NuGet package for PDF generation |
| `Views/ReportCardsView.xaml.cs` | **Export PDF**: FileSavePicker → combined PDF via QuestPDF; **Print All**: FolderPicker → individual PDFs per student; **Print** (per-student): opens Windows Print dialog via PrintManager pipeline |

### Moderation Removed ✅ DONE

| File | Change |
|------|--------|
| `Views/ModerationView.xaml` | Deleted |
| `Views/ModerationView.xaml.cs` | Deleted |
| `ViewModels/ModerationViewModel.cs` | Deleted |
| `Models/PerformanceModels.cs` | Removed `ModerationItem` class |
| `Services/NavigationService.cs` | Removed "Moderation" route |
| `Views/ShellView.xaml` | Removed `NavModeration` sidebar button |
| `Views/ShellView.xaml.cs` | Removed from `PageMeta`, `Routes`, `NavModeration.Visibility` |
| `AutoTable/Services/IDataService.cs` | Removed `VerifyAssessmentAsync`, `PublishAssessmentAsync` |
| `AutoTable/Services/DatabaseDataService.cs` | Removed implementations |
| `Demo/MockDataServiceAdapter.cs` | Removed mock moderation methods |
| `ViewModels/DashboardViewModel.cs` | Removed "ready for moderation" insight |
| `ViewModels/AiInsightsViewModel.cs` | Removed "ready for moderation" recommendation |

---

## Previous Session: 30 Aug 2026

**Branch:** `sql_rec`
**Build:** 0 errors (full solution)
**Working tree:** Active session (30+ files, commit pending)

---

### Multi-Subject Creation Error Fix — `Assessments.SubjectId` NOT NULL ✅ DONE

The runtime error "create a multi-subject assessment failed" was caused by legacy DBs where
`Assessments.SubjectId` was created `INTEGER NOT NULL` (SQLite can't drop a NOT NULL via
ALTER). Fix = full table-rebuild patch.

| File | Change |
|------|--------|
| `AutoTable/Data/SchemaPatches.cs` | `PatchAssessmentsNullableSubject` — PRAGMA-driven detection, `Assessments_new` with nullable SubjectId + identical FKs, copy rows, drop/rename, recreate indexes (now includes StreamId in the unique name index), restore AUTOINCREMENT sequence |
| `AutoTable/Data/SchemaPatches.cs` | `PatchAssessmentsCreatedAt` — adds `Assessments.CreatedAt` (back-fills from DueDate proxy) |
| `AutoTable/Data/SchemaPatches.cs` | `PatchAssessmentsStreamIds` — adds `Assessments.StreamIds` (CSV of target stream ids for stream-scoped papers) |

---

### Stream-Granularity Assessments — Stream / Class / Whole School ✅ DONE

| File | Change |
|------|--------|
| `Models/AssessmentItem.cs` | `AssessmentScope.Stream` (value 4); `StreamIds`, `StreamNames` lists |
| `Views/AssessmentsView.xaml.cs` | Creation dialog: "For a Specific Stream" scope, stream picker, `IsClassWide=false` + `StreamName` |
| `AutoTable/Services/DatabaseDataService.cs` | `CreateAssessmentAsync` validates stream + forces `IsClassWide=false`; `GetAssessmentsAsync`/`GetAssessmentAsync` map Stream scope/names; report card excluded for students not in a target stream |

Multi-stream note: an assessment can now target **any subset of a class's streams** (paper for 2 of 4 streams). Stream ids persist as `StreamId` (first) + `StreamIdsCsv` (all); marks entry, completion, and report cards roster students across all target streams.

---

### Marks Entry — Stream Filter Before Subject ✅ DONE

| File | Change |
|------|--------|
| `ViewModels/MarksEntryViewModel.cs` | Stream filter appears when the selected assessment is stream-scoped; populated with target streams + "All"; saves pass the chosen stream |
| `Views/MarksEntryView.xaml` | Stream combo inserted before Subject in the filter bar |
| `AutoTable/Services/DatabaseDataService.cs` | `GetStudentMarksAsync(className, subject, assessmentName, streamName = null)` narrows roster to the selected stream |
| `AutoTable/Services/IDataService.cs`, `Demo/MockDataServiceAdapter.cs` | New signature |

Root-cause note from the field: an assessment ("red") targeted the **Blue stream**, but all P2 students are in **Pink** — so marks entry returned 0 rows. Not a roster bug; the stream filter now makes this visible immediately.

---

### Assessments Page — Class-Restricted Subjects + Stream Filter ✅ DONE

| File | Change |
|------|--------|
| `ViewModels/AssessmentsViewModel.cs` | Subject combo populates from `GetSubjectsForClassAsync(classId)` (falls back to all subjects on "All classes"); conditional `StreamFilterVisibility` (Stream combo shown only when the selected class has stream-scoped assessments) |
| `Views/AssessmentsView.xaml` | Stream filter column inserted before Subject |

---

### Expandable + Scrollable Create-Assessment Modal ✅ DONE

| File | Change |
|------|--------|
| `Views/AssessmentsView.xaml.cs` | Initial display = 3 scope radio checkboxes (Entire School / Class / Stream). Selecting one expands the `detailsPanel` with the expected setup fields. Class scope → Class dropdown; Stream scope → Class + Stream pickers side by side; Entire School → no class/stream/subject tools. Subjects granularity combo (Single / All / Specific) drives which subject control appears. ScrollViewer wraps content (`MaxHeight=460`, Auto vertical scrollbar); `ContentDialog MaxHeight=620`; `CalendarDatePicker` replaces the 3-list `DatePicker`; weight left-aligned |

---

### Students List — Status, Terminate, View, Edit, Shift ✅ DONE

| File | Change |
|------|--------|
| `AutoTable/Models/Student.cs` | `StatusText`, `InactiveCauseText`, `StatusDotSource`, `StatusLabel`, `TerminationYear` |
| `Converters/FormatConverters.cs` | `StatusColorConverter` maps Active/Inactive → green/red dot |
| `AutoTable/Views/StudentsView.xaml` | Status column (dot + cause), column headers, removed 3rd (Class) column, buttons Shift → View → Edit → Terminate (red, last) |
| `AutoTable/Views/StudentsView.xaml.cs` | `TerminateStudent_Click` (reason modal: Completed / Expelled / ChangedSchool / Other), `ViewStudent_Click` (read-only full record modal), `EditStudent_Click` (editable fields modal), existing `ShiftEnrollment_Click` |
| `AutoTable/ViewModels/StudentsViewModel.cs` | Class+stream options loader, `ShiftEnrollmentAsync` |
| `Resources/DesignTokens.xaml` | `DangerButtonStyle`, `DangerGhostButtonStyle` |

Inactive students keep full records (audit / historical report cards) but are excluded from all active-student operations (marks rosters, completion, report cards, fees, performance, dashboard counts).

---

### Sidebar Branding + Misc UI ✅ DONE

| File | Change |
|------|--------|
| `Views/ShellView.xaml(.cs)` | Brand block now shows School Name (from settings) instead of "AutoTable", school motto as subtitle, and the configured logo image in the blue shape |
| `AutoTable/Models/GradingSystemModels.cs` | `BandsSummary` renders `A - 90-100` (hyphen between designation and range) |
| `Resources/DesignTokens.xaml` | Grading-system Delete uses `DangerGhostButtonStyle` (red on hover, white text on focus/pressed) |
| `Views/ReportCardsView.xaml` | Search bar widened (~80%) |

---

### Students Tab Crash — Runtime Resource Scope Fix ✅ DONE

Status-dot `Ellipse.Fill` was bound with `{StaticResource StatusColor}` — a converter declared
at the page's `StackPanel` scope. WinUI 3 **DataTemplates cannot see Page/StackPanel-scoped
resources**, so the lookup returned null at runtime and crashed the tab. Bound to the global
`StatusColorConverter` (registered in `App.xaml`) instead.

| File | Change |
|------|--------|
| `AutoTable/Views/StudentsView.xaml` | `Ellipse.Fill` binding now uses `{StaticResource StatusColorConverter}` |

---

### Toast Infrastructure — Added, Then Disabled (Dev Mode) 🔶

`ToastService` / `NotificationStore` created and wired into `AppServices.Toasts` with call sites
across views/viewmodels (settings save, enrollment, teacher register, class create, payment,
print, student updates). All call sites commented out during development after a build issue was
reported; infra remains as dead types for re-enabling later.

| File | Change |
|------|--------|
| `AutoTable/Services/ToastService.cs`, `NotificationStore.cs` | New toast infra |
| `AutoTable/AppServices.cs` | `Toasts` property commented out |
| 9 call sites (StudentsView, Enrollment, SchoolSettings, Teachers, Classes, FeeCollection, ReportCards) | `// AppServices.Toasts.Show(...)` commented |

---

## Session: 28 Aug 2026

**Branch:** `sql_rec`
**Build:** 0 errors (full solution)
**Working tree:** Active session (15+ files, commit pending)

---

### Promotion Role — 5-State Model ✅ DONE

`AssessmentPromotionRole` extended from tri-state to **five states** (legacy ints preserved):

| # | State | Persisted int |
|---|-------|--------------|
| 1 | Just an Assessment | 0 (None) |
| 2 | End of Term | 3 (EndOfTerm) |
| 3 | Contributory (End of Term) | 4 (ContributoryEndOfTerm) |
| 4 | Contributory (End of Year / Promotional) | 1 (CountsTowardPromotion) |
| 5 | End of Year (Promotional) | 2 (PromotionExam) |

| File | Change |
|------|--------|
| `Models/AssessmentItem.cs` | 5-state enum + doc comments |
| `Views/AssessmentsView.xaml.cs` | Dialog offers all 5; resolves via parallel enum array (display order ≠ int order) |
| `DatabaseDataService.cs` (report card) | EndOfTerm/PromotionExam → promotional table; Contributory* → contributory table; None excluded. Multi-subject papers fan out per subject. |
| `DatabaseDataService.cs` (promotion average) | Only end-of-year items count (PromotionExam = deciding, CountsTowardPromotion averaged) |

---

### Multi-Subject Assessments — One Assessment, Many Subjects ✅ DONE

Replaced the "one AssessmentEntity per subject" model with a single shared assessment carrying linked subjects.

**Schema** (`StudentEntity.cs`, `AppDbContext.cs`, `SchemaPatches.cs`):
- `MarkEntity.SubjectId` (nullable) — marks in multi-subject papers are keyed `(AssessmentId, StudentId, SubjectId)`
- `AssessmentEntity.SubjectId` now nullable; single-subject assessments keep it set (NOT migrated, per decision)
- New `AssessmentSubject` link table
- New `Assessments.IsSchoolWide` flag — AllInSchool papers match any class
- Idempotent ALTER patches + one-time back-fill (existing marks stamped from their assessment's subject)

**Services** (`DatabaseDataService.cs`, `IDataService.cs`, `MockDataServiceAdapter.cs`):
- `CreateAssessmentAsync` — one entity per scope; multi-subject carries `SubjectNames` → link table
- `UpdateMarkAsync` / `DeleteMarkAsync` — optional `subjectName`; required + validated for multi-subject papers
- `UpdateAssessmentCompletionAsync` — completion reflects the ENTIRE assessment: `students × linked subjects`
- `GetStudentMarksAsync` — loads the chosen subject's mark column
- Gradebook, report card (one row per subject), student performance, promotion average — group by the **mark's subject**
- Subject delete / remove-from-class guards also check the link table

**UI** (`Views/AssessmentsView.xaml.cs`, `ViewModels/MarksEntryViewModel.cs`, `Views/MarksEntryView.xaml`):
- Creation: no more per-subject loops; hints updated ("pick the subject at marks entry")
- Marks entry filter order: **Class → Assessment → Subject** — the Subject filter appears only when a multi-subject assessment is selected, populated with its linked subjects; saves pass the chosen subject

---

### Print — PDF + Any Installed Printer ✅ DONE

The Windows PrintManager pipeline already lists every installed printer plus "Microsoft Print to PDF". Hardened it:

| File | Change |
|------|--------|
| `Views/ReportCardsView.xaml.cs` | `ConfigurePrintTaskOptions` (A4 portrait, color, best-effort); `ShowPrintDialogAsync` guard with "Printing unavailable" message; clearer Export-PDF guidance |
| `Views/FeeCollectionView.xaml.cs` | Same A4 defaults + failure guard on fee slips |

---

### Active Term — User-Controlled ✅ DONE

An active term no longer gets silently deactivated when its end date passes.

| File | Change |
|------|--------|
| `App.xaml.cs` | Removed "deactivate ended active terms" startup housekeeping; only picks a default active term when NONE is active |
| `DatabaseDataService.UpdateTermAsync` | Removed `EndDate < now → IsActive = false` override |

---

### UI Polish ✅ DONE

| File | Change |
|------|--------|
| `Views/AssessmentsView.xaml` | All Assessments table: 8 equal `*` columns edge-to-edge (padding intact) + vertical scrollbar (`MaxHeight="480"`) |
| `Views/ReportCardsView.xaml` | Filter bar re-grouped: Class/Term/Stream left; Search + Fee Cleared checkbox right (flex spacer) |
| `ViewModels/PromotionViewModel.cs` | Admin gating applied-but-commented on all 5 commands (Reset added to match) |

---

## Previous Session: 27 Aug 2026

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
| — | Report Cards: PDF export (combined + individual) | ✅ **DONE** |
| — | Report Cards: Print All → individual PDFs | ✅ **DONE** |
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
| — | Student status system (inactive display, restore, colors) | ✅ **DONE** |
| — | Student filter bar layout (search + add student) | ✅ **DONE** |
| — | Term activeness persistence (button visibility) | ✅ **DONE** |
| — | Shift modal stream filtering | ✅ **DONE** |
| — | Assessments independent filters | ✅ **DONE** |
| — | Moderation removed | ✅ **DONE** |
| — | Build/crash fixes (XAML compiler, x:Bind, resources) | ✅ **DONE** |

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

### PromotionRole Mapping (5-state, 28 Aug)
```
AssessmentPromotionRole.None                 → int 0  (Just an Assessment)
AssessmentPromotionRole.CountsTowardPromotion → int 1  (Contributory End of Year / Promotional)
AssessmentPromotionRole.PromotionExam        → int 2  (End of Year / Promotional)
AssessmentPromotionRole.EndOfTerm            → int 3  (End of Term)
AssessmentPromotionRole.ContributoryEndOfTerm → int 4  (Contributory End of Term)
```

Report card classification:
- EndOfTerm / PromotionExam → promotional table; ContributoryEndOfTerm / CountsTowardPromotion → contributory table; None → excluded
- If any assessment has `PromotionRole != 0`: use explicit roles
- Otherwise (legacy data): fallback to weight-based heuristic

### Multi-Subject Assessments (28 Aug)
```
Mark row key:      (AssessmentId, StudentId, SubjectId)   // SubjectId null for single-subject marks
Completion:        students × linked subjects (whole assessment)
Marks entry flow:  Class → Assessment → Subject (subject filter only for multi-subject papers)
Subject links:     AssessmentSubject link table (AssessmentEntity.SubjectId null for multi)
School-wide:       Assessments.IsSchoolWide = true → matches any class
```

### Active Term Rule (28 Aug)
```
IsActive is user-controlled ONLY. No end-date auto-deactivation
(App.xaml.cs housekeeping + UpdateTermAsync both cleaned).
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

### Student Status System (30 Aug)
```
Active:        Green dot (#27AE60) + "Active" label
Inactive:      Color dot by cause + "Inactive" label + cause below
  Completed:   Blue (#2980B9)
  Expelled:    Red (#E74C3C)
  Left School: Orange (#F39C12)
  Other:       Red (#E74C3C)
Button logic:  Active → Shift/View/Edit/Terminate; Inactive → Restore (blue)
Restore:       Sets IsActive=true on existing record, clears termination
Counts:        All queries filter by .IsActive (Dashboard fixed 30 Aug)
```

### x:Bind Code-Behind Pattern (30 Aug)
```csharp
// When x:Bind targets a property not on the ViewModel:
// Add forwarding property on code-behind, initialize VM BEFORE InitializeComponent()
public ObservableCollection<Student> FilteredStudents => _vm.FilteredStudents;
public ReportCardsView() {
    _vm = new StudentsViewModel();  // BEFORE InitializeComponent
    InitializeComponent();
    DataContext = ViewModel;
}
```

### PDF Export (30 Aug)
```csharp
// QuestPDF for report card PDF generation
// Export PDF: FileSavePicker → combined PDF
// Print All: FolderPicker → individual PDFs per student
// Print: PrintManager pipeline → Windows Print dialog
```

---

## Next Actions — Remaining Gaps

| # | Task | Priority |
|---|------|----------|
| 1 | Commit working tree (5-state roles, multi-subject, print, term fix) | P1 |
| 2 | Integration tests for subject-aware marks (`UpdateMarkAsync` overloads) | P2 |
| 3 | UNIQUE(AssessmentId, SubjectId) index on AssessmentSubject | P3 |
| 4 | Print preview pagination — page navigation for multi-student prints | P3 |
| 5 | Re-enable dormant admin role-gating before production | P2 (pre-prod) |

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
| 28 Aug | **5-state promotion roles + multi-subject assessments** | PromotionRole 5-state model, report-card/promotion classification rework, multi-subject assessments (link table, subject-aware marks, dynamic subject filter at marks entry), print hardening (PDF/any printer), active-term user control, UI polish |
| 30 Aug | **Build fixes + Student status + PDF export + UI** | XAML compiler/Binding/crash fixes, nullable warnings, student status system (inactive display/restore/colors), filter bar layout, term management enhancements, shift stream filtering, teachers table spec, assessments independent filters, ReportCards PDF export (QuestPDF), Print All individual PDFs, moderation removed |
| 4 Sep | **Authentication system + role-based access** | Admin first-run registration, invite-code data-entrant sign-up (single-use, 30-day expiry), SHA-256 salted password hashing, DB-backed sign-in, role-based sidebar gating, per-user AllowedPages restrictions persisted on UserEntity (schema patch), invite-code generate/copy/revoke UI in School Settings, registration nullability/resource fixes |
| 5 Sep | **Auth hardening + test-project fix** | Restricted entrants land on first granted page (no blank frame/dialog on login); NavigateToShellPage central guard (AdminOnlyRouteTags + CurrentUserMayAccess) closes top-search/quick-action bypass; tour auto-start + Take Tour hidden for restricted users; demo mode signs in any credentials as Administrator; IntegrationTests.csproj now ImplicitUsings=enable (test project compiles linked sources — was broken since SimpleLookup gained DateTime? EndDate); build 0 errors, 9/9 tests passing |

---

*Last updated: 5 Sep 2026 — Buffy (Codebuff agent)*
