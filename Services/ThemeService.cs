using Microsoft.UI.Xaml;

namespace AutoTable.Services
{
    public static class ThemeService
    {
        private static Window? _mainWindow;

        /// <summary>
        /// Register the main window so theme changes propagate to the live UI.
        /// In WinUI 3, Application.RequestedTheme only affects new windows;
        /// the root element of an open window must also be updated for
        /// {ThemeResource} lookups to refresh immediately.
        /// </summary>
        public static void Initialize(Window window)
        {
            _mainWindow = window;
        }

        public static void SetTheme(ApplicationTheme theme)
        {
            try
            {
                if (Application.Current is Application app)
                {
                    // Default theme for any new windows
                    app.RequestedTheme = theme;

                    // Instant theme change for the visible window
                    if (_mainWindow?.Content is FrameworkElement rootElement)
                    {
                        rootElement.RequestedTheme = theme == ApplicationTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
                    }
                }
            }
            catch { }
        }

        public static void ToggleTheme()
        {
            try
            {
                if (Application.Current is Application app)
                {
                    var next = app.RequestedTheme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;
                    SetTheme(next);
                }
            }
            catch { }
        }
    }
}