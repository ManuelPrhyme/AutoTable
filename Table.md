# Table Layout Specification — Reusable Template

> Generic layout + alignment spec for any **"card → column-header band → item list"** table
> in this app. Use it as a template when building a new table, and as the source of truth
> for keeping **column headers aligned with the data rows**.

---

## 0. How to use this spec

- Replace the **placeholders** (`{TABLE_NAME}`, `{COLUMN_COUNT}`, …) with the values for your table.
- §1–§6 are the **generic recipe** (apply to any table).
- §7 / §7b are **worked examples** — the All Assessments and Students tables — showing filled-in instances.
- §8 is a **fill-in checklist** for applying the recipe to a new table.

**The core guarantee:** the header-row grid and the data-row grid must share *identical
geometry* — same number of columns, same widths, same spacing, same insets. Everything in
this spec serves that single rule.

### Placeholder legend

| Placeholder | Meaning | Typical value |
|-------------|---------|---------------|
| `{TABLE_NAME}` | Card title / header text | `All Assessments` |
| `{CARD_STYLE}` | Border style wrapping the table | `TableCardStyle` |
| `{CARD_HEADER_STYLE}` | Border style for the card title row | `CardHeaderBorderStyle` |
| `{COLUMN_HEADER_STYLE}` | Border style for the column-header band | `TableHeaderRowStyle` |
| `{COLUMN_COUNT}` | Number of columns | `8` |
| `{ITEMS_SOURCE}` | Collection bound to the ListView | `Assessments` |
| `{CELL_STYLE}` | Default cell text style | `TableCellStyle` |
| `{NUMERIC_STYLE}` | Numeric cell text style | `NumericCellStyle` |
| `{HEADER_TEXT_STYLE}` | Column-header text style | `TableHeaderStyle` |
| `{COLUMN_SPACING}` | Grid `ColumnSpacing` (both grids) | `4` |
| `{HEADER_PADDING}` | Header border padding `left,top` | `16,10` |
| `{ROW_PADDING}` | Row grid padding `left,top` | `16,8` |
| `{ROW_MAX_HEIGHT}` | ListView scroll cap (px) | `480` |

---

## 1. Container & card structure

```
Page (Background = SurfaceGrayBrush)
└── ScrollViewer (VerticalScrollBarVisibility = Auto)
    └── StackPanel (Padding = 24, Spacing = 16)
        ├── Filter bar (FilterBarStyle)                     [optional]
        ├── KPI strip                                       [optional]
        ├── ── {TABLE_NAME} CARD ──
        │   Border ({CARD_STYLE})
        │   └── StackPanel
        │       ├── Card title ({CARD_HEADER_STYLE})  → "{TABLE_NAME}"
        │       ├── Column headers ({COLUMN_HEADER_STYLE})
        │       └── ListView (ItemsSource = {ITEMS_SOURCE})
        └── Status message
```

The table always lives inside a `{CARD_STYLE}` Border so its background, corner radius
and border come from a shared design token. The card content is a plain `StackPanel`:
a title band on top, a `{COLUMN_HEADER_STYLE}` band below it, then the `ListView`
of data rows.
---

## 2. The column plan ({COLUMN_COUNT} columns)

The header row and every data row use the **exact same {COLUMN_COUNT}-column grid**.
Define your columns in a plan table like this:

| Col | Header label | Cell content / style |
|-----|--------------|----------------------|
| 0 | `{HEADER_0}` | `{BIND_0}` — `{CELL_STYLE}` |
| 1 | `{HEADER_1}` | `{BIND_1}` — `{CELL_STYLE}` |
| … | … | … |
| N | `{HEADER_N}` | `{BIND_N}` — `{NUMERIC_STYLE}` or custom control |

Plan rules:
- Keep **one cell per column**; use `{CELL_STYLE}` for text, `{NUMERIC_STYLE}` for
  numeric data, and a centered custom control (e.g. a 36×36 `ProgressRing` + `%` label)
  for indicators.
- For columns with **no label** (icon/ring columns), still place an empty
  `<TextBlock Text="" />` in the header band so the header spans all {COLUMN_COUNT}
  columns and keeps identical geometry with the rows.
- **Proportional widths:** Use `n*` star units to give wider columns more space.
  Typical ratios: `1.2*` for short IDs, `2.8*` for names, `1.2*` for categories,
  `1.5*` for status, `2.1*` for actions. Keep the same proportions in both grids.

---

## 3. The alignment contract — header ↔ rows (MANDATORY)

Four mirrored rules applied to **both** the header row and the row template, in lockstep.
Violating any one breaks alignment.

### 3.1 Identical column definitions
Both grids declare the **same {COLUMN_COUNT} column definitions with the same widths**
(`*` for equal star units, or identical `n*` proportions).
- Header grid: the `<Grid>` inside the `{COLUMN_HEADER_STYLE}` Border.
- Row grid: the `<Grid>` inside the `ListView.ItemTemplate`.

