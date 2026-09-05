# Report Card Template Recreation — Detailed QuestPDF Implementation Specification

## 0. Objective

Recreate the supplied reference report card as a **single-page, print-ready A4 portrait PDF** using **C# + QuestPDF**.

This document is intentionally more precise than a normal visual description. It defines:

- physical page geometry;
- approximate element coordinates and relative dimensions;
- section heights;
- horizontal/vertical relationships;
- colours;
- fonts and typography hierarchy;
- border thicknesses;
- table column ratios;
- dynamic/replacable content;
- image handling;
- QuestPDF-oriented implementation patterns;
- component boundaries;
- overflow rules;
- integration points with school settings;
- rendering/QA requirements.

The final implementation must be a **reusable report-card template**, not a hard-coded copy of the sample school.

The sample school shown in the reference is only test data. Production values must come from the application's school settings, student record, assessment data and configuration.

---

# 1. Reference Geometry

The supplied reference image is approximately **1054 × 1492 pixels** and represents an A4 portrait document.

Use A4 as the authoritative physical target:

- Width: **210 mm**
- Height: **297 mm**
- Portrait orientation.

QuestPDF supports A4 directly with `PageSizes.A4`. It also supports explicit units such as millimetres, centimetres and points, so use physical units for major dimensions where possible.

Recommended:

```csharp
page.Size(PageSizes.A4);
```

QuestPDF's documentation confirms that A4 is a supported standard page size and that page margins and page size can be explicitly configured.  
Reference: https://www.questpdf.com/api-reference/page/settings.html

---

# 2. Coordinate System for Implementation

For design discussion, use this coordinate system:

- `(0,0)` = physical top-left of the A4 page.
- `X` increases toward the right.
- `Y` increases downward.
- Page = **210 mm × 297 mm**.

The reference is intentionally inset from the physical page edge.

## Recommended safe content frame

Approximate:

```text
Page:
210 × 297 mm

Outer frame:
X ≈ 2 mm
Y ≈ 2 mm
Width ≈ 206 mm
Height ≈ 293 mm
```

This produces approximately:

- left border inset: 2 mm
- right border inset: 2 mm
- top border inset: 2 mm
- bottom border inset: 2 mm

Do not use a conventional 20 mm page margin. That would make the resulting report much smaller than the reference.

Instead, create the A4 page normally and construct an **inner framed container**.

Suggested architecture:

```csharp
page
    .PageColor(Colors.White)
    .Padding(2, Unit.Millimetre)
    .Border(0.6f)
    .BorderColor(theme.PrimaryNavy)
    .Column(...)
```

If the deployed QuestPDF version exposes slightly different signatures, adapt syntax while preserving the geometry.

---

# 3. High-Level Vertical Map

Use the following as a design grid.

| Section | Approx. Y start | Approx. height |
|---|---:|---:|
| Outer frame | 2 mm | 293 mm |
| Header | 5–48 mm | ~43 mm |
| Student information | 49–94 mm | ~45 mm |
| Results title + table | 96–163 mm | ~67 mm |
| Summary strip | 165–183 mm | ~18 mm |
| Comments + grading | 187–237 mm | ~50 mm |
| Principal remark | 241–266 mm | ~25 mm |
| Signatures | 267–289 mm | ~22 mm |
| Footer | 278–295 mm | ~17 mm |

These coordinates are **visual reference targets**, not instructions to use absolute positioning.

Prefer normal QuestPDF flow layout.

The important thing is to preserve the **relative vertical proportions**.

### Relative page allocation

A useful design model is:

```text
Header                         15%
Student information            15%
Results table                  25%
Summary                         6%
Comments + grading             18%
Principal remark                7%
Signatures                      8%
Footer                          6%
```

Small variations are acceptable if the complete normal report remains on one A4 page.

---

# 4. Horizontal Grid

Inside the outer frame, use a common left/right content grid.

Approximate:

```text
Inner frame width: ~206 mm

Main content:
Left padding ≈ 4 mm
Right padding ≈ 4 mm

Usable content width ≈ 198 mm
```

All major sections should share the same left and right edges:

- student information;
- results table;
- summary strip;
- comments/grading panels;
- principal remark;
- signatures.

The footer may extend almost completely to the inner frame edges.

This common grid is important because the reference has a very structured, professionally aligned appearance.

---

# 5. Theme / Design Tokens

Create one theme object instead of scattering visual constants throughout the code.

Example:

```csharp
public sealed record ReportCardTheme(
    string PrimaryNavy,
    string SecondaryNavy,
    string AccentGold,
    string LightBlue,
    string GridBlue,
    string Text,
    string MutedText,
    string White,
    string DisplayFont,
    string BodyFont
);
```

Recommended default values sampled/approximated from the reference:

```text
PrimaryNavy    #00184D
SecondaryNavy  #0A2860
AccentGold    #E2A01B
LightBlue     #ECF3FE
GridBlue      #9FB4D2
Text          #17213A
MutedText     #53627A
White         #FFFFFF
```

The darkest blue in the supplied image is approximately RGB `(0, 24, 77)`, while the gold accent is approximately around RGB `(226, 159, 27)`. Treat these as visual approximations rather than exact brand values.

The actual school should be able to override:

- primary colour;
- secondary colour;
- accent colour;
- light table colour;
- border colour;
- text colour.

QuestPDF accepts hexadecimal colour values directly, making a theme object particularly convenient.

Reference: https://www.questpdf.com/concepts/colors.html

---

# 6. Typography System

