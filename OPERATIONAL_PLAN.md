# Operationalization Plan for AutoTable

## Understanding
You want AutoTable to be a single-source-of-truth desktop app where every CRUD operation (students, teachers, assessments, marks, fees, etc.) persists to the SQLite database. UI lists, charts and print previews must read directly from the DB. Right now some pages fall back to an in-memory mock and some UI actions do not persist or return created records.

## Assumptions
- The repository in workspace is the working app; runtime currently uses %TEMP%\autotable.db by default and falls back to a MockDataServiceAdapter when DB init fails.
- You want the app to persist data between runs (production-like behavior) and not rely on the mock adapter for real data.
- You are comfortable with changes to startup, data service wiring, and small UI fixes to ensure created entities are returned and displayed.

## Goals
- Ensure DatabaseDataService is the canonical data provider and is always registered on startup (or fail fast with clear diagnostics).
- Move DB to a persistent app data location by default; support an explicit developer/test flag for ephemeral DB.
- Implement full CRUD paths from UI -> ViewModel -> IDataService -> Database (and return created records to UI to update collections in-place).
- Wire print/preview functionality to read from DB queries rather than ad-hoc in-memory data.
- Replace Mock fallback with a clearly labeled demo mode, or make mock adapter persist in-memory for session-only tests.

