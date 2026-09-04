using AutoTable.Models;
using AutoTable.Services;
using AutoTable.Views;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoTable.Controls
{
    /// <summary>
    /// A guided-tour sidebar panel that highlights a specific UI element
    /// with a rounded-rect spotlight and shows a right-side panel with
    /// step info, navigation, and progress. Supports navigating between
    /// pages to highlight features directly.
    /// </summary>
    public sealed partial class UserTourOverlay : UserControl
    {
        private IReadOnlyList<TourStep> _steps = Array.Empty<TourStep>();
        private int _currentIndex;
        private ShellView? _shell;
        private string? _lastNavigatedTag;

        /// <summary>
        /// Raised when the user finishes or skips the entire tour.
        /// </summary>
        public event Action? TourFinished;

        public UserTourOverlay()
        {
            InitializeComponent();
        }

        // ────────────────────────────────────────────────────────────
        //  PUBLIC API
        // ────────────────────────────────────────────────────────────

        public void StartTour(ShellView shell)
        {
            _shell = shell;
            _steps = UserTourService.Instance.GetTourSteps();
            _currentIndex = 0;
            _lastNavigatedTag = null;

            if (_steps.Count == 0) return;

            Visibility = Visibility.Visible;
            IsHitTestVisible = true;
            ShowStep(_currentIndex);
        }

        // ────────────────────────────────────────────────────────────
        //  STEP NAVIGATION
        // ────────────────────────────────────────────────────────────

        private async void ShowStep(int index)
        {
            if (index < 0 || index >= _steps.Count) return;

            var step = _steps[index];
            _currentIndex = index;

            // Update text content
            StepTitle.Text = step.Title;
            StepDescription.Text = step.Description;
            StepIcon.Glyph = step.IconGlyph;
            StepCounter.Text = $"Step {index + 1} of {_steps.Count}";
            ProgressText.Text = $"{index + 1} / {_steps.Count}";

            // Update button labels for last step
            if (index == _steps.Count - 1)
            {
                NextBtnText.Text = "Finish";
                NextBtnIcon.Glyph = "\uE73E"; // Checkmark
                NextBtn.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80)); // Green
            }
            else
            {
                NextBtnText.Text = "Next";
                NextBtnIcon.Glyph = "\uE72A"; // Arrow right
                NextBtn.Background = (Brush)Application.Current.Resources["PrimaryBlueBrush"];
            }

            // Build progress dots
            BuildProgressDots(index);

            // Navigate to the target page if needed
            if (!string.IsNullOrEmpty(step.NavigateTo) && step.NavigateTo != _lastNavigatedTag && _shell != null)
            {
                await _shell.NavigateToPageForTourAsync(step.NavigateTo);
                _lastNavigatedTag = step.NavigateTo;
                // Extra wait for the page to fully render
                await Task.Delay(250);
            }

            // Position spotlight and dimming on the target element
            LayoutSpotlight(step);

            // Show the dimming scrim + sidebar
            DimmingScrim.Visibility = Visibility.Visible;
            TourSidebar.Visibility = Visibility.Visible;

            // Entrance animation
            AnimateSidebarEntrance();
        }

        private void BuildProgressDots(int activeIndex)
        {
            ProgressDots.Children.Clear();
            for (int i = 0; i < _steps.Count; i++)
            {
                var dot = new Microsoft.UI.Xaml.Shapes.Rectangle
                {
                    Width = i == activeIndex ? 14 : 6,
                    Height = 6,
                    RadiusX = 3,
                    RadiusY = 3,
                    Fill = i == activeIndex
                        ? (Brush)Application.Current.Resources["PrimaryBlueBrush"]
                        : (Brush)Application.Current.Resources["BorderMediumBrush"],
                    Margin = new Thickness(1, 0, 1, 0)
                };
                ProgressDots.Children.Add(dot);
            }
        }

        // ────────────────────────────────────────────────────────────
        //  LAYOUT: spotlight positioning + dimming
        // ────────────────────────────────────────────────────────────

        private void LayoutSpotlight(TourStep step)
        {
            // Reset spotlight visibility
            SpotlightBorder.Visibility = Visibility.Collapsed;
            SpotlightGlow.Visibility = Visibility.Collapsed;

            double containerW = TourRoot.ActualWidth > 0 ? TourRoot.ActualWidth : 1200;
            double containerH = TourRoot.ActualHeight > 0 ? TourRoot.ActualHeight : 800;

            // Find the target element
            FrameworkElement? target = FindElementGlobal(step.TargetElementName);

            if (target == null || target.ActualWidth < 1 || target.ActualHeight < 1)
            {
                // No target found — no spotlight, just show sidebar
                return;
            }

            // Get target bounds relative to overlay
            var transform = target.TransformToVisual(TourRoot);
            if (transform == null) return;

            var targetPt = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            double tX = targetPt.X;
            double tY = targetPt.Y;
            double tW = target.ActualWidth;
            double tH = target.ActualHeight;

            // If target is off-screen, skip spotlight
            if (tX + tW < 0 || tY + tH < 0 || tX > containerW || tY > containerH)
                return;

            // Position spotlight with padding
            double pad = 6;
            double glowPad = 10;

            // Outer glow
            Canvas.SetLeft(SpotlightGlow, tX - glowPad);
            Canvas.SetTop(SpotlightGlow, tY - glowPad);
            SpotlightGlow.Width = tW + glowPad * 2;
            SpotlightGlow.Height = tH + glowPad * 2;
            SpotlightGlow.Visibility = Visibility.Visible;

            // Inner spotlight
            Canvas.SetLeft(SpotlightBorder, tX - pad);
            Canvas.SetTop(SpotlightBorder, tY - pad);
            SpotlightBorder.Width = tW + pad * 2;
            SpotlightBorder.Height = tH + pad * 2;
            SpotlightBorder.Visibility = Visibility.Visible;
        }

        // ────────────────────────────────────────────────────────────
        //  ANIMATION
        // ────────────────────────────────────────────────────────────

        private async void AnimateSidebarEntrance()
        {
            TourSidebar.Opacity = 0;
            TourSidebar.Translation = new Vector3(40, 0, 0);

            // Fade in the scrim
            DimmingScrim.Opacity = 0;

            await Task.Delay(20);

            try
            {
                var sidebarVisual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview
                    .GetElementVisual(TourSidebar);
                var scrimVisual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview
                    .GetElementVisual(DimmingScrim);
                var compositor = sidebarVisual.Compositor;

                if (compositor != null)
                {
                    // Sidebar slide + fade
                    var sidebarOpacity = compositor.CreateScalarKeyFrameAnimation();
                    sidebarOpacity.InsertKeyFrame(1f, 1f);
                    sidebarOpacity.Duration = TimeSpan.FromMilliseconds(250);
                    sidebarVisual.StartAnimation("Opacity", sidebarOpacity);

                    var slideAnim = compositor.CreateVector3KeyFrameAnimation();
                    slideAnim.InsertKeyFrame(1f, Vector3.Zero);
                    slideAnim.Duration = TimeSpan.FromMilliseconds(250);
                    sidebarVisual.StartAnimation("Translation", slideAnim);

                    // Scrim fade in
                    var scrimOpacity = compositor.CreateScalarKeyFrameAnimation();
                    scrimOpacity.InsertKeyFrame(1f, 1f);
                    scrimOpacity.Duration = TimeSpan.FromMilliseconds(300);
                    scrimVisual.StartAnimation("Opacity", scrimOpacity);

                    return;
                }
            }
            catch { }

            // Fallback: instant show
            TourSidebar.Opacity = 1;
            TourSidebar.Translation = Vector3.Zero;
            DimmingScrim.Opacity = 1;
        }

        // ────────────────────────────────────────────────────────────
        //  EVENT HANDLERS
        // ────────────────────────────────────────────────────────────

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _steps.Count - 1)
                ShowStep(_currentIndex + 1);
            else
                EndTour();
        }

        private void SkipBtn_Click(object sender, RoutedEventArgs e) => EndTour();
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => EndTour();

        private void EndTour()
        {
            UserTourService.Instance.CompleteTour();
            HideTour();
            TourFinished?.Invoke();
        }

        private void HideTour()
        {
            Visibility = Visibility.Collapsed;
            IsHitTestVisible = false;
            DimmingScrim.Visibility = Visibility.Collapsed;
            SpotlightBorder.Visibility = Visibility.Collapsed;
            SpotlightGlow.Visibility = Visibility.Collapsed;
            TourSidebar.Visibility = Visibility.Collapsed;
        }

        // ────────────────────────────────────────────────────────────
        //  HELPERS
        // ────────────────────────────────────────────────────────────

        private FrameworkElement? FindElementGlobal(string name)
        {
            if (_shell == null) return null;

            // 1. Search in the ShellView (sidebar, header, term selector, etc.)
            var found = FindElementByName(_shell, name) as FrameworkElement;
            if (found != null) return found;

            // 2. Search in the ContentFrame's loaded page
            var frame = _shell.GetContentFrame();
            if (frame?.Content is FrameworkElement pageContent)
            {
                found = FindElementByName(pageContent, name) as FrameworkElement;
                if (found != null) return found;
            }

            return null;
        }

        private static DependencyObject? FindElementByName(DependencyObject root, string name)
        {
            if (root is FrameworkElement fe && fe.Name == name)
                return root;

            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
                var found = FindElementByName(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