The report has three major typographic roles.

## 6.1 Display / school identity

Reference appearance:

- formal serif;
- uppercase;
- dark navy;
- strong weight.

Recommended:

```text
Georgia
Times New Roman
or another configured serif font
```

Suggested sizes:

```text
School name:             23–27 pt
School second line:      13–17 pt
Motto:                    8–10 pt italic
```

The school name should be the largest text on the page.

## 6.2 Body / interface typography

Use a clean sans-serif font.

Recommended:

```text
Lato
Arial
Aptos
Helvetica-like equivalent
```

QuestPDF uses Lato as its default font, which makes Lato a practical deployment-safe baseline. If custom fonts are required, explicitly register/package them rather than relying on fonts installed on the production machine.

Reference:
https://www.questpdf.com/api-reference/text/font-management.html

## 6.3 Suggested hierarchy

```text
School name                 23–27 pt, bold serif
School subtitle             13–17 pt, bold serif
REPORT CARD                 15–18 pt, bold sans-serif
Section headers              9–11 pt, bold sans-serif
Student labels               8–9 pt, bold
Student values               8–9 pt, regular
Table headers                7–8 pt, bold
Table values                 8–9 pt
Summary labels               7–8 pt, bold
Summary values               9–11 pt, bold
Comments                     8.5–9.5 pt
Principal remark              8–9 pt
Signature headings            7–8 pt, bold
Footer                        7–8 pt
```

Do not force all text into one font size.

The reference's visual quality comes from a strong typography hierarchy.

QuestPDF supports font family, font size, font weight, font colour, fallback fonts and inherited styles.

Reference:
https://www.questpdf.com/api-reference/text/text-style.html

---

# 7. Font Packaging / Deployment

For reliable PDF output in Docker, Linux, Azure, containers or other server environments:

- do not assume Microsoft fonts exist;
- package required fonts with the application;
- configure QuestPDF font management appropriately;
- provide fallback fonts for multilingual school names/student names.

For example:

```text
Resources/
    Fonts/
        Lato-Regular.ttf
        Lato-Bold.ttf
        Lato-Italic.ttf
        Georgia-compatible.ttf
```

If a school name can contain non-Latin scripts, provide appropriate fallback fonts.

QuestPDF supports font fallback; this is preferable to allowing missing glyphs to appear as boxes.

Reference:
https://www.questpdf.com/api-reference/text/font-management.html

---

# 8. Header — Exact Visual Composition

The header occupies roughly the first **43–45 mm** inside the frame.

Use a three-zone row:

```text
| Logo zone | School identity zone | Academic-year ribbon |
```

Suggested width ratio:

```text
Logo       26%
Identity   59%
Ribbon     15%
```

With ~198 mm usable width:

```text
Logo       ≈ 51 mm
Identity   ≈ 117 mm
Ribbon     ≈ 30 mm
```

These values are approximate; the logo should not consume unnecessary space.

---

# 9. Header — Logo

## Position

Approximate:

```text
X: 8–10 mm
Y: 8–10 mm
Width: 38–43 mm
Height: 35–40 mm
```

The logo should be centred vertically in the left header zone.

Use:

```csharp
container
    .Width(42, Unit.Millimetre)
    .Height(40, Unit.Millimetre)
    .AlignCenter()
    .AlignMiddle()
    .Image(...)
    .FitArea();
```

QuestPDF's image API preserves aspect ratio when constrained appropriately.

Reference:
https://www.questpdf.com/api-reference/image/basics.html

### Replaceable content

Replace:

```text
Bright Future crest
```

with:

```text
SchoolSettings.Logo
```

Preferred formats:

1. SVG
2. PNG with transparency
3. JPEG only if necessary

SVG is preferred for logos because it scales cleanly.

QuestPDF supports SVG images directly and can preload them when reused.

Reference:
https://www.questpdf.com/api-reference/image/svg.html

---

# 10. Header — School Identity

Centre this block horizontally.

Approximate vertical sequence:

```text
School name
      ↓ 1–2 mm
School subtitle
      ↓ 1–2 mm
Motto
      ↓ 4–6 mm
REPORT CARD ornament row
```

## Replaceable content

The following must come from school settings:

```text
SchoolSettings.Name
SchoolSettings.Type / Subtitle
SchoolSettings.Motto
SchoolSettings.Logo
```

Do not hard-code:

```text
BRIGHT FUTURE
SECONDARY SCHOOL
Knowledge. Discipline. Excellence.
```

## School name behaviour

If the school name is short:

- use the target display size.

If the school name is long:

- allow wrapping;
- reduce font size within a configured minimum;
- never allow it to collide with the ribbon.

A practical approach is to constrain the identity zone and use a two-line school identity rather than absolute positioning.

---

# 11. Report Card Ornament

The reference shows:

```text
──── ◆ REPORT CARD ◆ ────
```

where the horizontal rules are gold.

Recommended:

- line thickness: ~1 pt;
- gold colour;
- diamond: approximately 2.5–3 mm;
- report title: 15–18 pt bold.

Use a QuestPDF `Row`:

```text
RelativeItem()
Small fixed item: diamond
RelativeItem()
Title
RelativeItem()
Small fixed item: diamond
RelativeItem()
```

This is preferable to manually positioning lines.

---

# 12. Academic-Year Ribbon

Approximate dimensions:

```text
Width: 28–30 mm
Height: 33–36 mm
```

Position:

```text
Top: ~4 mm inside frame
Right: ~4 mm inside frame
```