Because both sides use the exact same width distribution, every column edge lines up
between header and rows at any window size.

### 3.2 Identical gutter
Both grids set the **same `ColumnSpacing="{COLUMN_SPACING}"`**, so the intra-column
gaps match 1:1.

### 3.3 Matching left/right insets
- Header: the `{COLUMN_HEADER_STYLE}` Border sets `Padding = {HEADER_PADDING}`.
- Rows: the row `<Grid>` sets `Padding = {ROW_PADDING}`.

The **left inset must be equal on both** so the first column starts at the same x in
the header and in every row (the vertical value may differ).

### 3.4 Full-width stretch
`ListView.ItemContainerStyle` sets **`HorizontalContentAlignment="Stretch"`**
(plus `Padding=0`, `Margin=0`, `MinHeight=0`) so each row's grid fills the full
card width and the star columns expand with it.

### 3.5 Horizontal alignment (headers must mirror the data)
**Rule:** set the **same `HorizontalAlignment` on the header label as the row content** in
that column. Because table content in this app is left-aligned, headers are almost always:

- **Headers:** `HorizontalAlignment="Left"` (or simply omit the attribute — the default
  `Stretch` already renders text flush-left). This puts each title **directly above the
  first character of its column content**, exactly like the Assessments table.
- **Data cells:** `HorizontalAlignment="Left"` on each data `TextBlock` / `StackPanel`.

**Why not center?** Centering a header over left-aligned content makes the title drift off
its column and no longer point at the content below it. Never center a header unless the
content of that column is itself centered.

**Exceptions (still mirror the content):**
- Column with a centered control (e.g. the 36×36 `ProgressRing` in Assessments): its header
  cell is **blank** — nothing to align.
- Numeric columns: may use `HorizontalAlignment="Right"`, but only if the header label is
  also right-aligned so they stay above their figures.
- Status/Actions columns that hold a `StackPanel`: keep both header label and the
  `StackPanel` at `Left`.

---

## 4. Even columns across the card

- Columns tile **edge-to-edge** across the full card width (inside the card padding),
  with no fixed widths.
- The star units re-balance whenever the window resizes, so the table always fills
  the available width.
- **Trade-off:** equal `*` columns give narrow-content columns the same visual width as
  wide ones. To go proportional, use identical `n*` values in **both** grids — the
  contract in §3 keeps working as long as header and rows share definitions.
---

## 5. Typography & vertical rhythm

Wire text styles from the shared design tokens (in `Resources\DesignTokens.xaml`):

| Element | Style token | Spec |
|---------|-------------|------|
| Column header text | `{HEADER_TEXT_STYLE}` | Segoe UI, 13px, `SemiBold`, `TextPrimaryBrush`, **`HorizontalAlignment=Left`** (mirrors the data) |
| Cell text | `{CELL_STYLE}` | Segoe UI, 14px, `Regular`, `TextPrimaryBrush`, `VerticalAlignment=Center`, **`HorizontalAlignment=Left`** |
| Numeric cell | `{NUMERIC_STYLE}` | Segoe UI, 14px, `SemiBold`, `VerticalAlignment=Center` |
| Special cell | *(your overrides)* | e.g. secondary text: 11px + `TextSecondaryBrush` |
| Row geometry | item container | `MinHeight=0`, cell padding `{ROW_PADDING}` → compact rows |

The column-header band is a `Border` using `{COLUMN_HEADER_STYLE}` — typically a shaded
`SurfaceGray2Brush` background with a `0,0,0,1` bottom border — a subtle band that
visually separates labels from data while sharing their geometry.

---

## 6. Scrolling

Cap the ListView at **`MaxHeight="{ROW_MAX_HEIGHT}"`** with
**`ScrollViewer.VerticalScrollBarVisibility="Auto"`**:
- Fewer rows → the list sizes to content (no scrollbar).
- More rows → the list caps at {ROW_MAX_HEIGHT}px and a vertical scrollbar appears,
  so the card never grows off-screen.

---

## 7. Worked example — the All Assessments table

Instantiation of the template for `Views\AssessmentsView.xaml`:

- `{TABLE_NAME}` = `All Assessments`, `{COLUMN_COUNT}` = `8`,
  `{ITEMS_SOURCE}` = `Assessments`, `{ROW_MAX_HEIGHT}` = `480`.

