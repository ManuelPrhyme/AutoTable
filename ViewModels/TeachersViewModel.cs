using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class TeachersViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _statusMessage = string.Empty;

        public ObservableCollection<Teacher> Teachers { get; } = new();

        public TeachersViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            try
            {
                Teachers.Clear();
                var list = await _dataService.GetTeachersAsync();
                foreach (var t in list) Teachers.Add(t);
                StatusMessage = $"Loaded {Teachers.Count} teacher(s).";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to load teachers: " + ex.Message;
            }
        }

        /// <summary>Persists a new teacher (built by the Add Teacher dialog) and inserts it at the top of the list.</summary>
        public async Task AddTeacherAsync(Teacher teacher)
        {
            var created = await _dataService.CreateTeacherAsync(teacher);
            Teachers.Insert(0, created);
            StatusMessage = $"Added teacher '{created.FullName}'.";
        }

        [RelayCommand]
        private async Task DeleteTeacherAsync(int teacherId)
        {
            try
            {
                await _dataService.DeleteTeacherAsync(teacherId);
                var existing = System.Linq.Enumerable.FirstOrDefault(Teachers, x => x.Id == teacherId);
                if (existing != null) Teachers.Remove(existing);
                StatusMessage = "Teacher removed.";
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Failed to remove teacher: " + ex.Message;
            }
        }

        /// <summary>Updates an existing teacher and refreshes the list item in place.</summary>
        public async Task UpdateTeacherAsync(Teacher teacher)
        {
            var updated = await _dataService.UpdateTeacherAsync(teacher);
            if (updated != null)
            {
                var index = System.Linq.Enumerable.ToList(Teachers).FindIndex(x => x.Id == updated.Id);
                if (index >= 0) Teachers[index] = updated;
                StatusMessage = $"Updated teacher '{updated.FullName}'.";
            }
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadAsync();
    }
}