Colour:

```text
PrimaryNavy
```

Text:

```text
ACADEMIC
YEAR
{AcademicYear}
```

Suggested:

- 8–9 pt;
- white;
- bold;
- centred;
- line spacing compact.

The lower edge is shaped like a ribbon notch.

## QuestPDF recommendation

Do not make the entire document dependent on absolute positioning.

Possible implementations:

### Option A — SVG

Create one small dynamic SVG ribbon background and overlay the academic year text.

### Option B — Canvas

Use QuestPDF's graphics/canvas facilities for the notch.

### Option C — Approximation

Use a navy rectangle and a small white triangle/notch.

SVG is usually the easiest to keep visually precise and scalable.

---

# 13. Student Information Section

Approximate height:

**42–45 mm**

Horizontal composition:

```text
Left details:  ~72%
Photo:         ~18%
Spacing:       ~10%
```

A better implementation is:

```text
| left information area | photo |
```

with the left information area internally split into two columns.

## Left information structure

```text
| Field | Value | Field | Value |
```

Five rows.

Suggested field-column ratios:

```text
Label      18%
Colon       3%
Value      28%
Gap         4%
Label      18%
Colon       3%
Value      26%
```

The photo occupies a separate right-hand column.

---

# 14. Student Information — Field Styling

Each field:

```text
LABEL : VALUE
───────────────
```

Use:

- label: bold uppercase;
- colon: regular/bold;
- value: regular;
- value underline: light blue-gray.

Approximate font:

```text
8.5–9 pt
```

Approximate row height:

```text
7–8 mm
```

The value underline should be approximately:

```text
0.5–0.7 pt
#9FB4D2
```

Avoid drawing the underline manually if a bottom border on the value container is sufficient.

---

# 15. Student Information — Dynamic Fields

Replace sample values with:

```text
Student.Name
Student.AdmissionNumber
Student.DateOfBirth
Student.Gender
Student.Class
Student.Term
Student.AcademicYear
Student.House
Student.ReportDate
```

Possible settings/data mappings:

```csharp
report.StudentName
report.AdmissionNumber
report.DateOfBirth
report.Gender
report.ClassName
report.TermName
report.AcademicYear
report.House
report.ReportDate
```

Keep formatting outside the raw model where practical.

Example:

```csharp
FormatDate(report.DateOfBirth, schoolSettings.DateFormat)
```

This allows different schools to choose:

```text
dd MMMM yyyy
dd/MM/yyyy
MM/dd/yyyy
```

without modifying the PDF layout.

---

# 16. Student Photo

Approximate:

```text
Outer frame: 34–36 mm wide
Outer frame: 45–49 mm high
```

The reference uses a pale-blue/grey border around the portrait.

Recommended:

```text
Outer border: 0.7–1 pt
Border colour: GridBlue
Padding: 2 mm
Inner image: FitArea()
```

Use `FitArea()` or equivalent aspect-preserving behaviour.

Do not use `FitUnproportionally()`.

QuestPDF documents that images can be constrained by width/height and that the normal image scaling preserves aspect ratio.

Reference:
https://www.questpdf.com/api-reference/image/basics.html

---

# 17. Examination Results Section

Approximate start:

```text
Y ≈ 96 mm
```

Approximate total height:

```text
65–68 mm
```

## Section title bar

Height:

```text
7–8 mm
```

Background:

```text
PrimaryNavy
```

Text:

```text
PROMOTIONAL EXAMINATION RESULTS
```

Style:

```text
White
Bold
9–10 pt
Centered
```

Use a rounded top border only if visually appropriate.

---

# 18. Results Table — Column Geometry

Use a QuestPDF `Table`.

This is strongly preferred over manually composing rows because the table is dynamic and the column alignment is critical.

QuestPDF provides dedicated table column definitions, cell styling, headers and dynamic row placement.

Reference:
https://www.questpdf.com/api-reference/table/basics.html

Recommended relative widths:

```text
Subject        1.75
Max Marks      1.00
CAT 1          1.00
CAT 2          1.00
Exam           1.00
Total          1.00
Average        1.00
Grade          0.90
```

Equivalent approximate percentages:

```text
Subject        22%
Max Marks      12.5%
CAT 1          12.5%
CAT 2          12.5%
Exam           12.5%
Total          12.5%
Average        12.5%
Grade          10.5%
```

If the subject column appears too wide in the actual render, reduce it to approximately 20–21%.

---

# 19. Results Table — Header

Header height:

```text
12–14 mm
```

Background:

```text
LightBlue
#ECF3FE
```

Border:

```text
GridBlue
~0.5–0.7 pt
```

Text:

```text
7–8 pt
Bold
Dark navy
Centered
```

Use deliberate line breaks:

```text
MAX
MARKS
```

```text
CAT 1
(%)
```

```text
CAT 2
(%)
```

```text
EXAM
(%)
```

```text
TOTAL
(%)
```

```text
AVERAGE
(%)
```

---

# 20. Results Table — Body

Reference row height:

```text
~9–10 mm
```

Body font:

```text
8.5–9 pt
```

Subject:

```text
left aligned
```

Numbers:

```text
centred
```

Grade:

```text
centred
bold if desired
```

Borders:

```text
0.5–0.7 pt
GridBlue
```

Avoid heavy black table lines.

The reference uses a subtle blue grid.

---

# 21. Dynamic Assessment Data

Do not hard-code the sample marks.

Use:

```csharp
IReadOnlyList<SubjectResult>
```

