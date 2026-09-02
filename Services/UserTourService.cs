using AutoTable.Models;
using System;
using System.Collections.Generic;

namespace AutoTable.Services
{
    /// <summary>
    /// Manages the guided tour of the application interface.
    /// Tracks progress, defines tour steps, and persists whether the
    /// tour has been completed so it only shows on first launch.
    /// Each step can navigate to a page and highlight a specific feature.
    /// </summary>
    public class UserTourService
    {
        private static UserTourService? _instance;
        public static UserTourService Instance => _instance ??= new UserTourService();

        /// <summary>
        /// Raised when the tour should begin (e.g. first launch).
        /// </summary>
        public event Action? TourRequested;

        /// <summary>
        /// Raised when the tour completes or is skipped.
        /// </summary>
        public event Action? TourCompleted;

        /// <summary>
        /// Whether the tour has been completed at least once.
        /// Persisted via local settings so it survives app restarts.
        /// </summary>
        public bool HasCompletedTour
        {
            get => GetSetting("UserTour_Completed", false);
            private set => SetSetting("UserTour_Completed", value);
        }

        /// <summary>
        /// Returns the ordered list of tour steps describing the
        /// main features of the AutoTable interface. Steps navigate
        /// to actual pages and spotlight real UI elements.
        /// </summary>
        public IReadOnlyList<TourStep> GetTourSteps()
        {
            return new List<TourStep>
            {
                // ── WELCOME ──────────────────────────────────────────
                new TourStep
                {
                    Title = "Welcome to AutoTable!",
                    Description = "This quick tour will walk you through the key features of your school management system. Tap anywhere or click Next to continue.",
                    IconGlyph = "\uE80F",
                    PopupPosition = TourPopupPosition.Right
                },

                // ── DASHBOARD ────────────────────────────────────────
                new TourStep
                {
                    NavigateTo = "Dashboard",
                    TargetElementName = "KpiPanel",
                    Title = "Dashboard — At a Glance",
                    Description = "Your home page shows KPI cards with total students, assessments, and fees. Everything you need to know at a glance.",
                    IconGlyph = "\uE80F",
                    PopupPosition = TourPopupPosition.Bottom
                },
                new TourStep
                {
                    NavigateTo = "Dashboard",
                    TargetElementName = "AssessmentProgressPanel",
                    Title = "Assessment Progress",
                    Description = "Track how each assessment's marks entry is progressing — see completion percentages and status at a glance.",
                    IconGlyph = "\uE9D5",
                    PopupPosition = TourPopupPosition.Right
                },
                new TourStep
                {
                    NavigateTo = "Dashboard",
                    TargetElementName = "QuickActionsList",
                    Title = "Quick Actions",
                    Description = "Jump directly to common tasks like creating assessments, entering marks, or adding students — all from the dashboard.",
                    IconGlyph = "\uE768",
                    PopupPosition = TourPopupPosition.Left
                },

                // ── ASSESSMENTS ──────────────────────────────────────
                new TourStep
                {
                    NavigateTo = "Assessments",
                    TargetElementName = "NewAssessmentBtn",
                    Title = "Create Assessments",
                    Description = "Click '+ New Assessment' to create tests, assignments, and exams. Set weightings, due dates, and link them to classes and subjects.",
                    IconGlyph = "\uE710",
                    PopupPosition = TourPopupPosition.Bottom
                },

                // ── MARKS ENTRY ──────────────────────────────────────
                new TourStep
                {
                    NavigateTo = "MarksEntry",
                    TargetElementName = "LoadMarksBtn",
                    Title = "Enter Student Marks",
                    Description = "Select a class and assessment, then click 'Load Marks' to enter scores for each student. Marks are saved as drafts until submitted.",
                    IconGlyph = "\uE8D3",
                    PopupPosition = TourPopupPosition.Left
                },

                // ── GRADEBOOK ────────────────────────────────────────
                new TourStep
                {
                    NavigateTo = "Gradebook",
                    TargetElementName = "ExportGradebookBtn",
                    Title = "Gradebook",
                    Description = "View consolidated marks, class averages, student ranks, and final grades. Export the gradebook to Excel with one click.",
                    IconGlyph = "\uE8F1",
                    PopupPosition = TourPopupPosition.Left
                },

                // ── TEACHERS ─────────────────────────────────────────
                new TourStep
                {
                    NavigateTo = "Teachers",
                    TargetElementName = "AddTeacherBtn",
                    Title = "Manage Teachers",
                    Description = "Register teachers with their qualifications, subjects, and next-of-kin details. Click '+ Add Teacher' to get started.",
                    IconGlyph = "\uE77B",
                    PopupPosition = TourPopupPosition.Bottom
                },

                // ── CLASSES ──────────────────────────────────────────
                new TourStep
                {
                    NavigateTo = "Classes",
                    TargetElementName = "CreateClassButton",
                    Title = "Set Up Classes",
                    Description = "Create classes, assign subjects, streams, and class teachers. Each class gets its own grading system and student roster.",
                    IconGlyph = "\uE8F1",
                    PopupPosition = TourPopupPosition.Bottom
                },
                new TourStep
                {
                    NavigateTo = "Classes",
                    TargetElementName = "CreateGradingSystemButton",
                    Title = "Grading Systems",
                    Description = "Define grading scales with grade bands (A, B, C...) and their score ranges. Set a default system that applies to new classes.",
                    IconGlyph = "\uE7C3",
                    PopupPosition = TourPopupPosition.Left
                },

                // ── TERM MANAGEMENT ──────────────────────────────────
                new TourStep
                {
                    NavigateTo = "TermManagement",
                    TargetElementName = "CreateTermCard",
                    Title = "Academic Terms",
                    Description = "Create terms (e.g. Term 1, 2026) with start and end dates. Set per-class term fees and activate the current term.",
                    IconGlyph = "\uE7BE",
                    PopupPosition = TourPopupPosition.Bottom
                },

                // ── REPORT CARDS ─────────────────────────────────────
                new TourStep
                {
                    NavigateTo = "ReportCards",
                    TargetElementName = "BtnMidTermSlips",
                    Title = "Report Cards",
                    Description = "Generate and print professional report cards. Filter by class and term, generate all at once, or print individual student reports.",
                    IconGlyph = "\uE8A5",
                    PopupPosition = TourPopupPosition.Bottom
                },

                // ── FEE COLLECTION ──────────────────────────────────
                new TourStep
                {
                    NavigateTo = "FeeCollection",
                    TargetElementName = "RecordPaymentBtn",
                    Title = "Fee Collection",
                    Description = "Track student fee payments, record new payments, and print receipt slips. KPIs show total expected, collected, and outstanding.",
                    IconGlyph = "\uE8C7",
                    PopupPosition = TourPopupPosition.Bottom
                },

                // ── AI INSIGHTS ──────────────────────────────────────
                new TourStep
                {
                    NavigateTo = "AiInsights",
                    TargetElementName = "RefreshInsightsBtn",
                    Title = "AI Insights",
                    Description = "Get AI-powered at-risk alerts, performance recommendations, and workflow automations. Click 'Refresh Insights' to update.",
                    IconGlyph = "\uE946",
                    PopupPosition = TourPopupPosition.Left
                },

                // ── SCHOOL SETTINGS ──────────────────────────────────
                new TourStep
                {
                    NavigateTo = "SchoolSettings",
                    TargetElementName = "LogoPreview",
                    Title = "School Settings",
                    Description = "Configure your school name, logo, motto, address, and head teacher. This info appears on report cards and receipts.",
                    IconGlyph = "\uE713",
                    PopupPosition = TourPopupPosition.Right
                },

                // ── GLOBAL HEADER ────────────────────────────────────
                new TourStep
                {
                    TargetElementName = "TermSelector",
                    Title = "Term Selector",
                    Description = "Switch between academic terms from the top bar. All data views update to show the selected term's information.",
                    IconGlyph = "\uE8EF",
                    PopupPosition = TourPopupPosition.Bottom
                },
                new TourStep
                {
                    TargetElementName = "TopSearchBox",
                    Title = "Global Search",
                    Description = "Quickly find students, classes, and teachers from anywhere in the app. Type a name and select a result to navigate directly.",
                    IconGlyph = "\uE721",
                    PopupPosition = TourPopupPosition.Bottom
                },
                new TourStep
                {
                    TargetElementName = "ThemeToggle",
                    Title = "Theme Toggle",
                    Description = "Switch between Light and Dark mode. Your preference is saved automatically.",
                    IconGlyph = "\uE771",
                    PopupPosition = TourPopupPosition.Bottom
                },

                // ── DONE ─────────────────────────────────────────────
                new TourStep
                {
                    Title = "You're All Set!",
                    Description = "That's the overview! You can restart this tour anytime from the 'Take Tour' button in the sidebar. Enjoy using AutoTable!",
                    IconGlyph = "\uE73E",
                    PopupPosition = TourPopupPosition.Right
                },
            };
        }

        /// <summary>
        /// Requests the tour to start. Called on first launch after login.
        /// </summary>
        public void RequestTour()
        {
            TourRequested?.Invoke();
        }

        /// <summary>
        /// Marks the tour as completed and fires the completed event.
        /// </summary>
        public void CompleteTour()
        {
            HasCompletedTour = true;
            TourCompleted?.Invoke();
        }

        /// <summary>
        /// Skips the tour without marking it as completed (so it can
        /// be shown again next launch). Use CompleteTour() to dismiss
        /// permanently.
        /// </summary>
        public void SkipTour()
        {
            HasCompletedTour = true;
            TourCompleted?.Invoke();
        }

        // ── Simple local-settings persistence ──────────────────────
        private static Windows.Storage.ApplicationDataContainer LocalSettings =>
            Windows.Storage.ApplicationData.Current.LocalSettings;

        private static bool GetSetting(string key, bool fallback)
        {
            try
            {
                if (LocalSettings.Values.ContainsKey(key) && LocalSettings.Values[key] is bool val)
                    return val;
            }
            catch { }
            return fallback;
        }

        private static void SetSetting(string key, bool value)
        {
            try
            {
                LocalSettings.Values[key] = value;
            }
            catch { }
        }
    }
}
