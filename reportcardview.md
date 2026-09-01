# Report Card Generation — Implementation Plan

This document describes the design and implementation plan for the A4 Report Card feature (PDF & on-screen sheet) using the codebase you provided. It is written for an external implementer who will finish wiring photos, polishing visuals and validating output.

## Understanding

- Goal: produce a printable A4 report card that matches the provided visual template (school header + ribbon, student photo, biographical block, promotional marks table, summary blocks, comments, grading key, signatures and footer). Output must be a single-page A4 PDF per student and an on-screen preview.
- The codebase already contains a ReportCardSheetView XAML control, a ReportCardsView that orchestrates print/export, and a modular QuestPDF-based generator (Reports/ReportCards/). School logo bytes are available in SchoolSettings; student photo bytes must be captured at enrollment and included into the report model.

## Assumptions

- The app targets .NET 8 (WinUI 3). QuestPDF is acceptable for server-side/pdf generation and is already added to the project.
- School logo bytes are available from IDataService.GetSchoolSettingsAsync() and are wired into ReportCardSheetModel.LogoBytes already.
- Database schema can be patched for legacy DBs via SchemaPatches.cs.
- Enrollment UI can accept a photo upload and persist bytes to the Students table.

## Approach

- Use QuestPDF for programmatic PDF layout to ensure consistent A4 output. A modular component-based generator was added (Reports/ReportCards/) and is wired into ReportCardsView export/print calls via ReportCardPdfGenerator.
- Ensure the data model carries both school logo bytes and student photo bytes to the report-page generator and to the XAML preview control. If the student photo is missing, render a silhouette placeholder image in both preview and PDF output.
- Keep the existing ReportCardSheetView XAML for on-screen preview and use QuestPDF for final PDF generation so the printed PDF can be tweaked independently for pixel-perfect results.

## Key Files (already present / modified)

- Views/Controls/ReportCardSheetView.xaml - on-screen A4 sheet control (existing). Use/extend to show student photo and school logo in preview.
- Views/ReportCardsView.xaml.cs - orchestrates preview, PDF export, printing and data load. It now calls QuestReportGenerator.GeneratePdf(...) for PDF generation.
- Services/QuestReportGenerator.cs - new QuestPDF-based generator that composes the A4 PDF pages.
- AutoTable.csproj - QuestPDF package reference added.
- Models/ReportCardModels.cs - ReportCardSheetModel now exposes StudentPhotoBytes and already contains LogoBytes.
- AutoTable/Models/Student.cs and AutoTable/Data/Entities/StudentEntity.cs - Student and StudentEntity now include PhotoBytes (raw image bytes) so photos can be stored.

## Tasks / Steps (atomic)

1. Add DB schema patch for student photo column (SchemaPatches.PatchStudents)
   - Add: AddColumnIfMissing(conn, "Students", "PhotoBytes", "ALTER TABLE Students ADD COLUMN PhotoBytes BLOB;");

2. Map DB column to entity & model
   - Ensure StudentEntity.PhotoBytes exists (already added).
   - Ensure DatabaseDataService.CreateStudentWithInitialDataAsync and UpdateStudentAsync persist PhotoBytes when provided.

3. Update enrollment UI to capture and save student photo
   - Views/SchoolSettingsView.xaml.cs contains a logo picker example. Replicate that pattern in EnrollmentFormView (or StudentsView dialog) to allow selecting an image file and setting the Student.PhotoBytes on submit.
   - Use FileOpenPicker to pick jpg/png and save bytes to the Student model before calling Create/Update service methods.

4. Populate ReportCardSheetModel.StudentPhotoBytes in GetReportCardSheetAsync
   - DatabaseDataService.GetReportCardSheetAsync currently sets LogoBytes from school settings. Add assignment: StudentPhotoBytes = student.PhotoBytes.

