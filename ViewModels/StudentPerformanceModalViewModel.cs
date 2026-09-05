using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;


namespace AutoTable.ViewModels
{
    public partial class StudentPerformanceModalViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private readonly string _studentName;
        private readonly string _className;
        private readonly string _subject;
        private readonly string _academicYear;
        private readonly string _term;
        private readonly string _stream;

        private StudentPerformanceDetail _detail = new();

        public string StudentName => _detail.StudentName;
        public string AdmissionNumber => _detail.AdmissionNumber;
        public string ClassName => _detail.ClassName;
        public string Stream => _detail.Stream;
        public string AcademicYear => _detail.AcademicYear;
        public string Term => _detail.Term;
        public double OverallAverage => _detail.OverallAverage;
        public string OverallGrade => _detail.OverallGrade;
        public int Rank => _detail.Rank;
        public string Status => _detail.Status;
        public ObservableCollection<StudentSubjectPerformance> SubjectPerformances { get; } = new();

        public int TotalSubjects => SubjectPerformances.Count;
        public int AtRiskSubjects => SubjectPerformances.Count(s => s.Average < 40);
        public int ExcellentSubjects => SubjectPerformances.Count(s => s.Average >= 80);
        public double SubjectAverage => SubjectPerformances.Count == 0 ? 0 : SubjectPerformances.Average(s => s.Average);

        public StudentPerformanceModalViewModel(IDataService dataService, string studentName, string className,
            string subject, string academicYear, string term, string stream)
        {
            _dataService = dataService;
            _studentName = studentName;
            _className = className;
            _subject = subject;
            _academicYear = academicYear;
            _term = term;
            _stream = stream;
        }

        public async Task InitializeAsync()
        {
            _detail = await _dataService.GetStudentPerformanceDetailAsync(
                _studentName, _className, _subject, _academicYear, _term, _stream);

            SubjectPerformances.Clear();
            foreach (var sp in _detail.SubjectPerformances)
                SubjectPerformances.Add(sp);

            OnPropertyChanged(nameof(StudentName));
            OnPropertyChanged(nameof(AdmissionNumber));
            OnPropertyChanged(nameof(ClassName));
            OnPropertyChanged(nameof(Stream));
            OnPropertyChanged(nameof(AcademicYear));
            OnPropertyChanged(nameof(Term));
            OnPropertyChanged(nameof(OverallAverage));
            OnPropertyChanged(nameof(OverallGrade));
            OnPropertyChanged(nameof(Rank));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(TotalSubjects));
            OnPropertyChanged(nameof(AtRiskSubjects));
            OnPropertyChanged(nameof(ExcellentSubjects));
            OnPropertyChanged(nameof(SubjectAverage));
        }

        [RelayCommand]
        private void Close()
        {
            // Closing is handled by the View via the ContentDialog
        }
    }
}