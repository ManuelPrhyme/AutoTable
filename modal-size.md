# AutoTable Modal Design Specification

## Reference
Based on the Walmart Old Checkout Modal design pattern (`Assets/modal-walmart-old-checkout.jpg`).

---

## Modal Dimensions

| Property | Value | Notes |
|----------|-------|-------|
| **Width** | 1040 px | Fixed width, centered on screen (expanded +30% from the original 800 px to fit both columns comfortably) |
| **Height** | 577 px | Fixed height, matches reference design |
| **Aspect Ratio** | ~1.80:1 | Landscape orientation |
| **Content Padding** | 24 px | Left, right, bottom |
| **Title Bar Height** | ~48 px | Standard WinUI ContentDialog |
| **Button Bar Height** | ~72 px | Standard WinUI ContentDialog buttons |
| **Usable Content Area** | 992 × 457 px | After dialog chrome |

---

## Layout Structure

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Title (Register New Teacher)                                             │
├───────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌───────────────────────────────┐  24px  ┌───────────────────────────┐   │
│  │  PERSONAL INFORMATION         │        │  ASSIGNMENTS              │   │
│  │  ────────────────────         │        │  ────────────────────     │   │
│  │  Full Name                    │        │  [Select Subjects]        │   │
│  │  Email                        │        │  Subject1, Subject2       │   │
│  │  Phone                        │        │                           │   │
│  │                               │        │  [Select Classes]         │   │
│  │  NEXT OF KIN                  │        │  P5, P6                   │   │
│  │  ────────────────────         │        │                           │   │
│  │  Name                         │        │  QUALIFICATIONS           │   │
│  │  Relationship                 │        │  ────────────────────     │   │
│  │  Phone                        │        │  Previous Schools         │   │
│  │                               │        │                           │   │
│  │                               │        │  ☐ Registered             │   │
│  │                               │        │  ☐ Student Teacher        │   │
│  └───────────────────────────────┘        └───────────────────────────┘   │
│                                                                           │
├───────────────────────────────────────────────────────────────────────────┤
│                                        [Cancel]  [Register Teacher]       │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## Two-Column Grid Layout

| Property | Left Column | Gap | Right Column |
|----------|------------|-----|--------------|
| **Width** | 1fr (star) | 24 px | 1fr (star) |
| **Min Width** | 408 px | — | 408 px |
| **Max Width** | 484 px | — | 484 px |
| **Content Width** | 408 px | — | 408 px |
| **Alignment** | Left | — | Left |

---

## Field Specifications

### Text Inputs
- **Width**: 408 px (full column width, reduced 15% from 480 px; dialog width unchanged)
- **Height**: Auto (standard WinUI TextBox)
- **Header**: 14 px, semibold
- **Placeholder**: 12 px, muted color

### Multi-Select Controls (Subjects / Classes)
- **Button Width**: 408 px (full column width)
- **Button Height**: 32 px
- **Flyout Width**: ~320 px
- **Flyout Max Height**: 180 px
- **Checkbox Size**: 16 × 16 px
- **Item Spacing**: 4 px
- **Summary Text**: 11 px, secondary color, wrapping enabled

### Toggle Switches
- **Width**: Auto (content-based)
- **Height**: 28 px (standard WinUI)
- **Spacing between toggles**: 8 px

### Section Headers
- **Font Size**: 11 px
- **Weight**: SemiBold
- **Color**: TextSecondaryBrush
- **Bottom Margin**: 4 px

---

## Section Grouping

### Left Column
1. **PERSONAL INFORMATION** — Full Name, Email, Phone
2. **NEXT OF KIN** — Name, Relationship, Phone

### Right Column
1. **ASSIGNMENTS** — Subjects (multi-select), Classes (multi-select)
2. **QUALIFICATIONS** — Previous Schools, Registered Teacher toggle, Student Teacher toggle

---

## Button Bar

| Button | Style | Position |
|--------|-------|----------|
| **Cancel** | SecondaryButtonStyle | Left of primary |
| **Register Teacher** | PrimaryButtonStyle | Right, default focused |

---

## Color Reference (from DesignTokens.xaml)

| Token | Hex | Usage |
|-------|-----|-------|
| SurfaceGrayBrush | — | Dialog background |
| TextSecondaryBrush | — | Section headers, summary text |
| TextMutedBrush | — | Placeholder text |
| PrimaryBlueBrush | — | Primary button, accents |
| WarningAmberBrush | — | Warning states |
| SuccessGreenBrush | — | Success states |

---

## Responsive Behavior

| Screen Width | Behavior |
|-------------|----------|
| ≥ 1104 px | Two-column layout as designed (full 1040 px dialog) |
| 800–1103 px | WinUI ContentDialog auto-centers; content may wrap |
| < 800 px | Content scrolls vertically |

---

## Record Payment Modal (Fee Collection)

Single-column dialog — deliberately narrower than the teacher modals, so it fits
inside WinUI's default `ContentDialogMaxWidth` (~548 px) and needs **no** width override.

| Property | Value | Notes |
|----------|-------|-------|
| **Content Width** | 440 px | Single `StackPanel`, Spacing 12 |
| **Layout** | Single column | Student → Amount → hint |
| **Created in** | `FeeCollectionView.xaml.cs` → `RecordPayment_Click` | Code-behind; button uses `Click` |

### Fields