5. Render photo in the on-screen template (ReportCardSheetView)
   - Add an Image control in Views/Controls/ReportCardSheetView.xaml (right column header) bound to StudentPhotoBytes via a converter that returns an ImageSource or fallback silhouette if null.
   - Keep sizes/clip to a rounded rectangle (matching the template). Use BitmapImage SetSource from a MemoryStream in code-behind if necessary.

6. Render photo + logo in PDF (ReportCardHeader component)
   - ReportCardHeader renders LogoBytes in the header. StudentInformation renders StudentPhotoBytes at top-right in a framed rounded rectangle. If StudentPhotoBytes is null/empty, a silhouette placeholder is drawn.
   - Ensure image scaling and DPI are correct so the photo prints crisp on A4 at 300 DPI equivalent.

7. Visual polish and alignment
   - Tune fonts, sizes, colors and paddings in ReportCardTheme to visually match the template (school name big + ribbon, field labels grey, blue section headers, table borders and fixed column widths).
   - In ReportCardSheetView.xaml adjust column widths and font sizes to match printed PDF so WYSIWYG preview is accurate.

8. Test & validate
   - Build the app, enroll a test student with a photo and without a photo. Generate and preview the report card for both students. Export to PDF and open at 100% zoom to verify A4 output and margins.
   - Test printing to a physical A4 printer (or Microsoft Print to PDF) to confirm no scaling/fit-to-page issues.

## Risks & Open Questions

- Database migration: adding a BLOB column to legacy SQLite should be idempotent but back up production DB before applying. The SchemaPatches central patcher must be updated and tested.
- Image size & memory: storing many full-resolution photos in DB may grow DB size. Consider resizing/thumbnailing images on upload (e.g. max 600×800) before saving bytes.
- QuestPDF licensing/performance: QuestPDF is permissive for typical internal use, but if you need more advanced features consider iText (licensing concerns).
- Exact pixel-match: matching the provided design may require iterations with the designer (ribbon, rounded photo frame, signature images). Expect several tweaks to ReportCardTheme layout constants.

## Implementation Checklist for an external agent

1. Add schema patch for PhotoBytes (SchemaPatches.PatchStudents).
2. Wire StudentEntity.PhotoBytes <-> DB column mapping if using EF migrations (or ensure manual ALTER in SchemaPatches).
3. Update DatabaseDataService.CreateStudentWithInitialDataAsync and UpdateStudentAsync to map PhotoBytes from Student model to entity.
4. Update EnrollmentFormView / StudentsView to allow image selection (FileOpenPicker) and set Student.PhotoBytes prior to saving.
5. Set ReportCardSheetModel.StudentPhotoBytes = student.PhotoBytes in GetReportCardSheetAsync.
6. Update Views/Controls/ReportCardSheetView.xaml to show the photo and logo in the header (with silhouette fallback). Add small helper converter or code-behind to convert bytes->BitmapImage.
7. Update Services/QuestReportGenerator.cs to render student photo (or silhouette) top-right, and ensure LogoBytes are rendered correctly.
8. Run the app, test preview, export PDF, and print to PDF/printer. Fix any visual misalignments.

## Example small code snippets (reference)

- Assign student photo in GetReportCardSheetAsync:

	// inside DatabaseDataService.GetReportCardSheetAsync when building the model
	LogoBytes = schoolSettings.LogoBytes,
	StudentPhotoBytes = student.PhotoBytes,

- Convert bytes to BitmapImage in a XAML view code-behind:

	var bitmap = new BitmapImage();
	using var ms = new MemoryStream(model.StudentPhotoBytes);
	bitmap.SetSource(ms.AsRandomAccessStream());
	PhotoImage.Source = bitmap;

## Contact

If any of the database or UI assumptions are inaccurate (for example if photos are meant to be stored externally or the app uses different upload patterns), reply with details so this plan can be adjusted.

---
Generated for: AutoTable (workspace: C:\Users\manue\Desktop\Desktop_Apps\AutoTable)