where each result contains:

```text
SubjectName
MaximumMarks
Cat1
Cat2
Exam
Total
Average
Grade
```

Render rows with:

```csharp
foreach (var result in report.Subjects)
{
    table.Cell()...
}
```

The PDF layer should preferably not calculate academic results.

Calculation should happen before rendering in the domain/service layer.

The PDF component receives final display values.

---

# 22. Summary Strip

Approximate:

```text
Height: 15–18 mm
Gap from table: 2–3 mm
```

Five equal columns:

```text
| Total | Average | Grade | Position | Status |
```

Each cell:

- light blue background;
- thin grid border;
- centred text;
- label above value.

Suggested label size:

```text
7–7.5 pt
```

Suggested value size:

```text
9–10.5 pt
Bold
```

---

# 23. Summary Strip — Dynamic Content

Replace:

```text
1,192 / 1,500
79.5%
B+
7 / 32
PROMOTED
```

with:

```csharp
report.TotalMarksDisplay
report.OverallAverage
report.OverallGrade
report.ClassPosition
report.ClassSize
report.Status
```

Recommended display generation:

```text
{Position} / {ClassSize}
```

Do not hard-code the slash format if the school settings support alternative formatting.

---

# 24. Teacher Comments + Grading Key

Approximate vertical position:

```text
Y ≈ 187 mm
```

Approximate height:

```text
47–50 mm
```

Horizontal ratio:

```text
Teacher comments   49%
Gap                 2%
Grading key        49%
```

Use a QuestPDF `Row`.

Each panel should be a reusable component.

---

# 25. Panel Header Styling

Both panels have a dark navy title bar.

Approximate height:

```text
7–8 mm
```

Text:

```text
9–10 pt
Bold
White
```

Teacher panel:

```text
TEACHER'S COMMENTS
```

Grading panel:

```text
GRADING KEY
```

Optional icons may appear at the far right.

Do not make icons a hard dependency.

If the application has an icon asset library, use SVG assets.

QuestPDF supports SVG rendering and therefore is suitable for small scalable UI icons.

Reference:
https://www.questpdf.com/api-reference/image/svg.html

---

# 26. Teacher Comments Body

Body padding:

```text
4–5 mm
```

Text:

```text
8.5–9.5 pt
```

Line spacing:

```text
compact / normal
```

Text should naturally wrap.

Do not use a fixed number of lines.

Dynamic source:

```text
report.TeacherComments
```

If the school supports multiple teacher comments, concatenate them at the application/domain layer or render them as separate paragraphs.

---

# 27. Grading Key

Use a dedicated QuestPDF table.

Three columns:

```text
GRADE       20%
RANGE       35%
REMARK      45%
```

Header:

```text
LightBlue
```

Body:

```text
White
```

Borders:

```text
GridBlue
```

Font:

```text
7.5–8.5 pt
```

Grading rules must come from configuration where possible:

```csharp
SchoolSettings.GradingScale
```

Example model:

```csharp
public record GradeBand(
    string Grade,
    string RangeDisplay,
    string Remark
);
```

This allows one school to use:

```text
A / B+ / B / C / D / E
```

while another uses:

```text
A / B / C / D / E / F
```

without changing the PDF component.

---

# 28. Principal's Remark

Approximate height:

```text
22–25 mm
```

Full width.

Header:

```text
Height: 6–7 mm
Background: LightBlue
Text: PrimaryNavy
Font: 8.5–9 pt bold
```

Body:

```text
White
Padding: 3–4 mm
Font: 8–9 pt
```

Dynamic source:

```text
report.PrincipalRemark
```

---

# 29. Signatures

Three equal-width zones.

```text
| Principal | Class Teacher | Date |
```

Each ≈ 33.3%.

Vertical separators:

```text
0.5 pt
GridBlue
```

Suggested vertical allocation:

```text
Heading          4 mm
Signature image  7–9 mm
Signature line   3 mm
Name             4 mm
Role             3 mm
```

Signature assets:

```text
SchoolSettings.PrincipalSignature
report.ClassTeacherSignature
```

Names:

```text
SchoolSettings.PrincipalName
report.ClassTeacherName
```

Titles should be configurable rather than hard-coded.

---

# 30. Footer

Approximate:

```text
Height: 15–17 mm
```

Background:

```text
PrimaryNavy
```

Text:

```text
White
7–8 pt
```

Four contact zones:

```text
Address | Telephone | Email | Website
```

Suggested width:

```text
Address      30%
Telephone    21%
Email        25%
Website      24%
```

Use compact icons if available.

Dynamic sources:

```text
SchoolSettings.PostalAddress
SchoolSettings.Phone
SchoolSettings.Email
SchoolSettings.Website
```

Never hard-code the sample:

```text
P.O. Box 12345
Kampala, Uganda
+256 701 234 567
info@brightfuture.sc.ug
www.brightfuture.sc.ug
```

---

# 31. Footer Decorative Gold Swoosh

The reference has a curved gold accent entering the lower-right corner.

Recommended:

- use a small SVG asset;
- position it in a background/foreground layer;
- clip it to the page/frame;
- keep it behind footer text.

QuestPDF supports page background and foreground slots. These are useful when a decorative element should span or overlay a page without affecting normal content flow.

Reference:
https://www.questpdf.com/api-reference/page/slots.html

Do not use absolute positioning for the main report simply to reproduce this decoration.

---

# 32. Outer Border

Reference:

```text
Very thin navy line
Rounded corners
```

Suggested:

