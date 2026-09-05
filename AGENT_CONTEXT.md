# AutoTable - Agent Context Export

This file summarizes the current workspace state, recent changes, and run/setup instructions so another agent (or automation) can continue work.

> Paths in this document are relative to the repository root (checked out on this machine at `C:\Users\manue\Desktop\AutoTable_Prod\AutoTable\`).

---

## Environment

- OS/IDE: Microsoft Visual Studio Community 2026 (18.7.3)
- Project target: .NET 8
- Solution file: `AutoTable.slnx`
- Active branch: `sql_rec` (origin: https://github.com/ManuelPrhyme/AutoTable) — carries the Sep 4 auth-system work; `trans` is 6 commits behind
- UI framework: WinUI 3 (Windows App SDK 2.3.x)
- Build command: `dotnet build AutoTable.csproj -p:Platform=x64` → **0 errors**
- Test command: `dotnet test Tests/AutoTable.IntegrationTests -p:Platform=x64` → **9/9 passing**

---

## High-level goal

AutoTable is a single-source-of-truth desktop school management app where every CRUD operation persists to SQLite via EF Core. The operational plan (OPERATIONAL_PLAN.md) is **fully implemented** for Phases 2–5. The current work is on the **P5 feature backlog**: grading system integration, assessment promotion roles, promotion/repeat flow, class management enhancements, role gating, analytics, enforcement, and **UI/UX polish**.

---

## Current State (5 Sep 2026)

### Build: 0 errors, clean

### What's Done (all operational plan steps)
- Persistent DB at `%LOCALAPPDATA%\AutoTable\autotable.db` with dev ephemeral flag
- Fail-loud startup with diagnostics, explicit demo mode only
- Full CRUD: Students, Teachers, Classes, Subjects, Terms, Assessments, Marks, Fees, Budget
- All ViewModels use `AppServices.DataService` (no mock fallback)
- A4 report-card printing (native Windows PrintManager)
- **Report Cards PDF export** — per-student and batch PDF generation via QuestPDF (modular components); Export PDF saves all to one file, Print All saves each student as a separate PDF, Print opens Windows Print dialog
- Grading systems domain (named scales with promotion/repeat bands)
- Class creation single-modal rework (teacher + grading system + streams/subjects)
- **Class edit modal** with full metadata editing (name, teacher, grading system) + streams/subjects
- **Immediate subject/stream persistence** — new items saved to DB on Add click, not deferred
- **Streams Add button fix** — priority logic corrected, button styled
- Integration tests (9 passing)
- **P5.1: Assessment promotion role** — 5-state enum (see mapping below) + creation dialog + report card classification
- **P5.2: Promotion/repeat flow** — full UI with promote/repeat/shift/reset + batch "Process All"
- **P5.3: Grade resolution via grading systems** — `GradeFromBands` replaces `GradeFromAverage`
- **Multi-subject assessments** — ONE assessment per scope (Single/AllInClass/SpecificSubjects/AllInSchool); marks keyed per subject via Marks.SubjectId + AssessmentSubject link table; marks entry filters Class → Assessment → Subject (subject filter appears only for multi-subject papers); completion = students x linked subjects; idempotent patches + one-time mark back-fill
- **Print hardening** — system print dialog (all installed printers + Microsoft Print to PDF), A4 portrait/color defaults, failure guard with clear message
- **Print modal printer picker** — modal detects configured printers (`PrinterService` via GDI `PrinterSettings.InstalledPrinters`), lets the user choose one for single or batch print; prints directly to that printer via GDI `PrintDocument` (2x-rendered pages), or falls back to the OS dialog
- **Term lifecycle fix** — IsActive is purely user-controlled; no end-date auto-deactivation (App.xaml.cs startup + UpdateTermAsync)
- **TermFees table fix** — schema migration for existing DBs
- **Financial Dashboard KPI merge** — term selector + merged KPI cards
- **Fee Collection per-student amounts** — Expected/Paid/Balance per student/class/term
- **Record Payment modal white text** — all text on dark dialog
- **LIN uniqueness gap fix** — 3-layer defense against orphaned enrollment records
- **Multi-subject NOT NULL constraint fix** — SQLite table-rebuild patch (`PatchAssessmentsNullableSubject`) makes `Assessments.SubjectId` nullable on legacy DBs (the "create multi-subject assessment" runtime error); stream IDs added via `PatchAssessmentsStreamIds` (StreamIdsCsv)
- **Stream-granularity assessments** — new `AssessmentScope.Stream` (value 4) + `AssessmentItem.StreamIds`/`StreamNames`; stream papers target 1-N streams, persist StreamId (first) + StreamIdsCsv (all); marks entry rosters all target streams
- **Expandable create-assessment modal** — initial display = 3 scope radio checkboxes (Entire School / Class / Stream); selecting a scope expands the details panel (name, class picker, Stream picker for stream scope, subject granularity, promotion role, weight, due date); entire modal scrollable (`ScrollViewer MaxHeight=460`, `ContentDialog MaxHeight=620`); `CalendarDatePicker` replaces the 3-list date picker; weight left-aligned
- **Assessments — independent Stream/Subject filters** — changing Subject no longer resets Stream and vice versa; only Class changes reload option lists
- **Marks Entry stream filter** — stream-scoped assessment shows a Stream dropdown (target streams + All) before Subject; `GetStudentMarksAsync` gained optional `streamName`
- **Assessments sorted by creation date** — `AssessmentEntity.CreatedAt` + `PatchAssessmentsCreatedAt` (back-fills due-date proxy); `GetAssessmentsAsync` orders `OrderByDescending(CreatedAt).ThenByDescending(Id)`
- **Students list overhaul** — status column (green dot Active / red/orange/blue dot Inactive with cause below "Inactive"), Terminate button (red, white text, last) with reason modal (Completed course / Expelled / Left school / Other), View button (read-only full-info modal), Edit button (editable modal), Shift button (class + stream change); column headers added; third column (Class) removed; `Student` gains `StatusText`, `InactiveCauseText`, `StatusDotSource`, `StatusLabel`, `TerminationYear`
- **Marks Entry table header** — Changed "ADM NO." to "LIN" in the column header
- **Student Restore** — Inactive students show Restore button (blue) instead of Shift/View/Edit; Restore sets IsActive=true on existing record, clears termination state
- **Active student counts** — all counts filtered by `.IsActive` (Dashboard fixed; marks, fees, report cards, performance already correct)
- **Student filter bar layout** — Search bar between filters and Add Student button (24px spacer); Add Student pushed to absolute right; Search bar widened (MinWidth=220)
- **Sidebar branding** — title shows the configured School Name, subtitle shows the school motto, blue shape hosts the logo image from Settings LogoBytes (falls back to blue glyph)
- **Grading system UI** — Delete becomes `DangerGhostButtonStyle` (stays red on hover, white on focus/pressed); `BandsSummary` renders `A - 90-100` (hyphen between designation and marks range)
- **Toast infrastructure added then disabled (dev)** — `ToastService`/`NotificationStore` created + `AppServices.Toasts` wired, then every call site commented out (dev mode); infra left as dead types for later re-enable
- **Term Management header** — shell header updates to "Term Management" when navigating to term management page
- **Term activeness persistence** — Set Active button hidden when term is active (Deactivate remains); both hidden when term has ended; button visibility controlled by `UpdateActiveIndicators()`
- **Teachers table layout** — Applied Table.md spec: column widths (2*/1.5*/1.5*/1.5*/2*/1.2*), header padding 16,10, row padding 16,8, headers left-aligned, MaxHeight=480
- **Status dot colors hardcoded** — Direct `SolidColorBrush` construction (Green=#27AE60, Blue=#2980B9, Orange=#F39C12, Red=#E74C3C) instead of ThemeResourceHelper lookup
- **🆕 User Interface Tour** — Interactive guided tour overlay that walks new users through the entire application. Features include:
  - **15 tour steps** covering all major areas: Dashboard, Assessments, Marks Entry, Gradebook, Students, Teachers, Classes, Term Management, Report Cards, Fee Collection, AI Insights, School Settings, Term Selector, Global Search, and Theme Toggle
  - **Dimming overlay** — semi-transparent black background dims everything except the highlighted element
  - **Spotlight highlight** — blue-bordered spotlight cutout reveals the target UI element
  - **Popup card** — positioned contextually (Right/Bottom/Left/Top) with icon, title, step counter, description, and progress dots
  - **Navigation buttons** — Skip Tour (left), Next/Finish (right), Close (X)
  - **First-launch auto-start** — tour triggers automatically on first app launch (persisted via local settings)
  - **Manual re-access** — "Take Tour" button in the sidebar below School Settings
  - **First-launch setup integration** — after the tour finishes, the grading system → class → term setup sequence runs automatically if the database is empty
  - **Smooth entrance animation** — popup slides up with opacity fade via Composition APIs
- **🆕 Authentication & role-based access (4 Sep)** — full auth system:
  - First-run admin registration (`AdminRegistrationView` — auto-shown when no Administrator exists) with SHA-256 + salt hashing (`PasswordHelper`)
  - Invite-code data-entrant sign-up (`DataEntrantRegistrationView`) with `InviteCodeEntity` storage, expiry, and single-use enforcement
  - DB-backed sign-in (`AuthService.SignInAsync`) — username = stored email, verified against the password hash
  - Role-based sidebar: admin-only pages (Term Management, Promotion, Audit Log, Budget, School Settings) hidden for non-admins
  - Per-user page restrictions: invites carry `AllowedPages`; persisted on `UserEntity` (schema patch) and enforced in the sidebar + on every route
  - School Settings invite-code generation (label + access-level presets), copy-to-clipboard, revocation, and a generated-codes list
  - Route guards centralized in `NavigationService.AdminOnlyRouteTags` + `CurrentUserMayAccess` (5 Sep)
- **Auth hardening (5 Sep)** — restricted entrants now land on the first page their invite grants instead of a blank frame + "Access Restricted" dialog; `NavigateToShellPage` refuses disallowed routes (top search / quick actions / setup flows can no longer bypass gating); the tour auto-start and "Take Tour" button are hidden for restricted users whose `AllowedPages` exclude tour pages; demo mode (no DB) accepts any credentials as Administrator so it stays usable

### What's Next (P5 backlog)
| # | Feature | Status |
|---|---------|--------|
| P5.4 | Admin role-gating | ✅ **DONE (4 Sep)** — full auth system + role gating, see Authentication section below |
| P5.5 | Defaulters / cohort analytics | 🔴 Not started |
| P5.6 | Mid-term slips | 🔴 Not started |
| P5.7 | Active-term enforcement | 🔴 Not started |

---

## Key Files

### Data Layer
- `AutoTable/Data/Entities/StudentEntity.cs` — ALL EF entities (Student, Class, Stream, Subject, Term, AcademicYear, Assessment, Mark, FeePayment, User, ClassSubject, ClassStream, TermFee, BudgetLine, GradingSystem, GradeBand, TerminationLog, Enrollment, InviteCode); `UserEntity` carries `AllowedPages`
- `AutoTable/Data/AppDbContext.cs` — EF Core DbContext with ForeignKeyInterceptor
- `AutoTable/Data/SeedData.cs` — NOT called at startup (by design)

### Services
- `AutoTable/Services/IDataService.cs` — full async interface (students, assessments, marks, grades, fees, teachers, budget, grading systems, promotion, report cards, class updates)
- `AutoTable/Services/DatabaseDataService.cs` — EF Core implementation (~2000 lines)
- `AutoTable/AppServices.cs` — global static IDataService holder (`Toasts` property commented out in dev)
- `AutoTable/Services/ToastService.cs` / `NotificationStore.cs` — dead types (toast infra, all call sites commented for dev)
- **🆕 `Services/UserTourService.cs`** — singleton managing tour state, step definitions, and first-launch persistence via `Windows.Storage.ApplicationData.LocalSettings`
- **🆕 `Services/AuthService.cs` / `IAuthService.cs`** — DB-backed admin registration, sign-in, and invite-code sign-up; `Services/PasswordHelper.cs` — SHA-256 + per-user salt; `Services/SessionService.cs` — current user, `IsAdministrator`, `AllowedPages`, first-launch flags

### Models
- `Models/AssessmentItem.cs` — `AssessmentScope` enum, `AssessmentPromotionRole` enum, `AssessmentItem` DTO
- `AutoTable/Models/GradingSystemModels.cs` — `GradingSystemInfo`, `GradeBandInfo`
- `Models/ReportCardModels.cs` — `ReportCardSheetModel`, `ReportCardAssessmentRow`
- `AutoTable/Models/ClassInfo.cs` — ClassInfo DTO with `ClassTeacherId` and `GradingSystemId`
- `AutoTable/Models/Student.cs` — Student DTO with enrollment fields, `StatusText`, `InactiveCauseText`, `StatusDotSource`, `StatusLabel`
- `AutoTable/Models/SimpleLookup.cs` — Id/Name DTO for lists (now includes optional `IsActive` and `EndDate` for term lookups)
- **🆕 `Models/TourStep.cs`** — Tour step data model (TargetElementName, Title, Description, IconGlyph, PopupPosition)

### Views & ViewModels
- `Views/ClassesView.xaml.cs` — class management (single-modal create, edit modal with name/teacher/grading/streams/subjects, white chip UI with ✕ buttons)
- `Views/AssessmentsView.xaml.cs` — expandable/scrollable assessment creation dialog (scope checkboxes Entire School / Class / Stream; class + stream pickers; subject granularity; promotion-role selector; CalendarDatePicker)
- `ViewModels/AssessmentsViewModel.cs` — assessment page filters (independent Stream + Subject; Subject restricted to the selected class's subjects; Stream filter always visible)
- `Views/MarksEntryView.xaml` / `ViewModels/MarksEntryViewModel.cs` — marks entry with Class → Assessment → Subject ordering + conditional Stream filter (stream-scoped papers)
- `AutoTable/Views/StudentsView.xaml(.cs)` — student list with status dots (color-coded by cause), column headers, Restore (blue) / Shift / View / Edit / Terminate (red) buttons + modals; FilteredStudents exposed via code-behind for x:Bind
- `Views/FeeCollectionView.xaml.cs` — fee collection with Record Payment modal (searchable typeahead, white text)
- `Views/ReportCardsView.xaml.cs` — A4 report card preview + PrintManager printing + QuestPDF PDF generation via `ReportCardPdfGenerator`; Print per-student opens Print dialog; Print All saves individual PDFs via FolderPicker; Export PDF saves combined PDF via FileSavePicker
- `Views/Controls/ReportCardSheetView.xaml` — A4 report card sheet control
- `Views/ShellView.xaml(.cs)` — navigation shell with role-gated sidebar; loads School Name/Motto/Logo into the brand block; includes TermManagement in PageMeta; **🆕 hosts the UserTourOverlay and wires auto-start + manual "Take Tour" button**
- `Views/TermManagementView.xaml(.cs)` — term management with active/ended button visibility logic
- `Resources/DesignTokens.xaml` — theme resources incl. `DangerButtonStyle`, `DangerGhostButtonStyle`, `DangerProgressBarStyle`
- `Converters/FormatConverters.cs` — converters incl. `StatusColorConverter` (hardcoded colors: Active=Green, Completed=Blue, Expelled=Red, ChangedSchool=Orange), `StatusLabelConverter` (all terminated → "Inactive"), `GradeColorConverter`, `BoolToVisibilityConverter`, `BoolToVisibilityNegateConverter`
- **🆕 `Views/Controls/UserTourOverlay.xaml(.cs)`** — the tour overlay UserControl with dimming Grid, spotlight Border, popup card (icon + title + description + progress dots + navigation buttons), and step-positioning logic

### Startup
- `App.xaml.cs` — DB init, connection string, schema patches (ALTER TABLE for legacy DBs, TermFees table creation), ForeignKeyInterceptor registration

---

## Authentication & Access Control (4–5 Sep)

```
Startup (App.xaml.cs)
  └─ no Administrator in Users? → AdminRegistrationView (first-run setup)
  └─ else → LoginView (SignInAsync → SessionService.SetUser)

School Settings (admin)
  └─ Generate Code → InviteCodes row (Role=DataEntrant, AllowedPages preset, 30-day expiry)
  └─ entrant registers → UserEntity row with AllowedPages copied from invite; invite marked used

ShellView sidebar gating (per session)
  ├─ Admin: everything visible
  ├─ Data entrant + AllowedPages set: only granted nav items visible (Dashboard etc.)
  └─ Data entrant without AllowedPages: all non-admin pages

Route enforcement (defense in depth)
  ├─ NavigationService.CurrentUserMayAccess(tag) — checked in NavigateToShellPage
  │   (top search, dashboard quick actions, setup flows) BEFORE the frame switches
  ├─ ShellView.NavigateTo (sidebar clicks) — dialog + refuse for admin-only / not-granted tags
  └─ ShellView.OnExternalShellNavigated — chrome-sync guard after external navigation

Landing page: admins/unrestricted → Dashboard; restricted entrants → first page their
AllowedPages grant (PreferredStartTags order), so login never yields a blank frame.

Demo mode (AUTOTABLE_DEMO_MODE=true): no DB users; sign-in accepts any non-empty
credentials as Administrator.
```

### Files
| File | Role |
|------|------|
| `Services/AuthService.cs` | Admin registration, sign-in, invite-code sign-up; sets the session |
| `Services/PasswordHelper.cs` | `salt:hash` via SHA-256 + 16-byte random salt |
| `Services/SessionService.cs` | `CurrentUser`, `IsAdministrator`, sign-out, first-launch flags |
| `Services/NavigationService.cs` | Central route guard (`AdminOnlyRouteTags`, `CurrentUserMayAccess`) enforced before any shell navigation |
| `Views/AdminRegistrationView.xaml(.cs)` | First-run admin creation |
| `Views/DataEntrantRegistrationView.xaml(.cs)` | Invite-code sign-up for data entrants |
| `Views/LoginView.xaml(.cs)` | Username/password sign-in; link to data-entrant registration |
| `Views/SchoolSettingsView.xaml(.cs)` | Invite-code generate / copy / revoke + access-level presets (admin only) |
| `Views/ShellView.xaml(.cs)` | Sidebar role gating, AllowedPages visibility, landing-page selection, tour gating |

---

## User Tour Architecture

### Tour Flow

```
ShellView_Loaded
  ├─ First launch (HasCompletedTour = false)
  │   └─ await Task.Delay(600) → TourOverlay.StartTour(this)
  │       └─ TourOverlay_TourFinished → RunFirstLaunchSetupIfNeeded()
  │           └─ Empty DB? → NavigateTo Classes → grading system → class → term setup
  └─ Subsequent launches (HasCompletedTour = true)
      └─ RunFirstLaunchSetupIfNeeded() immediately
```

### Tour Steps (15 total)
1. Dashboard — sidebar nav
2. Assessments — assessment management
3. Marks Entry — mark input
4. Gradebook — consolidated view
5. Students — student records
6. Teachers — teacher management
7. Classes — class/subject setup
8. Term Management — academic terms
9. Report Cards — report generation
10. Fee Collection — payments
11. AI Insights — recommendations
12. School Settings — branding
13. Term Selector — header dropdown (Bottom position)
14. Global Search — header search (Bottom position)
15. Theme Toggle — light/dark switch (Bottom position)

### Files
| File | Role |
|------|------|
| `Models/TourStep.cs` | Data model: target element name, title, description, icon, popup position |
| `Services/UserTourService.cs` | Singleton: step definitions, completion persistence (`LocalSettings`), first-launch detection |
| `Views/Controls/UserTourOverlay.xaml` | XAML: dimming Grid, spotlight Border, popup card, Skip/Next/Close buttons, progress dots |
| `Views/Controls/UserTourOverlay.xaml.cs` | Code-behind: step navigation, spotlight/popup positioning, entrance animation, first-launch setup integration |
| `Views/ShellView.xaml` | Hosts `<controls:UserTourOverlay>` (Grid.ColumnSpan=2, ZIndex=9999) + "Take Tour" sidebar button |
| `Views/ShellView.xaml.cs` | Wires auto-start, manual trigger, `TourOverlay_TourFinished` → `RunFirstLaunchSetupIfNeeded()` |

---

## Grading System Resolution

When resolving grades for a class:
```
Class.GradingSystem → GradingSystems.FirstOrDefault(IsDefault) → first available system → hard-coded 50% fallback
```

`GradeFromBands(mark, bands)` replaces `GradeFromAverage(mark)`. If no bands exist, falls back to the old A-F scale.

---

## PromotionRole Mapping (5-state, since 28 Aug)

```
AssessmentPromotionRole.None                  → int 0  (default: "Just an Assessment")
AssessmentPromotionRole.CountsTowardPromotion → int 1  (Contributory End of Year / Promotional)
AssessmentPromotionRole.PromotionExam         → int 2  (End of Year / Promotional)
AssessmentPromotionRole.EndOfTerm             → int 3  (End of Term)
AssessmentPromotionRole.ContributoryEndOfTerm → int 4  (Contributory End of Term)
```

Legacy ints 0/1/2 preserved; no migration needed. The creation dialog resolves the
selected option through a parallel enum array (display order differs from int order).

Report card classification:
- If any assessment has `PromotionRole != 0`: EndOfTerm/PromotionExam → promotional table;
  ContributoryEndOfTerm/CountsTowardPromotion → contributory table; None → excluded.
- Otherwise (legacy data): fallback to weight-based heuristic
- Multi-subject assessments fan out to one report-card row per linked subject.

Year-end promotion average (`ComputeStudentAverageAsync`): counts ONLY end-of-year
items (PromotionExam = deciding mark, CountsTowardPromotion averaged); end-of-term
roles and None are excluded. Groups by the MARK's subject (`Marks.SubjectId` first).

Admin role-gating (P5.4): gates are coded on all promotion commands but COMMENTED
OUT (dev mode). Uncomment `SessionService.IsAdministrator` checks in
`PromotionViewModel` + `ShellView.xaml.cs` before production.

---

## Student Status System

| Status | Dot Color | Label | Cause Display |
|--------|-----------|-------|---------------|
| Active | 🟢 Green (#27AE60) | "Active" | — |
| Inactive (Completed course) | 🔵 Blue (#2980B9) | "Inactive" | "Completed course" below |
| Inactive (Expelled) | 🔴 Red (#E74C3C) | "Inactive" | "Expelled" below |
| Inactive (Left school) | 🟠 Orange (#F39C12) | "Inactive" | "Left School" below |
| Inactive (Other) | 🔴 Red (#E74C3C) | "Inactive" | "Other" below |

- All terminated students show "Inactive" (not their specific status)
- Specific cause displayed below the word "Inactive" in the table
- Status dot colors are hardcoded `SolidColorBrush` values (not theme-dependent)
- Inactive students excluded from all active-student operations (marks, fees, report cards, performance, dashboard counts)
- Historical records (marks, fees, performance) remain intact for inactive students
- Inactive students show Restore button (blue) instead of Shift/View/Edit/Terminate
- Restore sets `IsActive=true` on existing record — no new student created

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

*Last updated: 5 Sep 2026 — Buffy (Codebuff agent)*
