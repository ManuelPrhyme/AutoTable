using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class StudentsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        public ObservableCollection<Student> Students { get; } = new();

        [ObservableProperty]
        private string filter = string.Empty;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        public StudentsViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
        }

        public async Task LoadAsync()
        {
            try
            {
                var list = await _dataService.GetStudentsAsync();
                Students.Clear();
                foreach (var s in list) Students.Add(s);
                StatusMessage = Students.Count == 0
                    ? "No students yet — use \"Add Student\" (or enroll via the Enrollment page) to create the first record."
                    : $"Showing {Students.Count} student(s) from the database.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to load students: " + ex.Message;
            }
        }

        public async Task AddStudentAsync()
        {
            var student = new Student
            {
                LIN = $"LIN-{System.Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                FullName = "New Student",
            };
            var created = await _dataService.CreateStudentAsync(student);
            Students.Add(created);
        }

        partial void OnFilterChanged(string value)
        {
            var lower = value?.ToLower() ?? "";
            // client-side filter: for brevity we'll just reload visible items
            var filtered = Students.Where(s => s.FullName.ToLower().Contains(lower) || s.LIN.ToLower().Contains(lower)).ToList();
            // In production use CollectionViewSource; here we simply do nothing extra
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadAsync();
        }

        [RelayCommand]
        private async Task TerminateStudentAsync(int studentId)
        {
            await _dataService.TerminateStudentAsync(studentId, StudentTerminationReason.Expelled, System.DateTime.UtcNow, anonymize: false);
            await LoadAsync();
        }
    }
}