```text
0.7–1.0 pt
PrimaryNavy
```

Approximate inset:

```text
2 mm
```

This is one of the few elements where a thin frame container is preferable to a page margin.

---

# 33. Recommended QuestPDF Component Tree

Use reusable components.

```text
ReportCardDocument
│
└── Page
    │
    └── ReportCardFrame
        │
        ├── ReportCardHeader
        │   ├── SchoolLogo
        │   ├── SchoolIdentity
        │   ├── ReportTitle
        │   └── AcademicYearRibbon
        │
        ├── StudentInformation
        │   ├── StudentFieldGrid
        │   └── StudentPhoto
        │
        ├── ExaminationResults
        │   ├── SectionHeader
        │   └── ResultsTable
        │
        ├── SummaryStrip
        │
        ├── CommentsAndGrading
        │   ├── TeacherComments
        │   └── GradingKey
        │
        ├── PrincipalRemark
        │
        ├── SignatureSection
        │
        └── Footer
```

Prefer QuestPDF `IComponent` classes for major reusable sections if the application already uses component architecture.

QuestPDF explicitly supports modular components and recommends reusable document structures for maintainability.

Reference:
https://www.questpdf.com/features-overview.html

---

# 34. Suggested C# Data Contracts

Do not couple the PDF to EF entities.

Use dedicated view/data contracts.

Example:

```csharp
public sealed record ReportCardData(
    SchoolReportCardSettings School,
    StudentReportCardData Student,
    IReadOnlyList<SubjectResult> Subjects,
    IReadOnlyList<GradeBand> GradingScale
);
```

School settings:

```csharp
public sealed record SchoolReportCardSettings(
    string Name,
    string Subtitle,
    string Motto,
    string AcademicYear,
    byte[]? Logo,
    string? LogoSvg,
    string PostalAddress,
    string Phone,
    string Email,
    string Website,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string TableBackgroundColor,
    string GridColor,
    string BodyFont,
    string DisplayFont,
    string DateFormat,
    string PrincipalName,
    byte[]? PrincipalSignature
);
```

Student:

```csharp
public sealed record StudentReportCardData(
    string Name,
    string AdmissionNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string ClassName,
    string Term,
    string AcademicYear,
    string? House,
    DateOnly ReportDate,
    byte[]? Photo,
    string TeacherComments,
    string PrincipalRemark,
    string ClassTeacherName,
    byte[]? ClassTeacherSignature,
    int TotalMarks,
    int MaximumMarks,
    decimal OverallAverage,
    string OverallGrade,
    int ClassPosition,
    int ClassSize,
    string Status
);
```

This is illustrative; adapt to the existing application's architecture.

---

# 35. Theme Resolution

Create the theme once:

```csharp
var theme = ReportCardTheme.From(schoolSettings);
```

Then pass it to components.

Do not repeatedly parse hex colours throughout the document.

Example:

```csharp
public sealed class ReportCardTheme
{
    public string PrimaryNavy { get; init; } = "#00184D";
    public string SecondaryNavy { get; init; } = "#0A2860";
    public string AccentGold { get; init; } = "#E2A01B";
    public string LightBlue { get; init; } = "#ECF3FE";
    public string GridBlue { get; init; } = "#9FB4D2";
    public string Text { get; init; } = "#17213A";
}
```

This makes future school branding changes straightforward.

---

# 36. QuestPDF Styling Pattern

Use local reusable style functions instead of repeating:

```csharp
.Background(...)
.Border(...)
.BorderColor(...)
.Padding(...)
.FontSize(...)
.FontColor(...)
```

For example:

```csharp
IContainer ResultsCellStyle(IContainer container)
{
    return container
        .Border(0.6f)
        .BorderColor(theme.GridBlue)
        .PaddingVertical(2)
        .PaddingHorizontal(2);
}
```

QuestPDF explicitly documents reusable cell-style patterns as a recommended way of keeping table styling consistent.

Reference:
https://www.questpdf.com/api-reference/table/cell-style-pattern.html

---

# 37. Default Text Style

Set a page-level default style:

```csharp
page.DefaultTextStyle(text =>
    text
        .FontFamily(theme.BodyFont)
        .FontSize(8.5f)
        .FontColor(theme.Text)
);
```

Then override only where necessary.

This is preferable to specifying the font family and colour on every text element.

QuestPDF supports inherited default text styles.

Reference:
https://www.questpdf.com/api-reference/default-text-style.html

---

# 38. Page Composition Recommendation

Use:

```csharp
Document.Create(document =>
{
    document.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.PageColor(Colors.White);

        page.DefaultTextStyle(...);

        page.Content()
            .Padding(2, Unit.Millimetre)
            .Element(ComposeReportCard);
    });
});
```

Then:

```csharp
void ComposeReportCard(IContainer container)
{
    container
        .Border(0.8f)
        .BorderColor(theme.PrimaryNavy)
        .Column(column =>
        {
            ...
        });
}
```

Do not set a conventional large page margin.

The reference design uses nearly the entire A4 page.

---

# 39. Avoid Absolute Positioning

Do not build the entire report using:

```text
TranslateX
TranslateY
Canvas
fixed coordinates
```

Normal flow is much more robust.

Use:

```text
Row
Column
Table
RelativeItem
ConstantItem
Padding
AlignCenter
AlignMiddle
Border
Background
```

Absolute/graphic positioning should be reserved for decorative details such as:

- ribbon notch;
- decorative swoosh;
- small ornamental diamond;
- special logo overlays.

