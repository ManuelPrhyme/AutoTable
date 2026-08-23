# AutoTable Modal Design Specification

## Reference
Based on the Walmart Old Checkout Modal design pattern (`Assets/modal-walmart-old-checkout.jpg`).

---

## Modal Dimensions

| Property | Value | Notes |
|----------|-------|-------|
| **Width** | 800 px | Fixed width, centered on screen |
| **Height** | 577 px | Fixed height, matches reference design |
| **Aspect Ratio** | ~1.39:1 | Landscape orientation |
| **Content Padding** | 24 px | Left, right, bottom |
| **Title Bar Height** | ~48 px | Standard WinUI ContentDialog |
| **Button Bar Height** | ~72 px | Standard WinUI ContentDialog buttons |
| **Usable Content Area** | 752 × 457 px | After dialog chrome |

---

## Layout Structure

```
┌─────────────────────────────────────────────────────────────┐
│  Title (Register New Teacher)                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────┐  24px  ┌─────────────────────┐    │
│  │  PERSONAL INFORMATION│        │  ASSIGNMENTS        │    │
│  │  ───────────────────│        │  ───────────────────│    │
│  │  Full Name           │        │  [Select Subjects]  │    │
│  │  Email               │        │  Subject1, Subject2 │    │
│  │  Phone               │        │                     │    │
│  │                      │        │  [Select Classes]   │    │
│  │  NEXT OF KIN         │        │  P5, P6             │    │
│  │  ───────────────────│        │                     │    │
│  │  Name                │        │  QUALIFICATIONS     │    │
│  │  Relationship        │        │  ───────────────────│    │
│  │  Phone               │        │  Previous Schools   │    │
│  │                      │        │                     │    │
│  │                      │        │  ☐ Registered       │    │
│  │                      │        │  ☐ Student Teacher  │    │
│  └─────────────────────┘        └─────────────────────┘    │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                              [Cancel]  [Register Teacher]   │
└─────────────────────────────────────────────────────────────┘
```

---

## Two-Column Grid Layout

| Property | Left Column | Gap | Right Column |
|----------|------------|-----|--------------|
| **Width** | 1fr (star) | 24 px | 1fr (star) |
| **Min Width** | 320 px | — | 320 px |
| **Max Width** | 376 px | — | 376 px |
| **Content Width** | 320 px | — | 320 px |
| **Alignment** | Left | — | Left |

---

## Field Specifications

### Text Inputs
- **Width**: 320 px (full column width)
- **Height**: Auto (standard WinUI TextBox)
- **Header**: 14 px, semibold
- **Placeholder**: 12 px, muted color

### Multi-Select Controls (Subjects / Classes)
- **Button Width**: 320 px (full column width)
- **Button Height**: 32 px
- **Flyout Width**: ~280 px
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
| ≥ 1024 px | Two-column layout as designed |
| 800–1023 px | Two-column, fields may wrap |
| < 800 px | WinUI ContentDialog auto-centers; content scrolls vertically |

---

## Implementation Notes

- Modal is created programmatically in `TeachersView.xaml.cs`
- No XAML template — all fields built in code-behind for dynamic data loading
- Multi-select controls load subjects/classes from `IDataService` at dialog open time
- Selected values stored as comma-separated strings in the `Teacher` model
- Edit dialog pre-selects checkboxes from existing comma-separated values via `ParseCommaSeparated()`

---

*Document created: August 23, 2026*
*Reference image: Assets/modal-walmart-old-checkout.jpg (800×577 px)*
