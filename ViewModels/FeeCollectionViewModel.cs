using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace AutoTable.ViewModels
{
    public partial class FeeCollectionViewModel : BaseViewModel
    {
        [ObservableProperty] private string _selectedClass = "P5";
        [ObservableProperty] private string _selectedTerm = "Term 2, 2025";
        [ObservableProperty] private string _selectedStatus = "All";

        public ObservableCollection<string> Classes { get; }
        public ObservableCollection<string> Terms { get; }
        public ObservableCollection<string> StatusOptions { get; } = new() { "All", "Paid", "Partial", "Unpaid" };
        public ObservableCollection<FeeRecord> FeeRecords { get; } = new();

        public decimal TotalExpected => FeeRecords.Sum(f => f.ExpectedAmount);
        public decimal TotalCollected => FeeRecords.Sum(f => f.PaidAmount);
        public decimal TotalOutstanding => FeeRecords.Sum(f => f.Balance);
        public double CollectionRate => TotalExpected == 0 ? 0 : (double)(TotalCollected / TotalExpected * 100);

        public FeeCollectionViewModel()
        {
            Classes = new ObservableCollection<string>(MockDataService.Instance.Classes);
            Terms = new ObservableCollection<string>(MockDataService.Instance.Terms);
            Load();
        }

        partial void OnSelectedClassChanged(string value) => Load();
        partial void OnSelectedTermChanged(string value) => Load();

        [RelayCommand]
        private void Refresh() => Load();

        [RelayCommand]
        private void RecordPayment() { }

        private void Load()
        {
            FeeRecords.Clear();
            var students = MockDataService.Instance.GetStudentMarks(SelectedClass, "Mathematics", "Mid Term I");
            int i = 1;
            var rng = new System.Random(SelectedClass.GetHashCode());
            foreach (var s in students)
            {
                decimal expected = 450_000;
                decimal paid = rng.Next(0, 3) switch { 0 => 0, 1 => 225_000, _ => 450_000 };
                FeeRecords.Add(new FeeRecord
                {
                    RowNumber = i++,
                    StudentName = s.StudentName,
                    AdmissionNumber = s.AdmissionNumber,
                    ClassName = s.ClassName,
                    ExpectedAmount = expected,
                    PaidAmount = paid,
                    Term = SelectedTerm,
                    PaymentDate = paid > 0 ? "Jul 2025" : "-"
                });
            }
            OnPropertyChanged(nameof(TotalExpected));
            OnPropertyChanged(nameof(TotalCollected));
            OnPropertyChanged(nameof(TotalOutstanding));
            OnPropertyChanged(nameof(CollectionRate));
        }
    }
}