QuestPDF provides many normal flow and positional elements; use those before resorting to hard coordinates.

Reference:
https://www.questpdf.com/api-reference/tip-layout-constraints.html

---

# 40. Use Relative Columns Instead of Hard Pixel Widths

For example:

```csharp
table.ColumnsDefinition(columns =>
{
    columns.RelativeColumn(1.75f);
    columns.RelativeColumn(1f);
    columns.RelativeColumn(1f);
    columns.RelativeColumn(1f);
    columns.RelativeColumn(1f);
    columns.RelativeColumn(1f);
    columns.RelativeColumn(1f);
    columns.RelativeColumn(0.9f);
});
```

This is better than hard-coding pixel widths because the table remains aligned when page/content dimensions are adjusted.

QuestPDF's table API explicitly supports relative and constant column definitions.

Reference:
https://www.questpdf.com/api-reference/table/basics.html

---

# 41. Use Constant Dimensions for Small Decorative Elements

Good candidates for fixed dimensions:

```text
Logo maximum area
Student photo
Academic ribbon
Signature image height
Icon size
Diamond ornament
Border thickness
```

Use relative sizing for:

```text
Main columns
Results columns
Summary cells
Footer zones
Comments/grading split
```

This produces a stable but flexible layout.

---

# 42. Images — Performance

Do not repeatedly load the same logo from disk for every component.

If the school logo is reused:

- load it once;
- cache/preload it;
- pass the image object into components.

QuestPDF supports shared/preloaded images and SVG preloading, which can reduce repeated image processing.

SVG reference:
https://www.questpdf.com/api-reference/image/svg.html

Image reference:
https://www.questpdf.com/api-reference/image/basics.html

---

# 43. Images — Resolution

Student photographs do not need to be enormous.

For an approximately 35 × 45 mm printed photo:

```text
300 DPI target ≈ 413 × 531 px
```

A source image around:

```text
600 × 800 px
```

is more than sufficient for typical use.

Avoid embedding multi-megapixel originals when they are not necessary.

For logos, prefer SVG.

For signatures, prefer transparent PNG or SVG.

---

# 44. SVG Recommendation

Use SVG for:

- school logo;
- signature if available as vector;
- icons;
- academic-year ribbon decoration;
- footer gold swoosh;
- small decorative symbols.

This improves scaling and reduces rasterization artifacts.

QuestPDF has first-class SVG support.

Reference:
https://www.questpdf.com/api-reference/image/svg.html

---

# 45. Tables — Header Repetition

The report normally contains only a few subject rows, so pagination is not expected.

Nevertheless, implement the results table using QuestPDF's table API rather than manually constructed rows.

If future configurations permit many subjects, QuestPDF's table header functionality can repeat headers across pages.

Reference:
https://www.questpdf.com/api-reference/table/header-and-footer.html

---

# 46. Content Overflow Strategy

The preferred output is **one A4 page**.

However, do not make layout constraints impossible.

Avoid excessive use of:

```text
Height(...)
MinHeight(...)
MaxHeight(...)
```

especially on text containers.

QuestPDF warns that impossible constraints can cause layout exceptions.

Reference:
https://www.questpdf.com/api-reference/tip-layout-constraints.html

Instead:

1. allow natural text wrapping;
2. use sensible minimum heights;
3. reduce spacing;
4. reduce font size within configured limits;
5. only then consider allowing a second page.

---

# 47. One-Page Control Strategy

For the normal report:

- constrain major section heights only where the design absolutely requires it;
- use compact padding;
- use small table row padding;
- keep comment areas reasonably bounded;
- ensure signatures and footer remain visible.

A useful implementation technique is to render representative worst-case data during development:

```text
Very long school name
Very long student name
Long teacher comment
Long principal remark
10–15 subjects
Long grading remarks
```

The production document should not be tested only against the short sample data.

---

# 48. Data Formatting Rules

Keep formatting functions outside the visual components.

Examples:

```csharp
string FormatDate(DateOnly? date, string format)
string FormatAverage(decimal average)
string FormatPosition(int position, int classSize)
string FormatMarks(int total, int maximum)
```

Then the component receives ready-to-display values where practical.

This keeps QuestPDF components concerned with layout rather than business rules.

---

# 49. Accessibility / Semantic Consideration

If the application later needs accessible PDFs, maintain meaningful content structure.

Do not turn all text into images.

Text should remain actual PDF text.

Logos, decorative icons and signatures can be images.

QuestPDF supports accessibility/PDF-UA features; therefore a reusable component architecture leaves room for future accessibility improvements.

Reference:
https://www.questpdf.com/features-overview.html

---

# 50. Suggested Project Structure

```text
Reports/
    ReportCards/
        ReportCardDocument.cs
        ReportCardTheme.cs
        ReportCardData.cs

        Components/
            ReportCardHeader.cs
            StudentInformation.cs
            ResultsTable.cs
            SummaryStrip.cs
            TeacherComments.cs
            GradingKey.cs
            PrincipalRemark.cs
            SignatureSection.cs
            ReportCardFooter.cs

        Styling/
            ReportCardStyles.cs

Resources/
    Fonts/
    ReportCard/
        ribbon.svg
        footer-swoosh.svg
        icons/
```

The exact structure can differ, but avoid putting all code in one 1,000+ line `Compose()` method.

---

# 51. School Settings Integration Map

The coding agent should explicitly map the following visual elements to settings/data.

