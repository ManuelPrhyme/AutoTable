# AutoTable Design Theme

This document defines a complete semantic design-token contract for AutoTable with **true Light and Dark modes**. Every page surface, text role, border, control, interactive state, overlay, status, and feature component must use theme-aware resources so the entire application switches correctly at runtime.

Use `{ThemeResource ...}` for all values that must respond to theme switching. Do not hard-code UI colors in feature/page XAML.

## Overview
- Theme is organized with ThemeDictionaries (Light and Dark). Use `{ThemeResource ...}` in XAML for resources that should respond to theme switching at runtime.
- Some styles rely on a base style (use `{StaticResource ...}`) where a Style object is required (e.g. `BasedOn`).

## How to switch theme programmatically
The repo includes `AutoTable.Services.ThemeService` with:

```csharp
// Set explicit theme
ThemeService.SetTheme(ApplicationTheme.Dark);
// Toggle
ThemeService.ToggleTheme();
```

## Spacing & Layout
- Grid unit: 8px baseline
- Space keys: Space1=4, Space2=8, Space3=12, Space4=16, Space5=20, Space6=24, Space8=32
- Common Thickness: CardPadding = 16, PagePadding = 24, RowPadding = 16,10

## Shapes
- CardCornerRadiusValue = 10
- BadgeCornerRadiusValue = 6
- ButtonCornerRadiusValue = 8

## Typography (named styles)
- PageTitleStyle: 20px, Semibold, Segoe UI
- SectionTitleStyle: 16px, Semibold
- MutedTextStyle: 13px, muted
- BodyTextStyle: 14px
- KpiValueStyle: 28px, Bold
- TableHeaderStyle: 13px, Semibold
- TableCellStyle / NumericCellStyle: 14px

## Theme Tokens (Light)
Colors (Light theme):

### Core surfaces


- BackgroundColor: #F5F5F5
- SurfaceWhiteColor: #FFFFFF
- SurfaceGrayColor: #F5F5F5
- SurfaceGray2Color: #EEEEEE
- SurfaceElevatedColor: #FFFFFF
- SurfaceSunkenColor: #E9E9E9
- SurfaceDisabledColor: #F0F0F0
- TextPrimaryColor: #2D2D2D
- TextSecondaryColor: #666666
- TextMutedColor: #9E9E9E
- TextDisabledColor: #BDBDBD
- TextOnAccentColor: #FFFFFF
- OverlayColor: #66000000 (40% black)

Semantic accents (Light):

- BlueColor: #2196F3 (Primary)
- BlueHoverColor: #1976D2
- BluePressedColor: #1565C0
- BlueSubtleColor: #E3F2FD
- GreenColor: #4CAF50 (Success)
- GreenHoverColor: #388E3C
- GreenSubtleColor: #E8F5E9
- OrangeColor: #FF9800 (Warning/Amber)
- OrangeHoverColor: #F57C00
- OrangeSubtleColor: #FFF3E0
- RedColor: #F44336 (Danger)
- RedHoverColor: #D32F2F
- RedSubtleColor: #FFEBEE
- PurpleColor: #673AB7 (Accent)
- PurpleHoverColor: #512DA8
- PurpleSubtleColor: #EDE7F6
- TealColor: #009688 (Finance)
- TealHoverColor: #00796B
- TealSubtleColor: #E0F2F1

### Interactive states

- HoverColor: #F0F0F0
- PressedColor: #E5E5E5
- SelectedColor: #E3F2FD
- SelectedHoverColor: #D7EBFC
- DisabledColor: #F0F0F0
- DisabledBorderColor: #D6D6D6

Dividers / borders:

- DividerColor / BorderLightColor: #DDDDDD
- BorderMediumColor: #CCCCCC
- BorderStrongColor: #AFAFAF
- FocusBorderColor: #1976D2

## Theme Tokens (Dark)
Colors (Dark theme):

- BackgroundColor: #1E1E1E
- SurfaceWhiteColor: #2D2D2D (card surface in dark)
- SurfaceGrayColor: #1E1E1E
- SurfaceGray2Color: #2D2D2D
- TextPrimaryColor: #E0E0E0
- TextSecondaryColor: #A0A0A0
- DividerColor / BorderLightColor: #333333
- BorderMediumColor: #444444
- OverlayColor: #66000000

