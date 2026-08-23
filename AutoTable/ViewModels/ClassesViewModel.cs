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
        public ObservableCollection<AutoTable.Models.ClassInfo> ClassInfos { get; } = new();
        public ObservableCollection<SimpleLookup> AllSubjects { get; } = new();
        public ObservableCollection<SimpleLookup> SubjectsForClass { get; } = new();
        public ObservableCollection<SimpleLookup> StreamsForClass { get; } = new();
        public ObservableCollection<SimpleLookup> AllStreams { get; } = new();

        public ClassesViewModel()
        {
            _dataService = AppServices.DataService ?? throw new System.InvalidOperationException("DataService not configured.");
        }

        public async Task LoadAsync()
        {
            var classes = await _dataService.GetClassesAsync();
            Classes.Clear();
            ClassInfos.Clear();

            var students = await _dataService.GetStudentsAsync();

            foreach (var c in classes) {
                Classes.Add(new SimpleLookup { Id = c.Id, Name = c.Name });
            }

            // Build ClassInfos with streams, subjects, student counts and class teacher
            var teacherNames = await _dataService.GetClassTeacherNamesAsync();
            foreach (var c in classes)
            {
                var streamsForClass = await _dataService.GetStreamsForClassAsync(c.Id);
                var subjectsForClass = await _dataService.GetSubjectsForClassAsync(c.Id);
                var count = students.Count(s => s.ClassId == c.Id && s.IsActive);
                var info = new AutoTable.Models.ClassInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    StreamsCsv = string.Join(", ", streamsForClass.Select(s => s.Name)),
                    SubjectsCsv = string.Join(", ", subjectsForClass.Select(s => s.Name)),
                    StudentCount = count,
                    ClassTeacherName = teacherNames.TryGetValue(c.Id, out var tn) ? tn : string.Empty
                };
                ClassInfos.Add(info);
            }

            var allSubjects = await _dataService.GetSubjectsAsync();
            AllSubjects.Clear();
            foreach (var s in allSubjects) AllSubjects.Add(new SimpleLookup { Id = s.Id, Name = s.Name });
        }

        public async Task CreateClassAsync(string name, int? classTeacherId = null)
        {
            await _dataService.CreateClassAsync(name, classTeacherId);
        }

        public async Task DeleteClassAsync(int classId)
        {
            await _dataService.DeleteClassAsync(classId);
        }

        public async Task CreateSubjectAsync(string name)
        {
            await _dataService.CreateSubjectAsync(name);
        }

        public async Task CreateStreamAsync(string name, int? classId = null)
        {
            await _dataService.CreateStreamAsync(name, classId);
        }

        public async Task LoadSubjectsForClassAsync(int classId)
        {
            var subs = await _dataService.GetSubjectsForClassAsync(classId);
            SubjectsForClass.Clear();
            foreach (var s in subs) SubjectsForClass.Add(new SimpleLookup { Id = s.Id, Name = s.Name });
            var streams = await _dataService.GetStreamsForClassAsync(classId);
            StreamsForClass.Clear();
            foreach (var s in streams) StreamsForClass.Add(new SimpleLookup { Id = s.Id, Name = s.Name });
        }

        public async Task LoadAllStreamsAsync()
        {
            var list = await _dataService.GetAllStreamsAsync();
            AllStreams.Clear();
            foreach (var s in list) AllStreams.Add(new SimpleLookup { Id = s.Id, Name = s.Name });
        }

        public async Task AssignStreamToClassAsync(int classId, int streamId)
        {
            await _dataService.AssignStreamToClassAsync(classId, streamId);
        }

        public async Task RemoveStreamFromClassAsync(int classId, int streamId)
        {
            await _dataService.RemoveStreamFromClassAsync(classId, streamId);
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