| Visual element | Source |
|---|---|
| School logo | `SchoolSettings.Logo` |
| School name | `SchoolSettings.Name` |
| School subtitle/type | `SchoolSettings.Subtitle` |
| Motto | `SchoolSettings.Motto` |
| Academic year | report/school settings |
| Primary navy | `SchoolSettings.PrimaryColor` |
| Secondary navy | `SchoolSettings.SecondaryColor` |
| Gold | `SchoolSettings.AccentColor` |
| Table background | `SchoolSettings.TableBackgroundColor` |
| Grid colour | `SchoolSettings.GridColor` |
| Body font | `SchoolSettings.BodyFont` |
| Display font | `SchoolSettings.DisplayFont` |
| Address | `SchoolSettings.PostalAddress` |
| Phone | `SchoolSettings.Phone` |
| Email | `SchoolSettings.Email` |
| Website | `SchoolSettings.Website` |
| Principal name | `SchoolSettings.PrincipalName` |
| Principal signature | `SchoolSettings.PrincipalSignature` |
| Student photo | `Student.Photo` |
| Student name | `Student.Name` |
| Admission number | `Student.AdmissionNumber` |
| DOB | `Student.DateOfBirth` |
| Gender | `Student.Gender` |
| Class | `Student.ClassName` |
| Term | `Student.Term` |
| House | `Student.House` |
| Report date | `Student.ReportDate` |
| Subjects | `Report.Subjects` |
| Teacher comments | `Student.TeacherComments` |
| Principal remark | `Student.PrincipalRemark` |
| Class teacher | `Student.ClassTeacherName` |
| Class teacher signature | `Student.ClassTeacherSignature` |
| Grading scale | `SchoolSettings.GradingScale` |

---

# 52. Replaceable / Optional Content

The following should be optional:

```text
School motto
School subtitle
Student photo
House
Student gender
Principal signature
Class teacher signature
Website
Icons
Decorative elements
```

If a field is absent:

- hide it cleanly;
- preserve the overall grid;
- avoid showing empty labels where possible.

For example:

If no website:

```text
do not render an empty globe + blank line
```

Instead allow the remaining footer items to redistribute their available width.

---

# 53. Theme Customization Without Breaking the Design

School settings should control colours, but enforce readability.

For example:

```csharp
theme.PrimaryNavy
theme.AccentGold
```

may be configurable.

However:

- footer text must remain readable;
- section headers need sufficient contrast;
- table text must remain dark enough;
- borders must remain visible when printed.

Do not allow arbitrary school colours to make the template visually unusable.

A validation layer can reject colours with insufficient contrast.

---

# 54. Print Considerations

The report should be designed for physical printing.

Avoid:

- extremely thin borders below approximately 0.5 pt;
- very light text;
- excessive transparency;
- tiny icons;
- edge-to-edge elements.

The gold decoration can be decorative, but the document must remain readable if printed in grayscale.

The navy header bars should still have strong tonal contrast against the white/light-blue areas.

---

# 55. Visual QA Process

After implementing the QuestPDF template:

1. Generate PDF using the exact sample data.
2. Render PDF to PNG at 150–300 DPI.
3. Compare against the reference.
4. Check page dimensions.
5. Check all major section boundaries.
6. Check table column alignment.
7. Check logo/photo scaling.
8. Check text wrapping.
9. Check footer placement.
10. Check that the page remains exactly one A4 page.

Create a test snapshot with the same sample values shown in the reference.

---

# 56. Important Reference Test Data

Use the following only as a **development fixture**.

## School

```text
BRIGHT FUTURE
SECONDARY SCHOOL
Knowledge. Discipline. Excellence.
Academic Year: 2025/2026
```

## Student

```text
NABUWATI NANGOBI
SSS/2023/0158
S.3 BLUE
SECOND TERM
12 March 2010
Female
EDISON
28-05-2026
```

## Results

```text
English              100  20  20  60  246  82.0  B+
Science              100  20  20  60  230  76.7  B
Social Studies       100  20  20  60  236  78.7  B+
Mathematics          100  20  20  60  210  70.0  B-
Religious Education  100  20  20  60  270  90.0  A
```

## Summary

```text
1,192 / 1,500
79.5%
B+
7 / 32
PROMOTED
```

## Footer

```text
P.O. Box 12345, Kampala, Uganda
+256 701 234 567
info@brightfuture.sc.ug
www.brightfuture.sc.ug
```

These values should exist only in a fixture/test class.

---

# 57. QuestPDF References

The coding agent should use the official QuestPDF documentation as the primary technical reference.

## Core

- Quick start: https://www.questpdf.com/quick-start.html
- Page settings: https://www.questpdf.com/api-reference/page/settings.html
- Page basics: https://www.questpdf.com/api-reference/page/basics.html

## Layout

- Layout constraints: https://www.questpdf.com/api-reference/tip-layout-constraints.html
- Components / architecture: https://www.questpdf.com/features-overview.html

## Styling

- Colours: https://www.questpdf.com/concepts/colors.html
- Content styling patterns: https://www.questpdf.com/concepts/code-patterns/content-styling.html
- Default text style: https://www.questpdf.com/api-reference/default-text-style.html
- Text style: https://www.questpdf.com/api-reference/text/text-style.html
- Font management: https://www.questpdf.com/api-reference/text/font-management.html

## Tables

- Table basics: https://www.questpdf.com/api-reference/table/basics.html
- Cell style pattern: https://www.questpdf.com/api-reference/table/cell-style-pattern.html
- Table headers/footers: https://www.questpdf.com/api-reference/table/header-and-footer.html