Accent palette is shared (same hex values as Light theme for consistent highlights).

## Brushes (semantic aliases)

Define every alias in **both Light and Dark ThemeDictionaries**.

- BackgroundBrush
- SurfaceWhiteBrush / SurfaceGrayBrush / SurfaceGray2Brush
- SurfaceElevatedBrush / SurfaceSunkenBrush / SurfaceDisabledBrush
- TextPrimaryBrush / TextSecondaryBrush / TextMutedBrush / TextDisabledBrush
- TextOnAccentBrush
- DividerBrush / BorderLightBrush / BorderMediumBrush / BorderStrongBrush
- FocusBorderBrush
- PrimaryBlueBrush / PrimaryBlueHoverBrush / PrimaryBluePressedBrush
- AccentPurpleBrush / PurpleHoverBrush
- SuccessGreenBrush / SuccessGreenHoverBrush
- WarningOrangeBrush / WarningOrangeHoverBrush
- DangerRedBrush / DangerRedHoverBrush
- FinanceTealBrush / TealHoverBrush
- HoverBrush / PressedBrush / SelectedBrush / SelectedHoverBrush
- DisabledBrush / DisabledBorderBrush
- OverlayBrush / ScrimBrush / CardShadowBrush

Legacy aliases remain valid:
- BlueBrush → PrimaryBlueBrush
- GreenBrush → SuccessGreenBrush
- RedBrush → DangerRedBrush
- InfoBlueBrush → PrimaryBlueBrush

Use `{ThemeResource TextPrimaryBrush}` when text should follow theme.

## Component Styles (high level)

- CardBorderStyle (Border)
  - Background: SurfaceWhiteBrush
  - Border: BorderLightBrush (1px)
  - CornerRadius: CardCornerRadiusValue
  - Padding: CardPadding

- KpiCardStyle (Border)
  - Left accent bar pattern, minWidth/minHeight

- TableCardStyle (Border)
  - Table container card

- FilterBarStyle (Border)
  - Background: SurfaceWhiteBrush, border, corner radius

- TableHeaderRowStyle (Border)
  - Background: SurfaceGray2Brush, BorderMediumBrush for divider

- Button Styles
  - PrimaryButtonStyle: accent background (BlueBrush), white text, padding, corner radius
  - SecondaryButtonStyle: SurfaceWhiteBrush background, BorderMediumBrush border
  - GhostButtonStyle: transparent
  - DangerButtonStyle: RedBrush background
  - IconButtonStyle: minimal padding

- ComboBoxStyle: SurfaceWhiteBrush background, BorderLightBrush border
- SearchBoxStyle: SurfaceWhiteBrush background, BorderLightBrush, PlaceholderForeground = TextSecondaryBrush
- CalendarStyle: SurfaceWhiteBrush background, BorderLightBrush

- Modal/Dialog
  - ModalDialogStyle: SurfaceWhiteBrush background, BorderLightBrush, CornerRadius=CardCornerRadiusValue
  - DialogCloseButtonStyle: ghost small button (36x36)
  - DialogPrimaryButtonStyle: BasedOn PrimaryButtonStyle

## Theme-aware states and controls

All controls must have Light and Dark resources for normal, hover, pressed, focused, selected, disabled, and validation states.

