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
    public partial class TermManagementViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        public ObservableCollection<SimpleLookup> Terms { get; } = new();
        public ObservableCollection<SimpleLookup> Classes { get; } = new();
        public ObservableCollection<TermFee> TermFees { get; } = new();

        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private int? _activeTermId;

        // School-wide KPI values for the selected term
        [ObservableProperty] private decimal _schoolExpected;
        [ObservableProperty] private decimal _schoolCollected;
        [ObservableProperty] private decimal _schoolOutstanding;
        [ObservableProperty] private double _schoolCollectionRate;
        [ObservableProperty] private int _schoolStudentCount;
        [ObservableProperty] private string _kpiTermLabel = string.Empty;

        public TermManagementViewModel()
        {
            _dataService = AppServices.DataService ?? throw new InvalidOperationException("DataService not configured.");
            _ = LoadAsync();
        }

        public async Task LoadAsync()
        {
            Terms.Clear();
            var terms = await _dataService.GetTermLookupsAsync();
            foreach (var t in terms) Terms.Add(new SimpleLookup { Id = t.Id, Name = t.Name });

            var activeTerm = await _dataService.GetActiveTermAsync();
            ActiveTermId = activeTerm?.Id;

            Classes.Clear();
            var classes = await _dataService.GetClassesAsync();
            foreach (var c in classes) Classes.Add(new SimpleLookup { Id = c.Id, Name = c.Name });

            TermFees.Clear();
            var fees = await _dataService.GetTermFeesAsync();
            foreach (var f in fees) TermFees.Add(f);
        }

        /// <summary>Returns the configured fee amount for a given class within a specific term, or 0 if not set.</summary>
        public double GetFeeForClassInTerm(int classId, int termId)
        {
            return TermFees.FirstOrDefault(tf => tf.ClassId == classId && tf.TermId == termId)?.Amount ?? 0;
        }

        /// <summary>
        /// Computes school-wide KPIs for the given term: expected (term-fee × students per class),
        /// collected (sum of payments), outstanding, and collection rate.
        /// </summary>
        public async Task RefreshSchoolKpisAsync(int? termId, string? termName)        {
            if (termId == null)
            {
                SchoolExpected = 0;
                SchoolCollected = 0;
                SchoolOutstanding = 0;
                SchoolCollectionRate = 0;
                SchoolStudentCount = 0;
                KpiTermLabel = string.Empty;
                return;
            }

            var errors = new List<string>();

            // Students
            IReadOnlyList<Student> students = Array.Empty<Student>();
            try
            {
                students = await _dataService.GetStudentsAsync();
            }
            catch (Exception ex) { errors.Add("students: " + ex.Message); }
            var activeStudents = students.Where(s => s.IsActive).ToList();

            // Term fees for this term
            decimal expected = 0;
            try
            {
                var termFeesAll = await _dataService.GetTermFeesAsync();
                var termFeesThis = termFeesAll.Where(tf => tf.TermId == termId.Value).ToList();
                foreach (var tf in termFeesThis)
                {
                    var count = activeStudents.Count(s => s.ClassId == tf.ClassId);
                    expected += (decimal)(tf.Amount * count);
                }
            }
            catch (Exception ex) { errors.Add("term fees: " + ex.Message); }

            // Payments for this term
            decimal collected = 0;
            try
            {
                var payments = await _dataService.GetFeePaymentsAsync(null, termId);
                collected = (decimal)payments.Sum(p => p.Amount);
            }
            catch (Exception ex) { errors.Add("payments: " + ex.Message); }

            SchoolExpected = expected;
            SchoolCollected = collected;
            SchoolOutstanding = expected > collected ? expected - collected : 0;
            SchoolCollectionRate = expected == 0 ? 0 : Math.Round((double)(collected / expected * 100), 1);
            SchoolStudentCount = activeStudents.Count;
            KpiTermLabel = termName ?? string.Empty;

            if (errors.Count > 0)
                StatusMessage = "KPI load errors: " + string.Join("; ", errors);
        }

        public async Task CreateTermAsync(string name, DateTime? start, DateTime? end)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            await _dataService.CreateTermAsync(name, start, end);
            await LoadAsync();
        }

        public async Task SetTermFeeAsync(int termId, int classId, double amount)
        {
            await _dataService.SetTermFeeAsync(termId, classId, amount);
            await LoadAsync();
        }

        public async Task ActivateTermAsync(int termId)
        {
            await _dataService.SetActiveTermAsync(termId);
            ActiveTermId = termId;
            StatusMessage = "Term activated successfully.";
        }

        public async Task DeactivateTermAsync(int termId)
        {
            await _dataService.DeactivateTermAsync(termId);
            if (ActiveTermId == termId)
                ActiveTermId = null;
            StatusMessage = "Term deactivated.";
        }
    }
}
