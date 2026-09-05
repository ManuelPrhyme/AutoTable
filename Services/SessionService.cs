using AutoTable.Models;

namespace AutoTable.Services
{
    public class SessionService
    {
        private static SessionService? _instance;
        public static SessionService Instance => _instance ??= new SessionService();

        public User? CurrentUser { get; private set; }

        public bool IsAuthenticated => CurrentUser != null;

        /// <summary>
        /// Set by ShellView on login when the database has zero classes.
        /// Consumed by ClassesView to auto-open the first-launch setup
        /// sequence: grading-system modal (skipped when at least one grading
        /// system already exists) → class-creation modal → class teacher
        /// assignment. Reset once consumed so it only fires on first launch
        /// (or whenever the app opens with an empty school) — not every time
        /// the user visits the page.
        /// </summary>
        public bool ShouldAutoOpenGradingSystemCreation { get; set; }

        /// <summary>
        /// Set when the "No teachers available" prompt in the class-creation flow
        /// is answered with "Add Teacher". Consumed by TeachersView to auto-open
        /// the create-teacher modal once, then reset.
        /// </summary>
        public bool ShouldAutoOpenTeacherCreation { get; set; }

        /// <summary>
        /// Set when the first-launch setup flow finishes creating a class
        /// (and its grading system exists) so the next setup step — term
        /// creation — opens automatically. Consumed by TermManagementView to
        /// auto-open the create-term modal once, then reset.
        /// </summary>
        public bool ShouldAutoOpenTermCreation { get; set; }

        public void SetUser(User user) => CurrentUser = user;

        public void SignOut() => CurrentUser = null;

        public bool IsAdministrator => CurrentUser?.Role == UserRole.Administrator;
    }
}
