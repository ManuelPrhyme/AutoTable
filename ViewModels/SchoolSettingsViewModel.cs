using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class SchoolSettingsViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        [ObservableProperty] private string _schoolName = "AutoTable Academy";
        [ObservableProperty] private string _schoolAddress = "";
        [ObservableProperty] private string _schoolPhone = "";
        [ObservableProperty] private string _headTeacherName = "";
        [ObservableProperty] private string _motto = "";
        [ObservableProperty] private string _statusMessage = "";

        public SchoolSettingsViewModel()
        {
            _dataService = AppServices.DataService!;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var settings = await _dataService.GetSchoolSettingsAsync();
            SchoolName = settings.SchoolName;
            SchoolAddress = settings.SchoolAddress;
            SchoolPhone = settings.SchoolPhone;
            HeadTeacherName = settings.HeadTeacherName;
            Motto = settings.Motto;
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private async Task SaveAsync()
        {
            var settings = new SchoolSettings
            {
                SchoolName = SchoolName,
                SchoolAddress = SchoolAddress,
                SchoolPhone = SchoolPhone,
                HeadTeacherName = HeadTeacherName,
                Motto = Motto
            };
            await _dataService.UpdateSchoolSettingsAsync(settings);
            StatusMessage = "Settings saved successfully.";
        }
    }
}