| Field | Control | Behavior |
|-------|---------|----------|
| **Student** | TextBox + **inline results panel** | Live filter while typing: an inline panel directly beneath the field (NOT a light-dismiss Flyout — the TextBox must never lose focus) lists up to 8 active students matching name or LIN. Each result row shows **Name on the left**, **Class - Stream over LIN on the right** (plain Border rows, no selector chrome). The panel is shown/collapsed via Visibility as results appear/disappear; clicking a row selects the student, echoes the name into the box, and collapses the panel (re-population suppressed via flag). |
| **Amount** | TextBox | Header pre-fills the expected amount from `TermFees` for the picked student's class + ACTIVE term when configured (e.g. *"Amount (expected: 250,000)"*). |
| **Hint** | TextBlock, 12 px, 70% opacity | Shows `Class: {student's class} \| Term: {active term}` plus expected amount when known. Picking a student overrides the page's class filter with the student's own class; term always defaults to the active term (`GetActiveTermAsync`), ignoring the filter bar's term. |

### Validation & Submit

- Primary **Record** button starts disabled.
- Enabled only when **a student has been explicitly picked from the popup** AND the amount parses to > 0.
- On submit: `IDataService.CreateFeePaymentAsync(studentId, amount, termId, null, "Recorded via UI")`,
  then `FeeCollectionViewModel.RefreshCommand` re-loads records/KPIs; status message confirms the recorded amount.
- Errors surface through `ViewModel.StatusMessage`.

> Note: an older LIN-based stub of this dialog lived in `FeeCollectionViewModel.RecordPaymentAsync`.
> It was removed — payment recording is owned by the code-behind modal.

---

## Enroll New Student Modal (EnrollmentFormView)

XAML-based two-column dialog hosted in a ContentDialog from `StudentsView.AddStudent_Click`.

| Property | Value | Notes |
|----------|-------|-------|
| **Dialog Width** | 1040 px | With mandatory `ContentDialogMaxWidth = 1088` override |
| **Content Size** | 992 × 457 px | Fixed on the UserControl root grid |
| **Layout** | Two columns, 24 px gutter | Each column is its own ScrollViewer (fields exceed 457 px) |
| **Action Bar** | Bottom row, spans both columns | Status message left, **Submit Enrollment** primary button right |

### Column grouping

| Left Column | Right Column |
|-------------|--------------|
| **STUDENT INFORMATION** — Full name; Class + Stream (paired row); LIN; Date of birth + Gender (paired row); Nationality + Religion (paired row); Previous school | **RESIDENCY VERIFICATION** — Proof of residence; District + Zone/Village (paired row) |
| **PARENT / GUARDIAN DETAILS** — Guardian name; Relationship + Contact number (paired row); Email; Residential address; Custody-documents checkbox | **HEALTH RECORDS** — Immunization + Medical-exam checkboxes (one row); Allergies; Health insurance |
| | **EMERGENCY CONTACTS** — Contact name; Relationship + Phone (paired row); Authorized pickup person(s) |

### Implementation notes

- All bindings are unchanged from the original single-column form — only the layout moved.
- Section headers: 11 px, SemiBold, `TextSecondaryBrush` (per Field Specifications).
- Paired fields use `ColumnSpacing="8"` two-star grids; single fields stretch to full column width.
- Submit flow: `EnrollmentViewModel.SubmitCommand` → `OnSubmittedAsync` callback → inserts created student at index 0 in `StudentsView` → `dialog.Hide()`.

---

## Implementation Notes

- Modal is created programmatically in `TeachersView.xaml.cs`
- No XAML template — all fields built in code-behind for dynamic data loading
- Multi-select controls load subjects/classes from `IDataService` at dialog open time
- Selected values stored as comma-separated strings in the `Teacher` model
- Edit dialog pre-selects checkboxes from existing comma-separated values via `ParseCommaSeparated()`

### ⚠️ REQUIRED — ContentDialogMaxWidth override

WinUI's `ContentDialog` clamps its own width through the **`ContentDialogMaxWidth`**
theme resource (**~548 px default**). Without an override, any dialog wider than that
is silently clipped — wide modals appear to "lose" their right-hand column even though
it exists in code (this exact bug shipped once; see CONTEXT_REPORT iteration
*2026-08-24-dialog-maxwidth-clipping-fix*).

Every wide modal MUST set, before `ShowAsync()`:

```csharp
dialog.Resources["ContentDialogMaxWidth"] = ModalWidth + 48d; // 1040 + padding allowance
```

Applied in both teacher dialogs (`Register New Teacher`, `Edit Teacher`). If more wide
modals are added, extract this into a shared helper (e.g. `CreateWideDialog`).

---

*Document created: August 23, 2026*
*Reference image: Assets/modal-walmart-old-checkout.jpg (800×577 px)*
*Updated 23 Aug 2026: width expanded +30% (800 → 1040 px) to comfortably fit both columns; field width raised 320 → 480 px.*
*Updated 24 Aug 2026: documented mandatory ContentDialogMaxWidth override — WinUI's ~548px default clamp was clipping the second column.*
*Updated 24 Aug 2026: both modal columns narrowed 15% (field width 480 → 408 px); dialog stays 1040×577 with everything else unchanged.*
*Updated 24 Aug 2026: documented the Record Payment modal (Fee Collection) — single-column 440 px, searchable student Flyout; old ViewModel stub removed.*
*Updated 24 Aug 2026: Enroll New Student modal restructured to the two-column 1040×577 standard (was a 600×720 single-column scroll); ContentDialogMaxWidth override applied.*
