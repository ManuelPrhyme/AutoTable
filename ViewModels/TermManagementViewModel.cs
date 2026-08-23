using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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

            Classes.Clear();
            var classes = await _dataService.GetClassesAsync();
            foreach (var c in classes) Classes.Add(new SimpleLookup { Id = c.Id, Name = c.Name });

            TermFees.Clear();
            var fees = await _dataService.GetTermFeesAsync();
            foreach (var f in fees) TermFees.Add(f);
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
    }
}
