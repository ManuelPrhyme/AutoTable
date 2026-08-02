using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Models
{
    public class NavMenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = "\uE80F";
        public string Tag { get; set; } = string.Empty;
        public UserRole MinimumRole { get; set; } = UserRole.DataEntrant;
        public NavigationViewItemSeparator? SeparatorBefore { get; set; }
    }
}