### Inputs
- **TextBoxStyle:** SurfaceWhiteBrush background, TextPrimaryBrush foreground, BorderLightBrush border, BorderMediumBrush hover border, FocusBorderBrush focus border, TextSecondaryBrush placeholder, SurfaceDisabledBrush/TextDisabledBrush disabled state.
- **ComboBoxStyle:** same semantic treatment as TextBox; dropdown surface uses SurfaceElevatedBrush.
- **SearchBoxStyle:** same as TextBox with TextSecondaryBrush placeholder.
- **CalendarStyle:** SurfaceWhiteBrush background, TextPrimaryBrush foreground, HoverBrush date hover, PrimaryBlueBrush selected date, TextDisabledBrush disabled dates.
- **CheckBoxStyle:** BorderMediumBrush unchecked border, PrimaryBlueBrush checked fill, TextOnAccentBrush glyph, FocusBorderBrush focus, DisabledBrush/TextDisabledBrush disabled.
- **RadioButtonStyle:** BorderMediumBrush outline, PrimaryBlueBrush selected state, TextDisabledBrush disabled.
- **ToggleSwitchStyle:** BorderMediumBrush off track, PrimaryBlueBrush on track, theme-aware thumb and disabled state.
- **SliderStyle:** BorderMediumBrush track, PrimaryBlueBrush active track/thumb, DisabledBrush disabled state.
- **ProgressBarStyle:** SurfaceGray2Brush track with PrimaryBlueBrush, SuccessGreenBrush, WarningOrangeBrush, or DangerRedBrush progress.

### Navigation
- **NavigationPaneStyle:** Background/SurfaceWhiteBrush and TextPrimaryBrush.
- **NavigationItemStyle:** TextSecondaryBrush normal, HoverBrush hover, SelectedBrush selected background, PrimaryBlueBrush selected foreground/indicator, TextDisabledBrush disabled.

### Tables
- **TableHeaderRowStyle:** SurfaceGray2Brush background, TextPrimaryBrush foreground, BorderMediumBrush divider.
- **TableRowStyle:** SurfaceWhiteBrush background, TextPrimaryBrush foreground, DividerBrush separator.
- **TableRowHoverStyle:** HoverBrush background.
- **TableRowSelectedStyle:** SelectedBrush background with TextPrimaryBrush foreground.
- Empty, loading, error, and disabled rows must use semantic text/state brushes.

### Menus, flyouts, tooltips
- **MenuFlyoutStyle:** SurfaceElevatedBrush background, BorderLightBrush border, TextPrimaryBrush foreground.
- **MenuItemStyle:** HoverBrush/PressedBrush states and TextDisabledBrush disabled state.
- **TooltipStyle:** SurfaceElevatedBrush background, TextPrimaryBrush foreground, BorderLightBrush border.

### Dialogs and overlays
- **ModalDialogStyle:** SurfaceElevatedBrush background, TextPrimaryBrush foreground, BorderLightBrush border.
- **DialogCloseButtonStyle:** transparent normal state with TextSecondaryBrush, HoverBrush and PressedBrush states.
- **DialogPrimaryButtonStyle:** BasedOn PrimaryButtonStyle.
- **DialogSecondaryButtonStyle:** BasedOn SecondaryButtonStyle.
- **Overlay/Scrim:** use ScrimBrush; never hard-code black.

### Status, badges and validation
- **Info:** BlueSubtleBrush + PrimaryBlueBrush.
- **Success:** GreenSubtleBrush + SuccessGreenBrush.
- **Warning:** OrangeSubtleBrush + WarningOrangeBrush.
- **Error:** RedSubtleBrush + DangerRedBrush.
- Validation text must use semantic danger/success brushes and must not depend on fixed Light-mode colors.
- Badges should use semantic status backgrounds/foregrounds appropriate to the active theme.

## Theme-Safe Feature Rules

1. No hard-coded UI colors such as `#FFFFFF`, `#000000`, `#F5F5F5`, or `#1E1E1E` in feature/page XAML.
2. Page backgrounds use `BackgroundBrush`.
3. Cards/panels use `SurfaceWhiteBrush` or `SurfaceElevatedBrush`.
4. Text uses TextPrimaryBrush, TextSecondaryBrush, TextMutedBrush, or TextDisabledBrush.
5. Every interactive state is theme-aware.
6. Inputs, dropdowns, tables, navigation, dialogs, flyouts, tooltips, overlays, and status controls all have Light/Dark values.
7. Icons should use semantic foreground brushes.
8. Dark mode must not simply invert or dim Light mode.
9. Focus indicators must remain visible in both themes.
10. Empty, loading, error, validation, and disabled states must remain distinguishable in both themes.

## DesignTokens.xaml requirement

`DesignTokens.xaml` should define matching Light and Dark dictionaries with the same semantic keys:

```xaml
<ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
        <!-- Light colors and brushes -->
    </ResourceDictionary>

    <ResourceDictionary x:Key="Dark">
        <!-- Dark colors and brushes -->
    </ResourceDictionary>

    <ResourceDictionary x:Key="Default">
        <!-- Safe Light fallback with the same keys -->
    </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

Every color/brush consumed by a feature must exist in both Light and Dark dictionaries. This prevents silent fallback to Light-mode resources.

### Recommended usage

```xaml
<Grid Background="{ThemeResource BackgroundBrush}">
    <Border
        Background="{ThemeResource SurfaceWhiteBrush}"
        BorderBrush="{ThemeResource BorderLightBrush}">
        <StackPanel>
            <TextBlock
                Foreground="{ThemeResource TextPrimaryBrush}"
                Text="My Card" />
            <TextBlock
                Foreground="{ThemeResource TextSecondaryBrush}"
                Text="Body content" />
        </StackPanel>
    </Border>
</Grid>
```

For Style objects, use `StaticResource` when required by XAML, e.g. `Style="{StaticResource CardBorderStyle}"`.

## Shadows & Elevation
- A subtle CardShadowBrush exists (CardShadowColor #0A000000) as a placeholder. For true elevation use WinUI ThemeShadow or DropShadow and attach it to the card root.

## Usage guidance
- Prefer ThemeResource for brushes/colors in XAML so they react to theme changes, e.g.:

```xaml
<Border Background="{ThemeResource SurfaceWhiteBrush}" />
```

- Use StaticResource when a Style object is referenced (e.g. `BasedOn` in a Style) or when resolving at compile-time is required.

- For new tokens:
  - Add color entries inside Light / Dark ThemeDictionaries (semantic names first), then add corresponding SolidColorBrush aliases.
  - Update DesignTokens.xaml and reference via ThemeResource.

## Theme implementation checklist

- [ ] Page/application background switches.
- [ ] Cards, panels, KPI cards and tables switch.
- [ ] Primary, secondary, ghost, icon and danger buttons switch.
- [ ] Text hierarchy and icons switch.
- [ ] TextBox, ComboBox, SearchBox and Calendar switch.
- [ ] CheckBox, RadioButton, ToggleSwitch, Slider and ProgressBar switch.
- [ ] Navigation and selected navigation states switch.
- [ ] Menus, flyouts and tooltips switch.
- [ ] Modals, dialogs and overlays switch.
- [ ] Hover, pressed, selected, focused and disabled states switch.
- [ ] Info, success, warning and error states switch.
- [ ] Empty/loading/error/validation states switch.
- [ ] No accidental hard-coded feature colors remain.
- [ ] Theme switching works without restarting the application.

## Common keys reference (selected)

- Colors: OverlayColor, BlueColor, GreenColor, OrangeColor, RedColor, PurpleColor, TealColor
- Brushes: SurfaceWhiteBrush, SurfaceGrayBrush, SurfaceGray2Brush, PrimaryBlueBrush, AccentPurpleBrush, BorderLightBrush, BorderMediumBrush
- Spacing: CardPadding, PagePadding, RowPadding
- Radii: CardCornerRadiusValue, BadgeCornerRadiusValue, ButtonCornerRadiusValue
- Styles: PrimaryButtonStyle, SecondaryButtonStyle, GhostButtonStyle, KpiCardStyle, CardBorderStyle, TableCardStyle, TableHeaderRowStyle, ModalDialogStyle, DialogCloseButtonStyle

## Accessibility notes
- Typography uses dark text (#212121 / #2D2D2D) on light surfaces to meet contrast requirements.
- Reserve accent colors for calls-to-action and important indicators.

## Example: Creating a card using theme tokens

```xaml
<Border Style="{StaticResource CardBorderStyle}">
  <StackPanel>
	<TextBlock Style="{ThemeResource SectionTitleStyle}" Text="My Card" />
	<TextBlock Style="{ThemeResource BodyTextStyle}" Text="Body content" />
  </StackPanel>
</Border>
```

---
If you want, I can also export a JSON or YAML manifest of the theme tokens for design tools or a style dictionary. Which format do you prefer?