## Key files (where to focus)
- App.xaml.cs — startup, DB connection, AppServices.DataService registration
- AutoTable/Data/AppDbContext.cs — EF model and relationships (schema)
- AutoTable/Data/SeedData.cs — seed data used at first run
- AutoTable/Services/DatabaseDataService.cs — EF-backed IDataService implementation (business logic + DB access)
- AutoTable/Services/MockDataServiceAdapter.cs — current mock adapter fallback
- AutoTable/AppServices.cs — global static holder for IDataService
- Views/* and ViewModels/* — UI pages and viewmodels that call IDataService (StudentsViewModel, EnrollmentViewModel, AssessmentsViewModel, ReportCardsViewModel, etc.)

## High-level approach
1. Make DB persistent by default (use %LOCALAPPDATA%\AutoTable\autotable.db) and keep a dev flag for ephemeral mode. This prevents unexpected lost data and aligns UI behavior with DB state.
2. Harden App startup: attempt DB init; if it fails, write a clear diagnostic dialog and either exit or allow developer demo mode. Avoid silently substituting mock adapter unless explicitly requested.
3. Review all ViewModels and ensure every create/update/delete operation calls IDataService and that the IDataService implementation (DatabaseDataService) returns the created/updated model (with assigned Id/metadata). Update callers so they insert/update collections rather than reloading unnecessarily.
4. Ensure StudentsViewModel.LoadAsync orders students by CreatedAt desc (or desired order) so newly created records appear at top when List is reloaded.
5. Implement missing CRUD endpoints in DatabaseDataService for teachers, financials, and any other domain objects defined by UI. Add necessary entity classes and migrations if missing.
6. Replace ad-hoc print data builders with queries to IDataService that produce printable models; keep preview UI but populate it from DB queries.
7. Add error reporting to the UI (show dialogs or toasts) when DB operations fail, and log detailed exception text to temp logs for troubleshooting.
8. Decide on migration strategy: continue using EnsureCreated for dev but add EF Migrations for schema evolution in production.

## Concrete Steps
1. Persist DB: change devDbFile in App.OnLaunched to Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoTable", "autotable.db"). Ensure directory created.
2. Add a configuration flag (app settings or compile-time constant) DEV_EPHEMERAL_DB to control whether DB is temporary. Document how to flip it for testing.
3. Make App.OnLaunched fail loudly if DB initialization throws — show a dialog with instructions (or allow an explicit demo mode). Remove silent auto-register of mock adapter unless demo mode requested.
4. Audit DatabaseDataService: ensure CreateXAsync methods set and return the created model (with Id and CreatedAt). Fix any methods that return void or no value. (Assessments CreateAssessmentAsync currently returns void — change it to return created AssessmentItem or id.)
5. Update ViewModels to consume return values and update observable collections accordingly (insert created entity at index 0). Example: EnrollmentViewModel should return Student created; StudentsViewModel should insert it (already implemented).
6. Implement Teacher entity and IDataService methods (GetTeachersAsync, CreateTeacherAsync, UpdateTeacherAsync, DeleteTeacherAsync) with DB entity and migrations.
7. Review and update printing code (ReportCardsView, ReportCardsViewModel, Print helpers) to call IDataService to fetch printable data (GetGradebookAsync used already — ensure it reads from DB and represents current data). Replace any manual lists with DB queries.
8. Add missing CRUD endpoints for Assessments (CreateAssessmentAsync returns created item), Marks (AddMarkAsync/UpdateMarkAsync/DeleteMarkAsync), Fees (CreateFeePaymentAsync), and ensure ViewModels call these and show results.
9. Add unit/integration tests for key flows (create student -> read student list; add assessment -> appears in assessments list; enter mark -> reflected in gradebook and report cards). Use an in-memory SQLite provider for tests.
10. Add diagnostics: log database initialization error file to %LOCALAPPDATA% or show dialog at startup with path to log.
11. Clean up MockDataServiceAdapter: either remove fallback or make it explicit demo mode; if keeping, implement persistence of created entities in-memory for session (so creates are visible until app exit).
12. Run manual QA checklist and fix UI wiring (buttons, commands): ensure each button maps to a ViewModel command which invokes IDataService and updates UI.

## Risks & Open Questions
- Current code deletes and recreates DB on each run — this destroys data. Confirm whether you want persistent DB or ephemeral for dev.
- Some Create methods currently return void (Assessments) — callers expect immediate state change; need to update API and callers.
- Mock fallback hides DB problems during debugging. Decide whether to keep or remove.
- Print pipeline must accept large datasets; use pagination or streaming for large exports.

## Minimal priority implementation plan (atomic steps)
1. Change DB path to LocalApplicationData and ensure directory exists. (App.xaml.cs)
2. Remove automatic mock fallback; add explicit demo mode flag. (App.xaml.cs)
3. Update DatabaseDataService: change CreateAssessmentAsync to return created AssessmentItem; add CreateFeePaymentAsync, AddMarkAsync, CreateTeacherAsync. (AutoTable/Services/DatabaseDataService.cs)
4. Update IDataService interface to reflect new return types. (AutoTable/Services/IDataService.cs)
5. Update ViewModels: AssessmentsViewModel, ReportCardsViewModel, StudentsViewModel, EnrollmentViewModel to use returned objects and update ObservableCollections by inserting at index 0. (ViewModels/*)
6. Implement Teacher entity and DbSet in AppDbContext and migration (AutoTable/Data/Entities, AppDbContext). Add seed data. (AutoTable/Data/*)
7. Wire printing to IDataService queries (ReportCardsViewModel -> GetGradebookAsync). Ensure Print UI uses that returned data. (Views/ReportCardsView.xaml.cs)
8. Add UI error dialogs on all submit/update operations to surface DB errors. (View code-behind or centralized error service)
9. Add integration tests using SQLite in-memory for basic CRUD flows.
10. QA and iterate: manual tests for each quick action and button.

## Commands & checks
- Inspect runtime DB path and file: (PowerShell)
  $db = Join-Path $env:LOCALAPPDATA "AutoTable\autotable.db"; Test-Path $db
- Inspect tables with sqlite3 CLI:
  sqlite3 $db ".tables"
  sqlite3 $db ".schema Students"
- To run app in ephemeral dev mode, use existing temp path (currently implemented). To switch to persistent, change App.OnLaunched as above.

---

If you confirm I should proceed, I will:
1) Create the plan file in the repo root (this file).  
2) Implement step-1: change DB to LocalApplicationData and ensure directory creation.  
3) Implement step-3: update DB initialization to surface fatal errors and make mock fallback explicit.  
4) Make minimal API adjustments required to ensure CreateAssessment and other create ops return created model and ensure ViewModels insert created items at top.

Which steps do you want me to start implementing now? (I recommend step-1 then step-3.)