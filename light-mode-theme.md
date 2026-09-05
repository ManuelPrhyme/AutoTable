# AutoTable — Light Mode Implementation Guide

> **Goal:** Transform AutoTable from its current *always-dark-chrome* look into a fully realised, first-class **Light Mode**, while keeping the existing Dark Mode intact. This document is a design + implementation blueprint: it audits the current theming, defines the target light palette, and walks through the transformation of **every feature area and layout surface**.
>
> **Audience:** developers implementing the theme. Covers the whole repo: `Resources/DesignTokens.xaml`, `Services/ThemeService.cs`, `App.xaml(.cs)`, `Views/ShellView.xaml(.cs)`, all module views, controls, and print sheets.

---

## 1. What the app does today (audit)

AutoTable is a **WinUI 3 (Windows App SDK 2.3.x)** school-management dashboard. Theming is centralised in **one file** — `Resources/DesignTokens.xaml` — and switched at runtime by `ThemeService`.

### 1.1 Theme dictionaries present

`Resources/DesignTokens.xaml` defines **three** `ResourceDictionary.ThemeDictionaries` (line numbers from the current file):

| Dictionary | Lines | Content type |
|------------|-------|--------------|
| `Default` | 15–224 | **Light clone** — light surfaces/texts, but an **always-dark navy sidebar** (#1E1E1E) |
| `Light` | 225–435 | Duplicate of `Default` (also keeps the navy sidebar #1E1E1E) |
| `Dark` | 436–636 | True dark theme (backgrounds #1E1E1E, sidebar #121212) |

WinUI rule: when `Application.RequestedTheme` is `Light`/`Dark`, the matching dictionary is used. When it is `Default`, the **`Default`** dictionary is used as the common/fallback. Today `Default` and `Light` are effectively the same file content.

### 1.2 Why it looks “in dark mode” even in “light”

The **chrome (sidebar) is never light**. Both `Default` and `Light` dictionaries define the sidebar as dark navy:

```text
Light/Default:  SidebarColor      #1E1E1E   SidebarHoverColor #2A2A2A   SidebarActiveColor #383838   SidebarBorderColor #2A2A2A
Dark:           SidebarColor      #121212   SidebarHoverColor #1E1E1E   SidebarActiveColor #2E2E2E   SidebarBorderColor #1E1E1E
```

Because the shell, navigation items, and the login illustration panel all draw their text with the **`TextOnDarkBrush`/`TextOnDarkMutedBrush`** tokens (meant for the navy), the app always *reads* as dark even when the content area is light.

### 1.3 How the theme is applied today

- `App.xaml.cs` (around line 721) registers the window: `ThemeService.Initialize(_window);` — **no forced startup theme**. The app therefore boots with `Application.RequestedTheme == Default` and follows the OS.
- The only runtime switch is the header `ToggleSwitch` (`ThemeToggle`, `OffContent="Light"` / `OnContent="Dark"`) in `Views/ShellView.xaml`; its `Toggled` handler calls `ThemeService.SetTheme(ApplicationTheme.Light / Dark)`.
- `Services/ThemeService.cs`: sets `app.RequestedTheme` **and** the root element `RequestedTheme`, so `{ThemeResource}` lookups refresh the open window immediately.
- Code-behind looks up brushes by dictionary key: `ShellView.GetThemeBrush(...)` and `ThemeResourceHelper.GetCurrentThemeKey()` in `Converters/FormatConverters.cs` both map `Dark → "Dark"`, else `"Light"` (see §6 re keeping this accurate while `Default` is a Light clone).

### 1.4 Known gaps

| # | Gap | Where |
|---|-----|-------|
| G1 | **No preference persistence** — the chosen theme is never saved, so it resets to OS Default every launch. | `ThemeService`, `SessionService` (no theme field) |
| G2 | **`Default` and `Light` are duplicates** — drift risk; Light is not an independent, tuned theme. | `DesignTokens.xaml` lines 15–435 |
| G3 | **Sidebar is dark in every theme** — light mode does not yet exist visually. | Sidebar tokens + `ShellView` |
| G4 | **Hard-coded hexes** outside the token file (report-card brand navy, mid-term slip, `#F8F8F8` in settings). Some are intentional (print sheets), one is a leftover. | `ReportCardSheetView`, `MidTermSlipView`, `SchoolSettingsView` |
| G5 | **`TextOnDark` is overloaded** — used on the navy sidebar and the navy login panel; it is wrong once the sidebar becomes light. | `ShellView`, `LoginView` |
---

## 2. Design decisions

### D1. Sidebar treatment (the central brand decision)

Choose **one** strategy — everything else in this guide depends on it.

**Option A — "Keep the navy sidebar constant" (smallest effort).**
The left rail stays deep navy `#1E1E1E` in *both* themes (a deliberate brand constant, like VS Code/GitHub sidebars). Only the content area flips light. Pros: minimal changes, no new text tokens, strong brand. Cons: still "dark chrome"; light mode is partial. The content-area work in this guide still applies under **Option A**.

**Option B — "Truly light sidebar" (recommended for a real light mode).**
In the `Light` dictionary the sidebar becomes a **light surface** (`#FFFFFF`, divider `#E5E7EB`, hover `#F2F4F7`, active `#2196F3`), and sidebar **text flips to dark**. Requires ~4 new sidebar text tokens and small edits to `ShellView` + `LoginView`. This is the full light experience.

> **Recommendation:** implement **Option B** for a genuine light UI, but you can ship **Option A** first as a stepping-stone (§10). The remainder of the guide assumes **Option B** unless stated otherwise.

### D2. Where "white/navy" surfaces must NOT change

**Report cards, mid-term slips, and any A4 print/preview surface must stay light with navy brand regardless of app theme.** They are printed documents — paper is white. Do **not** route them through theme tokens; leave their hard-coded `#1F3864` / `#2E86C1` / `#E8EDF7` / `#D4E6F1` values as-is (already light-on-navy and intentionally theme-independent). Only theme the **hosting preview page** (chrome around them).

### D3. Persistence decision

Add a small **theme preference store** so users keep their choice across launches (§6.1).

---

## 3. Target palette (Light)

Baseline from the current `Light` dictionary, plus the **new/changed tokens** for a real light mode. Keep values consistent between `Light` and `Default` (§4.1).

### 3.1 Core surfaces & text

| Token | Light value | Purpose |
|-------|-------------|---------|
| `BackgroundColor` | `#F5F5F5` | page / app background |
| `SurfaceWhiteColor` | `#FFFFFF` | cards, header, footer, modals |
| `SurfaceGrayColor` | `#F5F5F5` | neutral page / panel background |
| `SurfaceGray2Color` | `#EEEEEE` | table headers, inset fills |
| `SurfaceElevatedColor` | `#FFFFFF` | popups, flyouts |
| `SurfaceSunkenColor` | `#E9E9E9` | pressed / disabled wells |
| `SurfaceDisabledColor` | `#F0F0F0` | disabled controls |
| `TextPrimaryColor` | `#2D2D2D` | headers, titles (≈13:1 on white) |
| `TextSecondaryColor` | `#5A5A5A` | body / secondary — raise from `#666` to pass AA ≈4.9:1 |
| `TextMutedColor` | `#6B7280` | captions — **darken from `#9E9E9E`** (≈2.8:1 → `#6B7280` ≈4.4:1) |
| `TextDisabledColor` | `#BDBDBD` | disabled text (low contrast OK) |
| `TextOnAccentColor` | `#FFFFFF` | text on solid accent (blue/green/…) |
| `TextOnDarkColor` | `#FFFFFF` | **kept for navy login panel + print** |
| `TextOnDarkMutedColor` | `#CCCCCC` | **kept for navy login panel** |

### 3.2 NEW sidebar tokens (only for **Option B**)

| Token | Light value | Notes |
|-------|-------------|-------|
| `SidebarTextColor` *(new)* | `#1F2933` | near-black text on a light rail |
| `SidebarTextMutedColor` *(new)* | `#6B7280` | section labels, logo subtitle |
| `SidebarBorderColor` *(change)* | `#E5E7EB` | fine divider under the logo |
| `SidebarHoverColor` *(change)* | `#F2F4F7` | subtle hover fill |
| `SidebarActiveColor` *(change)* | `#2196F3` *(solid)* **or** `#E3F2FD` *(soft)* | active "pill" — §5.1 |

Add matching brush aliases in the same dictionary: `SidebarTextBrush`, `SidebarTextMutedBrush` (keeping `SidebarHoverBrush`, `SidebarActiveBrush`, `SidebarBorderBrush`). Keep `NavySidebarBrush` / `NavyDarkBrush` aliasing `SidebarColor` so existing references keep resolving.

### 3.3 Accent & status palette (shared, unchanged)

Theme-independent brand colors, left as-is (already contrast-safe on both themes):

| Token | Value | Role |
|-------|-------|------|
| `BlueColor` / `PrimaryBlueBrush` | `#2196F3` | primary, info, students, search |
| `GreenColor` / `SuccessGreenBrush` | `#4CAF50` | success, present, teachers |
| `OrangeColor` / `WarningAmberBrush` | `#FF9800` | warning |
| `RedColor` / `DangerRedBrush` | `#F44336` | danger, at-risk |
| `PurpleColor` / `AccentPurpleBrush` | `#673AB7` | accent |
| `TealColor` / `FinanceTealBrush` | `#009688` | finance, fees |
---

## 4. Step 1 — Refactor `Resources/DesignTokens.xaml`

### 4.1 Make `Default` the single source of truth for Light

Because `Default` is used whenever the app follows the OS (`RequestedTheme == Default`), treating it as "Light" here is correct (WinUI treats `Default` as the common/fallback dictionary). To remove G2 drift:

1. **Pick one canonical Light definition.** XAML cannot `BasedOn` a whole dictionary, so the pragmatic route is: **edit `Light` first, then copy the verified block into `Default`** so OS-Default and explicit Light render identically. Or use a build-time script (§7).
2. Apply every §5 change to **both** `Light` (lines 225–435) and `Default` (lines 15–224).

### 4.2 Exact edits (Option B) — per dictionary

For **`Light`** (mirrored into **`Default`**):

```xml
<!-- Surfaces / text tweaks -->
<Color x:Key="TextSecondaryColor">#5A5A5A</Color>      <!-- was #666666 -->
<Color x:Key="TextMutedColor">#6B7280</Color>          <!-- was #9E9E9E -->

<!-- Sidebar becomes light -->
<Color x:Key="SidebarColor">#FFFFFF</Color>            <!-- was #1E1E1E -->
<Color x:Key="SidebarHoverColor">#F2F4F7</Color>       <!-- was #2A2A2A -->
<Color x:Key="SidebarActiveColor">#2196F3</Color>      <!-- was #383838 (or #E3F2FD soft) -->
<Color x:Key="SidebarBorderColor">#E5E7EB</Color>      <!-- was #2A2A2A -->

<!-- NEW text tokens for the light rail -->
<Color x:Key="SidebarTextColor">#1F2933</Color>
<Color x:Key="SidebarTextMutedColor">#6B7280</Color>
```

Add brushes (in each dictionary's brush block):

```xml
<SolidColorBrush x:Key="SidebarTextBrush" Color="{StaticResource SidebarTextColor}" />
<SolidColorBrush x:Key="SidebarTextMutedBrush" Color="{StaticResource SidebarTextMutedColor}" />
```

Leave the **Dark** dictionary (lines 436–636) sidebar unchanged (still `#121212`).

> **Option A** alternative: skip the sidebar color/text edits; keep `Sidebar*` as today. Everything else still applies.

### 4.3 Make `SidebarItemTextStyle` theme-aware

The existing `SidebarItemTextStyle` (line ~733) hard-codes `TextOnDarkBrush`. Under Option B, switch its foreground setter so it works on both rails:

```xml
<Setter Property="Foreground" Value="{ThemeResource SidebarTextBrush}" />  <!-- was TextOnDarkBrush -->
```

For any inline `Foreground="{ThemeResource TextOnDarkBrush}"` inside **nav buttons**, switch to `SidebarTextBrush`. Keep `TextOnDarkBrush` for the navy login/header logo (D2).
---

## 5. Step 2 — Shell & layout

### 5.1 Navigational active state (code-behind)

`Views/ShellView.xaml.cs` `SetActiveButton` (lines ~243–256) and `GetThemeBrush` (222–241) need updating:

- `GetThemeBrush` already resolves the current dictionary — good. But nav buttons read `GetThemeBrush("TextOnDarkBrush")` as foreground. Change the **reset** and **active** foregrounds to `SidebarTextBrush`:

```csharp
// reset previous
_activeNavButton.Foreground = GetThemeBrush("SidebarTextBrush");   // was TextOnDarkBrush
...
active.Background = GetThemeBrush("SidebarActiveBrush");           // unchanged (now #2196F3 or #E3F2FD)
active.Foreground = GetThemeBrush("SidebarTextBrush");             // was TextOnDarkBrush
```

- **Active "pill" contrast:** if `SidebarActiveColor` = solid `#2196F3`, the active item's foreground must be **white** → use `TextOnAccentBrush` when active; for soft `#E3F2FD`, keep `SidebarTextBrush`. Pick **one** convention and implement it in `SetActiveButton` by tracking whether the active background is solid-blue (branch on the current palette, or a helper that returns the right foreground given the active color).

### 5.2 Shell sidebar XAML (`Views/ShellView.xaml`)

- Sidebar root `Background="{ThemeResource NavySidebarBrush}"` — `NavySidebarBrush` aliases `SidebarColor`, retargeted to white for Light. **No change needed** — it re-resolves automatically.
- Logo block: switch "AutoTable" title and "Bright Future Primary" subtitle to `SidebarTextBrush` / `SidebarTextMutedBrush`; the logo square keeps `PrimaryBlueBrush` with a `TextOnAccentBrush` icon.
- The `PERFORMANCE` module label uses `TextOnDarkMutedBrush` → change to `SidebarTextMutedBrush`.
- Every nav `FontIcon` + label uses `TextOnDarkBrush` → change to `SidebarTextBrush`.
- Section divider `BorderBrush="{ThemeResource SidebarBorderBrush}"` auto-updates to `#E5E7EB`.

### 5.3 Header bar, footer, content frame

Verify these follow §3.1 (they already use correct tokens):

- Header `Border` → `SurfaceWhiteBrush`, `BorderLightBrush` bottom border ✓.
- `TopSearchBox` (themed `SearchBoxStyle`), `TermSelector` (`ComboBoxStyle`), notifications/`ThemeToggle`, `HeaderUserName` (`TextPrimaryBrush`) — all theme-aware ✓.
- Footer `StatusFooterBar` → `SurfaceWhiteBrush` + `BorderLightBrush` ✓.
- `ContentFrame` background comes from each page's root.

### 5.4 Typography on light

Most styles already bind to `{ThemeResource TextPrimaryBrush / TextSecondaryBrush}` and re-resolve on switch. `MutedTextStyle`, `TableCellStyle`, etc. reference `TextMutedBrush` / `TextSecondaryBrush`, so fixing those two tokens in §4.2 fixes caption legibility app-wide. No per-view text overrides needed unless a view hard-coded black/white foreground — audit in §8.
---

## 6. Step 3 — Theme service, persistence & login

### 6.1 Persist the preference

Add persistence to `ThemeService` via `Windows.Storage.ApplicationData` (available in WinUI 3 at runtime):

```csharp
// Services/ThemeService.cs — add:
private const string PrefKey = "AppTheme";

public static void ApplySavedTheme()
{
    try
    {
        var saved = ApplicationData.Current.LocalSettings.Values.TryGetValue(PrefKey, out var v)
            ? v?.ToString() : null;
        if (string.Equals(saved, "Light", StringComparison.OrdinalIgnoreCase))
            SetTheme(ApplicationTheme.Light);
        else if (string.Equals(saved, "Dark", StringComparison.OrdinalIgnoreCase))
            SetTheme(ApplicationTheme.Dark);
        // else: leave OS Default
    }
    catch { /* LocalSettings may be unavailable in some tooling */ }
}

public static void SetTheme(ApplicationTheme theme)
{
    // ... keep existing RequestedTheme + root element logic ...
    try { ApplicationData.Current.LocalSettings.Values[PrefKey] =
        theme == ApplicationTheme.Dark ? "Dark" : "Light"; } catch { }
}
```

Call `ThemeService.ApplySavedTheme()` in `App.xaml.cs` **after** `ThemeService.Initialize(_window);` (line ~721) so a persisted choice wins at startup.

### 6.2 Keep the map `Default → Light` correct

`ShellView.GetThemeBrush` and `FormatConverters.GetCurrentThemeKey` both return `"Light"` when not Dark. Since `Default` is a Light clone, this stays correct after our edits. **Do not** add a `case "Default"`; the "not Dark → Light" fallback continues to cover it.

### 6.3 Theme toggle initial state

Syncing the toggle when theme is restored avoids a "misleading switch": in `ShellView.Loaded` (or `OnNavigatedTo`) set `ThemeToggle.IsOn = (app.RequestedTheme == ApplicationTheme.Dark)` so the header reflects the true state.

### 6.4 Login view (`Views/LoginView.xaml`)

- **Left illustration panel** (`Grid.Column="0" Background="{ThemeResource NavyDarkBrush}"`): `NavyDarkBrush` aliases `SidebarColor`, which under **Option B** is now **white**. That would turn the login's brand panel white and its `TextOnDarkBrush` text invisible.
  - **Fix:** this panel is a branded surface and should stay navy regardless of theme — do **not** bind it to `SidebarColor` (which becomes white under Option B). Introduce an explicit brand token and reference it: keep `TextOnDarkBrush`/`TextOnDarkMutedBrush` on this panel, and give the panel a **constant** navy background that never changes. Recommended: a new `LoginHeroColor` (`#1E293B`) defined in all three dictionaries, with a `LoginHeroBrush` alias, so the hero also no longer depends on the sidebar token.
  - **Simplest correct approach:** in the `Light`/`Default` dictionaries, add `LoginHeroColor = #1E293B` (a fixed deep navy) and a `LoginHeroBrush`, then use `Background="{ThemeResource LoginHeroBrush}"` + unchanged `TextOnDarkBrush` text. This keeps the illustrated hero legible in both themes regardless of the sidebar palette.
- **Right auth panel** (`Grid.Column="1" Background="{ThemeResource SurfaceGrayBrush}"`): light gray — correct for light mode; the inner `CardBorderStyle` (white) sits on it nicely. No change.

> Under **Option A** (sidebar stays navy), `NavyDarkBrush` remains `#1E1E1E`, so the login hero needs **no** change. Use LoginHeroColor anyway to decouple it from the sidebar token — safer.
---

## 7. Step 4 — Transform every feature area

Most module views already use themed styles/brushes (KpiCard, TableCardStyle, CardBorderStyle, FilterBarStyle, button styles, ComboBoxStyle, ModalDialogStyle), so they mostly need **verification** plus a few targeted swaps. Below is the per-feature checklist. Because the token layer does most of the work, the golden rule is: **only dip into a specific XAML file when it hard-codes a color or uses a "dark-only" text token on a light surface.**

### 7.1 Dashboard (`Views/DashboardView.xaml`)
- Page root `Background="{ThemeResource SurfaceGrayBrush}"` ✓.
- KPI row → `KpiCard` control (light `SurfaceWhiteBrush` card, blue accent bar) ✓.
- Tables (`TableCardStyle`, `TableHeaderRowStyle`, `TableCellStyle`) ✓ theme-aware.
- Quick Actions use `SecondaryButtonStyle` ✓.
- **Watch:** any DataTemplate you build row panels in code-behind — ensure `Foreground` uses `TextPrimaryBrush`, not white/black literals.

### 7.2 Students / Classes / Teachers / Assessment views
- Header bands, `FilterBarStyle`, search boxes, combo boxes → already themed. Confirm the `TableSelectedRowColor` (`#E3F2FD`) reads well on light (it does).
- Student "chip" UI in classes (`ClassesView.xaml.cs`) is drawn in code; if it uses `#FFFFFF` text on a dark fill, keep the dark fill but verify label text color is set deliberately.

### 7.3 Modals / dialogs (`ModalDialogStyle`, `DialogPrimaryButtonStyle`)
- Overlay `OverlayColor`/`ScrimColor` `#66000000` stays (correct for both themes) ✓.
- `DialogCloseButtonStyle` ghost button now needs a visible hover on light — it uses `HoverBrush`/`SelectedBrush`, already light-tuned ✓.
- Many "Record Payment", "Enroll Student", "Create Class" modals are built via `ContentDialog`/XAML with `SurfaceWhiteBrush` background; on light they are white-on-white — verify title/dark accent zones use `TextOnAccentBrush`, and that any button marked "Danger" has red-tinted hover using `DangerRedLightBrush` on light.

### 7.4 KPI cards (`Views/Controls/KpiCard.xaml`)
`ValueBlock` uses `TextPrimaryBrush` ✓; `AccentBar` + icon `PrimaryBlueBrush` ✓; `MutedTextStyle` title/subtitle ✓. No change required.

### 7.5 Charts & analytics
Charts are drawn with `ThemeResource` brushes (Blue/Green/Orange/Purple/Red series; `SurfaceGray2Brush` gridlines). On light:
- **Gridlines** `ChartGridlineColor` (= `#DDDDDD`) are visible on light — good. Do **not** darken, or they'll vanish.
- **Axis/labels** — if any chart uses `TextMutedBrush` for labels, they now read at `#6B7280` ✓.
- **Series**: keep saturated accent fills; on light they pop. Add `StrokeThickness` for line charts so light lines are legible.

### 7.6 Report cards & mid-term slips (print surfaces)
Per **D2**, leave `#1F3864` (report card navy), `#2E86C1` (mid-term brand), `#E8EDF7`/`#D4E6F1` (table fills), and inner body text `#555555`/`#77809A`/`#1E8449` **unchanged** — these are print-on-white documents. Only the hosting preview page (chrome) is themed.

### 7.7 Settings (`Views/SchoolSettingsView.xaml`)
Remove the leftover hard-coded light gray `#F8F8F8` from the background pill → replace with `{ThemeResource SurfaceGrayBrush}` (or `SurfaceGray2Brush`) so it matches the theme.

### 7.8 Dashboard header "Offline Mode" chip & avatar
Uses `SurfaceWhiteBrush` + `BorderLightBrush` ✓. Ellipse avatar `PrimaryBlueBrush` ✓.
---

## 8. Hardcoded-color audit (what to grep for)

To be exhaustive, search the source (excluding `.vs`, `bin`, `obj`) for stray literals and replace theme-affecting ones:

```text
#1F3864, #2E86C1, #E8EDF7, #D4E6F1   → LEAVE (print surfaces), see §7.6
#F8F8F8                              → SurfaceGrayBrush / SurfaceGray2Brush (Settings)
#000000 / #FFFFFF in XAML            → replace with TextPrimaryBrush / SurfaceWhiteBrush (unless print)
| Foreground="Black" / "White"   -> audit & convert to theme tokens
LinearGradientBrush                  → audit; gradients don't auto-switch — retarget stops via ThemeResource, else wrap per-theme
```

Note: `Colors.Black/White` and `new SolidColorBrush(...)` inside **code-behind** (e.g., `ShellView.SetActiveButton`, `ClassesView` chips, chart series) will **not** follow the theme. Prefer `ThemeResourceHelper.GetThemeBrush(key)` (already in `FormatConverters.cs`) when drawing in code.

---

## 9. Elevation & shadows (light-specific)

Light surfaces rely more on **borders and depth cues** than dark ones. Current setup uses 1px `BorderLightBrush` borders + a faint `CardShadowBrush` placeholder. For light mode:

- Keep 1px `#DDDDDD` card borders (essential separation on white) ✓ already.
- If you add real elevation, use `ThemeShadow`/`DropShadow` with `#1A000000` (soft dark) rather than `#0A000000` so cards lift off `#F5F5F5`. Increase on hover (a11y/affordance).
- Do **not** use pure `#000000` shadows at any opacity — they look dirty on light. Greys like `#0F000000`–`#26FFFFFF` (soft translucent black at low alpha) are safer.

---

## 10. Suggested roll-out phasing + final checklist

### Phasing
1. **Phase 0 (Option A, safe):** Ship the token refactor (§4.1, §4.2) for **content only** — keep navy sidebar. Verify every module renders correctly in Light. Low risk.
2. **Phase 1 (Option B):** flip `Sidebar*` tokens to light, add `SidebarText*`, update `ShellView`, `LoginView`, `SidebarItemTextStyle`, `SetActiveButton`. This is the visible "light mode" moment.
3. **Phase 2:** persistence (§6.1) + toggle initial-state sync (§6.3).
4. **Phase 3:** hardcoded-audit cleanup (§8) + chart/elevation polish (§9).

### Acceptance checklist (Light Mode)
- [ ] Sidebar is white, content is `#F5F5F5`/`#FFFFFF`, footer white — in **both** Light toggle **and** OS-Default.
- [ ] All nav icons/labels legible (dark on light rail); active item clearly highlighted.
- [ ] Header, search, term selector, notifications render correctly on white.
- [ ] All module pages: KPI cards, tables, filters, charts, modals readable at `TextPrimary #2D2D2D` / `TextSecondary #5A5A5A`.
- [ ] Print sheets (report card, mid-term) unchanged and white.
- [ ] Login hero stays navy/legible; auth card white.
- [ ] Dark mode still toggles back perfectly (regression).
- [ ] Persisted theme survives restart; toggle reflects current state.
- [ ] No hard-coded hexes leak `#1E1E1E`/`#121212`/`#FFFFFF` into surprising places in Light.
- [ ] `dotnet build AutoTable.csproj -p:Platform=x64` → 0 errors; integration tests pass.

---

## 11. Files to touch (quick reference)

| File | Change |
|------|--------|
| `Resources/DesignTokens.xaml` | Light/Default sidebar→light, text contrast, new `SidebarText*` brushes; add `LoginHeroBrush` |
| `Services/ThemeService.cs` | persistence (`ApplySavedTheme`, save in `SetTheme`) |
| `App.xaml.cs` | call `ApplySavedTheme()` after `Initialize` |
| `Views/ShellView.xaml` | nav text → `SidebarText*`, logo subtitle, module label |
| `Views/ShellView.xaml.cs` | `SetActiveButton`/`GetThemeBrush` foreground → `SidebarTextBrush` (white when active-solid-blue) |
| `Views/LoginView.xaml` | hero background → `LoginHeroBrush` (fixed navy) |
| `Views/SchoolSettingsView.xaml` | `#F8F8F8` → `SurfaceGrayBrush` |
| `Views/Controls/ReportCardSheetView.xaml` | (none — print stays) |
| `Views/Controls/MidTermSlipView.xaml` | (none — print stays) |
`Docs` | this guide |

*Produced from a direct audit of the current repo state (branch `sql_rec`).*
