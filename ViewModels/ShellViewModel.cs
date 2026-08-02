using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace AutoTable.ViewModels
{
    public partial class ShellViewModel : BaseViewModel
    {
        [ObservableProperty] private string _pageTitle = "Marks Management Dashboard";
        [ObservableProperty] private string _pageSubtitle = "Overview of assessments, marks entry, and student performance.";

        public string UserDisplayName { get; }
        public string UserRoleLabel { get; }
        public string SelectedTerm { get; }
        public bool IsAdministrator => SessionService.Instance.IsAdministrator;

        public static IReadOnlyDictionary<string, (string Title, string Subtitle)> PageMetadata { get; } =
            new Dictionary<string, (string, string)>
            {
                ["Dashboard"] = ("Marks Management Dashboard", "Overview of assessments, marks entry, and student performance."),
                ["Assessments"] = ("Assessments", "Create, track, and manage class assessments and weightings."),
                ["MarksEntry"] = ("Marks Entry", "Enter and update student marks for selected assessments."),
                ["Gradebook"] = ("Gradebook", "View consolidated marks, averages, ranks, and grades by class."),
            };

        public ShellViewModel()
        {
            var user = SessionService.Instance.CurrentUser;
            UserDisplayName = user?.FullName ?? "User";
            UserRoleLabel = user?.Role == UserRole.Administrator ? "Administrator" : "Data Entrant";
            SelectedTerm = "Term 2, 2025";
        }

        public void SetPageMetadata(string tag)
        {
            if (PageMetadata.TryGetValue(tag, out var meta))
            {
                PageTitle = meta.Title;
                PageSubtitle = meta.Subtitle;
            }
        }
    }
}