| Col | Header label | Cell content / style |
|-----|--------------|----------------------|
| 0 | `ASSESSMENT` | `Name` — `TableCellStyle` + `SemiBold`, ellipsis |
| 1 | `CLASS` | `ClassName` — `TableCellStyle` |
| 2 | `SUBJECT` | `Subject` — `TableCellStyle`, ellipsis |
| 3 | `WT%` | `WeightPercent` — `NumericCellStyle` |
| 4 | `DUE DATE` | `DueDate` — `TableCellStyle` + `DateFormatConverter` |
| 5 | *(blank)* | `ProgressRing` 36×36 + centered `%` label |
| 6 | `STATUS` | `StatusLabel` — `TableCellStyle` |
| 7 | `AUTHOR` | `AuthorName` — `TableCellStyle` overrides (11px, `TextSecondaryBrush`) |

Concrete wiring that instantiates the contract:
- Header grid: `<Grid ColumnSpacing="4">` inside `TableHeaderRowStyle` — 8× `*`.
- Row grid: the `<Grid Padding="16,8" ColumnSpacing="4">` in the item template — 8× `*`.
- `ListView.ItemContainerStyle`: `HorizontalContentAlignment=Stretch`, `Padding=0`,
  `Margin=0`, `MinHeight=0`.
- Card: `TableCardStyle`; title band `CardHeaderBorderStyle`; band `TableHeaderRowStyle`
  (`Padding=16,10`).
- **Alignment:** Headers are flush-left (no explicit alignment — default `Stretch`
  renders flush-left) and data cells are `HorizontalAlignment="Left"`, so each label sits
  directly above the first character of its column content. The only unlabeled column
  (5, the `ProgressRing`) has a blank header cell.

---

## 7b. Worked example — the Students table

Instantiation of the template for `Views\StudentsView.xaml`:

- `{TABLE_NAME}` = `Students`, `{COLUMN_COUNT}` = `6`,
  `{ITEMS_SOURCE}` = `FilteredStudents`, `{ROW_MAX_HEIGHT}` = `480`.

| Col | Header label | Cell content / style | Width |
|-----|--------------|----------------------|-------|
| 0 | `LIN` | `LIN` — `TableCellStyle` + `SemiBold`, ellipsis | `1.2*` |
| 1 | `FULL NAME` | `FullName` — `TableCellStyle`, ellipsis | `2.8*` |
| 2 | `CLASS` | `ClassName` — `TableCellStyle` | `1.2*` |
| 3 | `STREAM` | `StreamName` — `TableCellStyle`, ellipsis | `1.2*` |
| 4 | `STATUS` | Status dot + label `StackPanel` — `HorizontalAlignment=Left` | `1.5*` |
| 5 | `ACTIONS` | Buttons `StackPanel` (Shift, View, Edit, Terminate) | `2.1*` |

Concrete wiring:
- Header grid: `<Grid ColumnSpacing="4" Padding="0">` inside `TableHeaderRowStyle` — 6× proportional `n*`.
- Row grid: `<Grid ColumnSpacing="4" Padding="16,8">` in item template — same 6× proportional `n*`.
- `ListView.ItemContainerStyle`: `HorizontalContentAlignment=Stretch`, `Padding=0`,
  `Margin=0`, `MinHeight=0`.
- Card: `TableCardStyle`; title band `CardHeaderBorderStyle`; band `TableHeaderRowStyle`
  (`Padding=16,10`).
- **Alignment:** All header `TextBlock` elements use `HorizontalAlignment="Left"`
  (updated 30 Aug 2026 — previously `Center`), matching the left-aligned data cells
  (including the Status `StackPanel` and Actions `StackPanel`) so each title sits
  directly above its column content.

---

## 8. Fill-in checklist for a new table

- [ ] Pick `{COLUMN_COUNT}` and write the §2 column plan (headers + cell styles).
- [ ] Header grid: add `{COLUMN_COUNT}` column definitions with the chosen widths.
- [ ] Row-template grid: add the **same** definitions with the **same** widths.
- [ ] Set `ColumnSpacing` **identical** (`{COLUMN_SPACING}`) on both grids.
- [ ] Header Border left-padding equals row Grid left-padding (both `{HEADER_PADDING}`/`{ROW_PADDING}` left = 16).
- [ ] Set `HorizontalContentAlignment="Stretch"` on the `ListViewItem` style.
- [ ] Leave a **blank header cell** for every icon/ring column.
- [ ] Cap the `ListView` (`MaxHeight`) + `VerticalScrollBarVisibility="Auto"`.
- [ ] Reuse `TableHeaderStyle` / `TableCellStyle` / `NumericCellStyle` tokens for typography.
- [ ] **Headers:** Set `HorizontalAlignment="Left"` (mirroring the content alignment) on each header `TextBlock`.
- [ ] **Data cells:** Set `HorizontalAlignment="Left"` on each data `TextBlock` or `StackPanel` (right-align numerics only if their headers are also right-aligned).
- [ ] Verify at 3 window widths (narrow / typical / wide) that header edges still align with row edges.