using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;


namespace AutoTable.ViewModels
{
    public partial class StudentPerformanceModalViewModel : BaseViewModel
    {
        private readonly StudentPerformanceDetail _detail;

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
        public ObservableCollection<StudentSubjectPerformance> SubjectPerformances { get; }

        public int TotalSubjects => SubjectPerformances.Count;
        public int AtRiskSubjects => SubjectPerformances.Count(s => s.Average < 40);
        public int ExcellentSubjects => SubjectPerformances.Count(s => s.Average >= 80);
        public double SubjectAverage => SubjectPerformances.Count == 0 ? 0 : SubjectPerformances.Average(s => s.Average);

        public StudentPerformanceModalViewModel(GradebookRow student)
        {
            _detail = MockDataService.Instance.GetStudentPerformanceDetail(
                student.StudentName,
                student.ClassName,
                student.Subject,
                student.AcademicYear,
                student.Term,
                student.Stream);

            SubjectPerformances = new ObservableCollection<StudentSubjectPerformance>(_detail.SubjectPerformances);
        }

        [RelayCommand]
        private void Close()
        {
            // Closing is handled by the View via the ContentDialog
        }
    }
}
