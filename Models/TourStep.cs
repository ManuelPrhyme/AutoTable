namespace AutoTable.Models
{
    /// <summary>
    /// Defines a single step in the user interface tour.
    /// </summary>
    public class TourStep
    {
        /// <summary>
        /// The element name (x:Name) of the UI element to highlight.
        /// This element will be brought to focus while the rest dims.
        /// Can be in the ShellView or in the currently loaded page (ContentFrame).
        /// </summary>
        public string TargetElementName { get; set; } = string.Empty;

        /// <summary>
        /// Optional navigation tag. When set, the tour will navigate
        /// to the specified page before highlighting the target element.
        /// Must match a key in ShellView.Routes (e.g. "Dashboard", "Assessments").
        /// </summary>
        public string? NavigateTo { get; set; }

        /// <summary>
        /// Title displayed in the tour popup header.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description/body text explaining the highlighted element.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional icon glyph (Segoe MDL2 Assets) shown in the popup.
        /// </summary>
        public string IconGlyph { get; set; } = "\uE80F";

        /// <summary>
        /// Where to position the popup relative to the target element.
        /// </summary>
        public TourPopupPosition PopupPosition { get; set; } = TourPopupPosition.Right;
    }

    public enum TourPopupPosition
    {
        Top,
        Bottom,
        Left,
        Right
    }
}
