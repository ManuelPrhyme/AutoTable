using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class ClassesViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        public ObservableCollection<SimpleLookup> Classes { get; } = new();
        public ObservableCollection<SimpleLookup> AllSubjects { get; } = new();
        public ObservableCollection<SimpleLookup> SubjectsForClass { get; } = new();

        public ClassesViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
        }

        public async Task LoadAsync()
        {
            var classes = await _dataService.GetClassesAsync();
            Classes.Clear();
            foreach (var c in classes) Classes.Add(new SimpleLookup { Id = c.Id, Name = c.Name });

            var subs = await _dataService.GetSubjectsAsync();
            AllSubjects.Clear();
            foreach (var s in subs) AllSubjects.Add(new SimpleLookup { Id = s.Id, Name = s.Name });
        }

        public async Task CreateClassAsync(string name)
        {
            await _dataService.CreateClassAsync(name);
        }

        public async Task CreateSubjectAsync(string name)
        {
            await _dataService.CreateSubjectAsync(name);
        }

        public async Task LoadSubjectsForClassAsync(int classId)
        {
            var subs = await _dataService.GetSubjectsForClassAsync(classId);
            SubjectsForClass.Clear();
            foreach (var s in subs) SubjectsForClass.Add(new SimpleLookup { Id = s.Id, Name = s.Name });
        }

        public async Task AssignSubjectToClassAsync(int classId, int subjectId)
        {
            await _dataService.AssignSubjectToClassAsync(classId, subjectId);
        }

        public async Task RemoveSubjectFromClassAsync(int classId, int subjectId)
        {
            await _dataService.RemoveSubjectFromClassAsync(classId, subjectId);
        }
    }
}
