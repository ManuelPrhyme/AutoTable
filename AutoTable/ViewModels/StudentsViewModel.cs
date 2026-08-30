using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class StudentsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        public ObservableCollection<Student> Students { get; } = new();
        public ObservableCollection<Student> FilteredStudents { get; } = new();

        // Filter lookup collections
        public ObservableCollection<SimpleLookup> FilterClasses { get; } = new();
        public ObservableCollection<SimpleLookup> FilterStreams { get; } = new();
        public ObservableCollection<int> FilterTerminationYears { get; } = new();

        // Class + stream lookup collections for the "shift enrollment" editor.
        public ObservableCollection<SimpleLookup> Classes { get; } = new();
        public ObservableCollection<SimpleLookup> Streams { get; } = new();

        [ObservableProperty]
        private string filter = string.Empty;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private SimpleLookup? classFilter;

        [ObservableProperty]
        private SimpleLookup? streamFilter;

        [ObservableProperty]
        private string statusFilter = "All";

        [ObservableProperty]
        private int? terminationYearFilter;

        public StudentsViewModel()
        {
            _dataService = AppServices.DataService ?? throw new InvalidOperationException("DataService not configured.");
        }

        public async Task LoadAsync()
        {
            try
            {
                var list = await _dataService.GetStudentsAsync();
                Students.Clear();
                foreach (var s in list) Students.Add(s);

                // Build filter lookups
                await PopulateFilterLookupsAsync();

                // Apply filters and sorting
                ApplyFilters();
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to load students: " + ex.Message;
            }
        }

        /// <summary>
        /// Populates filter dropdown options (classes, streams, termination years)
        /// from the current student data.
        /// </summary>
        private async Task PopulateFilterLookupsAsync()
        {
            // Class filter options from DB
            var classList = await _dataService.GetClassesAsync();
            FilterClasses.Clear();
            FilterClasses.Add(new SimpleLookup { Id = -1, Name = "All Classes" });
            foreach (var c in classList) FilterClasses.Add(c);

            // Stream filter options from DB
            var streamList = await _dataService.GetAllStreamsAsync();
            FilterStreams.Clear();
            FilterStreams.Add(new SimpleLookup { Id = -1, Name = "All Streams" });
            foreach (var s in streamList) FilterStreams.Add(s);

            // Termination year options from student data
            var years = Students
                .Where(s => s.TerminationYear.HasValue)
                .Select(s => s.TerminationYear!.Value)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();
            FilterTerminationYears.Clear();
            foreach (var y in years) FilterTerminationYears.Add(y);
        }

        /// <summary>
        /// Filters and sorts the student list: active students first,
        /// then applies class, stream, status, search, and termination year filters.
        /// </summary>
        public void ApplyFilters()
        {
            var query = Students.AsEnumerable();

            // 1. Sort: Active students at the top
            query = query.OrderBy(s => s.IsActive ? 0 : 1)
                         .ThenBy(s => s.FullName);

            // 2. Class filter
            if (ClassFilter != null && ClassFilter.Id > 0)
            {
                query = query.Where(s => s.ClassId == ClassFilter.Id);
            }

            // 3. Stream filter
            if (StreamFilter != null && StreamFilter.Id > 0)
            {
                query = query.Where(s => s.StreamId == StreamFilter.Id);
            }

            // 4. Status filter
            if (StatusFilter == "Active")
                query = query.Where(s => s.IsActive);
            else if (StatusFilter == "Inactive")
                query = query.Where(s => !s.IsActive);

            // 5. Termination year filter (only when Inactive is selected and a year is chosen)
            if (StatusFilter == "Inactive" && TerminationYearFilter.HasValue)
            {
                query = query.Where(s => s.TerminationYear == TerminationYearFilter.Value);
            }

            // 6. Search text filter
            if (!string.IsNullOrWhiteSpace(Filter))
            {
                var search = Filter.Trim();
                query = query.Where(s =>
                    (s.FullName != null && s.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (s.LIN != null && s.LIN.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            var filtered = query.ToList();

            FilteredStudents.Clear();
            foreach (var s in filtered) FilteredStudents.Add(s);

            // Update status message with active count
            var activeCount = Students.Count(s => s.IsActive);
            var totalCount = Students.Count;
            StatusMessage = Students.Count == 0
                ? "No students yet — use \"Add Student\" (or enroll via the Enrollment page) to create the first record."
                : filtered.Count == totalCount
                    ? $"Showing {activeCount} active of {totalCount} student(s)."
                    : $"Showing {filtered.Count} of {totalCount} student(s) ({activeCount} active).";
        }

        public async Task AddStudentAsync()
        {
            var student = new Student
            {
                LIN = $"LIN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                FullName = "New Student",
            };
            var created = await _dataService.CreateStudentAsync(student);
            Students.Add(created);
            ApplyFilters();
        }

        /// <summary>Loads the class + stream options used by the shift-enrollment editor.</summary>
        public async Task LoadClassAndStreamOptionsAsync()
        {
            var classes = await _dataService.GetClassesAsync();
            Classes.Clear();
            foreach (var c in classes) Classes.Add(c);

            var streams = await _dataService.GetAllStreamsAsync();
            Streams.Clear();
            foreach (var st in streams) Streams.Add(st);
        }

        /// <summary>
        /// Shifts a student's enrollment: reassigns the class and/or stream via the data
        /// service, then updates the in-memory row so the list reflects the change immediately.
        /// </summary>
        public async Task ShiftEnrollmentAsync(Student student, int classId, int? streamId)
        {
            if (student == null) return;

            student.ClassId = classId;
            student.StreamId = streamId;
            await _dataService.UpdateStudentAsync(student);

            // Refresh the display names from the loaded lookups.
            student.ClassName = Classes.FirstOrDefault(c => c.Id == classId)?.Name ?? student.ClassName;
            student.StreamName = streamId.HasValue
                ? Streams.FirstOrDefault(st => st.Id == streamId.Value)?.Name
                : student.StreamName;

            ApplyFilters();
        }

        partial void OnFilterChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnClassFilterChanged(SimpleLookup? value)
        {
            ApplyFilters();
        }

        partial void OnStreamFilterChanged(SimpleLookup? value)
        {
            ApplyFilters();
        }

        partial void OnStatusFilterChanged(string value)
        {
            // Clear termination year filter when switching away from Inactive
            if (value != "Inactive")
            {
                TerminationYearFilter = null;
            }
            ApplyFilters();
        }

        partial void OnTerminationYearFilterChanged(int? value)
        {
            ApplyFilters();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadAsync();
        }

        [RelayCommand]
        private async Task TerminateStudentAsync(int studentId)
        {
            await _dataService.TerminateStudentAsync(studentId, StudentTerminationReason.Expelled, DateTime.UtcNow, anonymize: false);
            await LoadAsync();
        }
    }
}
