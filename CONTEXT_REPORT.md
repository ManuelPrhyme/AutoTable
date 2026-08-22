# AutoTable - Context Report (Updated 21 Aug 2026)

Session report covering work performed against AutoTable/OPERATIONAL_PLAN.md, the full codebase assessment, and the agreed implementation roadmap.

- Repo root: c:\Users\manue\Desktop\Desktop_Apps\AutoTable
- Branch: sql_rec (origin: https://github.com/ManuelPrhyme/AutoTable.git)
- Target: .NET 8 / WinUI 3 (Windows App SDK 2.3.0), build platform x64
- Last known commit: 2cf534ad3ddfd7be903c40c1158029e01bbbe890

## 1. The Operational Plan

OPERATIONAL_PLAN.md makes AutoTable a single-source-of-truth desktop app where every CRUD operation (students, teachers, assessments, marks, fees, etc.) persists to the SQLite database. UI lists, charts and print previews read directly from DB. Core goals:

1. DatabaseDataService is the canonical data provider, always registered on startup (fail fast with diagnostics).
2. DB moves to a persistent app-data location (%LOCALAPPDATA%\AutoTable\autotable.db); dev flag for ephemeral DB.
3. Full CRUD paths UI to ViewModel to IDataService to Database, returning created records to update collections in-place.
4. Print/preview reads from DB queries, not ad-hoc in-memory data.
5. Replace Mock fallback with a clearly labeled demo mode, or remove.

Key files: App.xaml.cs (startup, DB connection, registration), AutoTable/Data/AppDbContext.cs (EF schema), AutoTable/Data/SeedData.cs (seed - NOT called by design), AutoTable/Services/DatabaseDataService.cs (EF business logic + DB), AutoTable/AppServices.cs (global static holder), AutoTable/Services/IDataService.cs (interface), Views/* and ViewModels/*.

## 2. Phase Roadmap (from Prompts/Cursor Prompt.txt)

- Phase 1: Login, SignUp, Dashboard - DONE
- Phase 2: Assessments, MarksEntry, Gradebook - UI done, CRUD write paths incomplete
- Phase 3: StudentPerformance, Analytics, ReportCards - UI done, DB reads wired
- Phase 4: Moderation, AI Insights + admin pages (Classes, Students, Audit) - Partial, stubs remain
- Phase 5: Financials (Fin.Dashboard, FeeCollection, Budget) - Not started
- Phase 6: PostgreSQL, VBA/PowerAutomate - Not started (skipped)

## 3. Implementation Status (audit, 21 Aug 2026)

### DONE vs Operational Plan
- Step 1 Persistent DB path (LocalApplicationData + directory creation)
- Step 2 No silent mock fallback (temp log + dialog + abort on init error)
- Step 3 DatabaseDataService always registered; ViewModels throw if null
- Step 4 CreateStudent returns created model; Enrollment returns via callback and StudentsView inserts at index 0
- Step 7 Print/gradebook/report cards read from DB (GetGradebookAsync)
- Termination + audit log transactional
- Student termination, audit trail
- Classes management (create/delete/assign classes, subjects, streams)
- Navigation Shell registers all pages
- Diagnostics and startup error handling

### NOT DONE vs Operational Plan
- DEV_EPHEMERAL_DB flag (not implemented)
- CreateAssessmentAsync returns void (not Task of AssessmentItem); callers cannot insert created item
- Teacher entity + CRUD (no TeacherEntity, no teacher methods anywhere)
- Marks CRUD missing (AddMark/UpdateMark/DeleteMark absent; MarksEntryViewModel.SubmitMarks and SaveDraft are no-op stubs)
- Fee CRUD missing (no CreateFeePaymentAsync; FeeCollectionViewModel.RecordPayment is no-op stub)
- Moderation: ApproveAll in-memory only; Publish empty stub; no UpdateAssessment/Verify/Publish methods
- StudentsViewModel orders by Name; plan requests CreatedAt desc
- EF Migrations (only EnsureCreated)
- MockDataService.cs (root Services) is dead code, not labelled as demo
- FinancialsDashboardViewModel, BudgetViewModel and AiInsightsViewModel are hardcoded (not DB-wired)

### BY DESIGN (not a gap)
- SeedData.cs is NOT called from App.xaml.cs. Users configure their own classes/subjects/terms/streams via the Classes screen. The DB starts empty (this is why Assessment creation uses FirstOrDefaultAsync which will throw until a class/subject/year/term exist).

## 4. Detailed Operational Plan Step-by-Step Status

Concrete Steps (from OPERATIONAL_PLAN.md):
1. Persist DB path to LocalApplicationData and ensure directory - DONE in App.xaml.cs (lines ~55-57).
2. Add DEV_EPHEMERAL_DB config flag for ephemeral dev mode - NOT DONE.
3. Make App.OnLaunched fail loudly on DB init error; remove silent mock auto-substitution - DONE (error dialog + temp log + abort).
4. Audit DatabaseDataService: change CreateAssessmentAsync to return created AssessmentItem; fix void returns - IN PROGRESS (interface pending update).
5. Update ViewModels to consume return values and insert at index 0 - partially done for Students/Enrollment; AssessmentsView pending.
6. Teacher entity and IDataService methods + migration - NOT DONE.
7. Re-wire printing to IDataService queries - DONE (ReportCards uses GetGradebookAsync).
8. Missing CRUD: CreateAssessmentAsync returns created item; Marks (AddMark/UpdateMark/DeleteMark); Fees (CreateFeePaymentAsync); ViewModels call them - PENDING.
9. Integration tests with in-memory SQLite - NOT DONE.
10. Diagnostics logging for DB init errors - DONE (temp log on failure).
11. Clean up MockDataServiceAdapter: remove fallback or make explicit demo - pending (dead file exists).
12. Run manual QA checklist and fix UI wiring - pending.

## 5. Agreed Implementation Roadmap (User-Confirmed Order)

Build order confirmed by user: Phase 2 -> Phase 4 -> Phase 5 -> re-evaluate vs OPERATIONAL_PLAN.md.

### Phase 2 - Complete CRUD write paths
- Assessments: change CreateAssessmentAsync to Task of AssessmentItem (IDataService + DatabaseDataService); return created item with Id/metadata; AssessmentsView inserts created item at index 0 instead of full reload.
- Marks: add AddMarkAsync, UpdateMarkAsync, DeleteMarkAsync, GetAssessmentAsync, UpdateAssessmentCompletionAsync to IDataService + DatabaseDataService; wire MarksEntryViewModel.SaveDraft and SubmitMarks to persist marks and refresh completion.
- Students: change GetStudentsAsync to order by CreatedAt desc.

### Phase 4 - Moderation + AI Insights
- Add assessment update/verify/publish to IDataService + DatabaseDataService.
- Wire ModerationViewModel.ApproveAll to set IsVerified=true in DB; Publish to set IsPublished=true; add UI error handling in ModerationView.
- Wire AiInsightsViewModel to IDataService (derive at-risk alerts from gradebook).

### Phase 5 - Financials
- Add GetFeePaymentsAsync + CreateFeePaymentAsync to IDataService + DatabaseDataService.
- Wire FinancialsDashboardViewModel KPIs (total collected, outstanding) + recent transactions from DB.
- Fix FeeCollectionViewModel Load to use real fee data; wire RecordPayment to CreateFeePaymentAsync.
- Wire BudgetViewModel to DB.

### Then - remaining Operational Plan
- Step 6: Teacher entity + CRUD
- Steps 10/11: EF Migrations, clean up MockDataService, DEV_EPHEMERAL_DB flag
- Step 9: Integration tests (SQLite in-memory)

## 6. Files to be Modified

- AutoTable/Services/IDataService.cs
- AutoTable/Services/DatabaseDataService.cs
- Views/AssessmentsView.xaml.cs
- ViewModels/AssessmentsViewModel.cs
- ViewModels/MarksEntryViewModel.cs
- AutoTable/Models/StudentMarkRow.cs
- AutoTable/Models/AssessmentItem.cs
- AutoTable/ViewModels/ModerationViewModel.cs
- ViewModels/AiInsightsViewModel.cs
- ViewModels/FinancialsDashboardViewModel.cs
- ViewModels/FeeCollectionViewModel.cs
- ViewModels/BudgetViewModel.cs
- Views/ModerationView.xaml.cs
- AutoTable/Data/AppDbContext.cs (Migrations later)

## 7. Build & Environment

- Build command: dotnet build AutoTable.csproj -p:Platform=x64 --no-restore - succeeds (2 warnings: AssessmentsView.xaml.cs null dereference).
- Packages (v8.0.11): Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.Design; CommunityToolkit.Mvvm 8.4.2; Microsoft.WindowsAppSDK 2.3.1.
- No seed at startup (by design).
- MockDataService.cs (root Services) is dead/unreferenced; not used as runtime fallback.

## 8. Key Current Code References (for implementation)

- IDataService.CreateAssessmentAsync currently returns Task (void); needs Task of AssessmentItem.
- DatabaseDataService.CreateAssessmentAsync builds AssessmentEntity, calls SaveChangesAsync, returns nothing.
- MarksEntryViewModel.SaveDraft and SubmitMarksAsync are empty lambdas with TODO comments.
- ModerationViewModel.ApproveAll sets IsVerified on in-memory list only; Publish is an empty lambda.
- FinancialsDashboardViewModel uses hardcoded field values (_totalCollected = 48500000).
- FeeCollectionViewModel.RecordPayment is an empty relay command.