## Images

- Image basics: https://www.questpdf.com/api-reference/image/basics.html
- SVG support: https://www.questpdf.com/api-reference/image/svg.html

## Page layers / decoration

- Page slots: https://www.questpdf.com/api-reference/page/slots.html
- Background: https://www.questpdf.com/api-reference/background.html

Prefer official QuestPDF documentation over third-party examples when API behaviour differs.

---

# 58. Recommended Implementation Philosophy

The coding agent should follow these principles:

### 1. Data-driven

No school-specific information in the reusable PDF component.

### 2. Componentized

Every major visual block is independently maintainable.

### 3. Flow-based

Use Row/Column/Table/Container rather than absolute coordinates.

### 4. Theme-driven

Colours and fonts come from a single theme object.

### 5. Print-first

Design for A4 physical output, not merely browser-sized screenshots.

### 6. Image-safe

Preserve image aspect ratios and use SVG wherever appropriate.

### 7. Dynamic-content-safe

Long names and comments must wrap without overlapping.

### 8. Performance-aware

Cache/reuse images and avoid unnecessarily large embedded assets.

### 9. Testable

Provide a sample fixture and PDF snapshot test.

### 10. Maintainable

Avoid repeating style definitions and avoid one giant Compose method.

---

# 59. Final Visual Target

The completed document should visually read as:

```text
┌─────────────────────────────────────────────────────────────┐
│ LOGO     SCHOOL NAME                         ACADEMIC YEAR  │
│          SCHOOL SUBTITLE                                  │
│          Motto                                             │
│                 ── ◆ REPORT CARD ◆ ──                      │
├─────────────────────────────────────────────────────────────┤
│ STUDENT INFORMATION                         STUDENT PHOTO  │
│ Name       : __________       DOB       : _________        │
│ Admission  : __________       Gender    : _________        │
│ Class      : __________       House     : _________        │
│ Term       : __________       Report Dt : _________        │
│ Academic Yr: __________                                      │
├─────────────────────────────────────────────────────────────┤
│             PROMOTIONAL EXAMINATION RESULTS                │
├────────┬────────┬──────┬──────┬──────┬──────┬──────┬────────┤
│Subject │Max     │CAT 1 │CAT 2 │Exam  │Total │Avg   │Grade   │
├────────┼────────┼──────┼──────┼──────┼──────┼──────┼────────┤
│ ...    │ ...    │ ...  │ ...  │ ...  │ ...  │ ...  │ ...    │
├────────┴────────┴──────┴──────┴──────┴──────┴──────┴────────┤
│ TOTAL │ AVERAGE │ GRADE │ POSITION │ STATUS                  │
├────────────────────────────┬────────────────────────────────┤
│ TEACHER'S COMMENTS         │ GRADING KEY                    │
│                            │                                │
│                            │                                │
├────────────────────────────┴────────────────────────────────┤
│ PRINCIPAL'S REMARK                                          │
│                                                            │
├────────────────────┬────────────────────┬───────────────────┤
│ PRINCIPAL SIGN.    │ CLASS TEACHER SIGN │ DATE              │
├────────────────────┴────────────────────┴───────────────────┤
│ ADDRESS      │ PHONE       │ EMAIL       │ WEBSITE           │
└─────────────────────────────────────────────────────────────┘
```

This wireframe is not intended to replace the visual reference. It establishes the structural relationships the QuestPDF implementation should preserve.

---

# 60. Acceptance Criteria

The implementation is complete when:

- [ ] Output is exactly A4 portrait.
- [ ] Normal report fits on one page.
- [ ] Outer navy frame matches the reference proportions.
- [ ] Header consumes approximately 15% of the page.
- [ ] Logo is dynamically loaded from school settings.
- [ ] School name, subtitle and motto are dynamic.
- [ ] Academic year ribbon is dynamic.
- [ ] Student information is dynamic.
- [ ] Student photograph is dynamic.
- [ ] Results table uses QuestPDF `Table`.
- [ ] Results columns have stable relative widths.
- [ ] Results rows are generated dynamically.
- [ ] Summary values are dynamic.
- [ ] Teacher comments are dynamic.
- [ ] Grading scale is configurable.
- [ ] Principal remark is dynamic.
- [ ] Principal and class-teacher signatures are dynamic.
- [ ] Footer address/contact data is dynamic.
- [ ] Colours are theme-driven.
- [ ] Fonts are configurable and deployment-safe.
- [ ] SVG logos/decorations are supported.
- [ ] Images preserve aspect ratio.
- [ ] No production component contains the sample school's identity.
- [ ] No business calculations are embedded in the rendering component.
- [ ] Long names/comments do not overlap.
- [ ] Table borders and section bars match the reference visual weight.
- [ ] PDF remains readable when printed.
- [ ] The generated output has been visually compared with the supplied reference.

---

# 61. Critical Instruction to the Coding Agent

**Do not interpret this document as an instruction to reproduce the literal sample school.**

The sample is a **template reference**.

The actual implementation must render:

```text
SchoolSettings
+
StudentReportData
+
AssessmentResults
+
GradingConfiguration
+
BrandAssets
```

into the same visual structure.

The goal is a reusable QuestPDF report-card renderer that can generate a report for any configured school while preserving the supplied reference's:

- A4 proportions;
- compact information density;
- navy/gold visual language;
- typography hierarchy;
- table structure;
- section spacing;
- border treatment;
- footer composition;
- professional school-report appearance.
