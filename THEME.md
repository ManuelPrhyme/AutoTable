# AutoTable Design Theme

This document extracts the design theme from Resources/DesignTokens.xaml and summarizes the tokens, brushes, and styles available for use across the app. Use this as a reference when implementing UI components or adding new design tokens.

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

- BackgroundColor: #F5F5F5
- SurfaceWhiteColor: #FFFFFF
- SurfaceGrayColor: #F5F5F5
- SurfaceGray2Color: #EEEEEE
- TextPrimaryColor: #2D2D2D
- TextSecondaryColor: #666666
- TextMutedColor: #9E9E9E
- OverlayColor: #66000000 (40% black)

Semantic accents (Light):

- BlueColor: #2196F3 (Primary)
- GreenColor: #4CAF50 (Success)
- OrangeColor: #FF9800 (Warning/Amber)
- RedColor: #F44336 (Danger)
- PurpleColor: #673AB7 (Accent)
- TealColor: #009688 (Finance)

Dividers / borders:

- DividerColor / BorderLightColor: #DDDDDD
- BorderMediumColor: #CCCCCC

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
- PrimaryBlueBrush / BlueBrush → BlueColor
- PrimaryBlueHoverBrush → BlueHoverColor
- AccentPurpleBrush → PurpleColor
- SuccessGreenBrush → GreenColor
- DangerRedBrush → RedColor
- InfoBlueBrush → BlueColor
- SurfaceWhiteBrush / SurfaceGrayBrush / SurfaceGray2Brush
- BorderLightBrush / BorderMediumBrush

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
