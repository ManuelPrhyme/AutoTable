using AutoTable.Models;
using AutoTable.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace AutoTable.ViewModels
{
    public partial class EnrollmentViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;

        public System.Collections.ObjectModel.ObservableCollection<AutoTable.Models.SimpleLookup> Classes { get; } = new();
        public System.Collections.ObjectModel.ObservableCollection<AutoTable.Models.SimpleLookup> Streams { get; } = new();

        [ObservableProperty] private int? selectedClassId;
        [ObservableProperty] private int? selectedStreamId;

        [ObservableProperty] private string fullName = string.Empty;
        [ObservableProperty] private string lin = string.Empty;
        [ObservableProperty] private DateTime dateOfBirth = DateTime.Today;
        [ObservableProperty] private string gender = string.Empty;
        [ObservableProperty] private string nationality = string.Empty;
        [ObservableProperty] private string religion = string.Empty;
        [ObservableProperty] private string previousSchool = string.Empty;

        [ObservableProperty] private string guardianName = string.Empty;
        [ObservableProperty] private string guardianRelationship = string.Empty;
        [ObservableProperty] private string guardianPhone = string.Empty;
        [ObservableProperty] private string guardianEmail = string.Empty;
        [ObservableProperty] private string guardianAddress = string.Empty;
        [ObservableProperty] private bool hasCustodyDocuments;

        [ObservableProperty] private string residenceProofType = string.Empty;
        [ObservableProperty] private string residenceDistrict = string.Empty;
        [ObservableProperty] private string residenceZone = string.Empty;

        [ObservableProperty] private bool hasImmunizationCard;
        [ObservableProperty] private bool hasMedicalExamReport;
        [ObservableProperty] private string allergiesOrConditions = string.Empty;
        [ObservableProperty] private string healthInsurance = string.Empty;

        [ObservableProperty] private string emergencyName = string.Empty;
        [ObservableProperty] private string emergencyRelationship = string.Empty;
        [ObservableProperty] private string emergencyPhone = string.Empty;
        [ObservableProperty] private string authorizedPickupPerson = string.Empty;

        [ObservableProperty] private bool isSubmitting;
        [ObservableProperty] private string statusMessage = string.Empty;

        /// <summary>
        /// Callback invoked by the host (e.g. the Students page modal) after a successful save.
        /// The created Student (if any) is passed to the callback.
        /// </summary>
        public Func<Student?, Task>? OnSubmittedAsync { get; set; }

        // Notify the view when an error occurs so the UI can show a dialog
        public event Action<string>? ErrorOccurred;

        public EnrollmentViewModel()
        {
            _dataService = AppServices.DataService ?? throw new InvalidOperationException("DataService not configured.");
            _ = LoadLookupsAsync();
        }

        public async Task LoadLookupsAsync()
        {
            Classes.Clear();
            var classes = await _dataService.GetClassesAsync();
            foreach (var c in classes) Classes.Add(new SimpleLookup { Id = c.Id, Name = c.Name });

            Streams.Clear();
            var all = await _dataService.GetAllStreamsAsync();
            foreach (var s in all) Streams.Add(new SimpleLookup { Id = s.Id, Name = s.Name });
        }

        partial void OnSelectedClassIdChanged(int? value)
        {
            _ = LoadStreamsForSelectedClassAsync(value);
        }

        private async Task LoadStreamsForSelectedClassAsync(int? classId)
        {
            Streams.Clear();
            if (classId == null) return;
            var list = await _dataService.GetStreamsForClassAsync(classId.Value);
            foreach (var s in list) Streams.Add(new SimpleLookup { Id = s.Id, Name = s.Name });
        }

        [RelayCommand]
        private async Task SubmitAsync()
        {
            var autoLin = string.IsNullOrWhiteSpace(Lin)
                ? "LIN-" + System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()
                : Lin.Trim();

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(GuardianPhone))
            {
                StatusMessage = "Please provide the student's full name and guardian phone number.";
                return;
            }

            IsSubmitting = true;
            StatusMessage = "Submitting enrollment...";
            try
            {
                var data = new EnrollmentFormData
                {
                    FullName = FullName.Trim(),
                    LIN = autoLin,
                    DateOfBirth = DateOfBirth,
                    Gender = Gender,
                    Nationality = Nationality,
                    Religion = Religion,
                    PreviousSchool = PreviousSchool,
                    GuardianName = GuardianName,
                    GuardianRelationship = GuardianRelationship,
                    GuardianPhone = GuardianPhone,
                    GuardianEmail = GuardianEmail,
                    GuardianAddress = GuardianAddress,
                    HasCustodyDocuments = HasCustodyDocuments,
                    ResidenceProofType = ResidenceProofType,
                    ResidenceDistrict = ResidenceDistrict,
                    ResidenceZone = ResidenceZone,
                    HasImmunizationCard = HasImmunizationCard,
                    HasMedicalExamReport = HasMedicalExamReport,
                    AllergiesOrConditions = AllergiesOrConditions,
                    HealthInsurance = HealthInsurance,
                    EmergencyName = EmergencyName,
                    EmergencyRelationship = EmergencyRelationship,
                    EmergencyPhone = EmergencyPhone,
                    AuthorizedPickupPerson = AuthorizedPickupPerson,
                };

                // Auto-generate LIN when left blank (form hint: "leave blank to auto-generate")
                if (string.IsNullOrWhiteSpace(data.LIN))
                {
                    data.LIN = "LIN-" + System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
                }

                await _dataService.SaveEnrollmentAsync(data);

                // Also create the student record so they show up in the Students list
                Student? createdStudent = null;
                try
                {
                    createdStudent = await _dataService.CreateStudentAsync(new Student
                    {
                        LIN = data.LIN,
                        FullName = data.FullName,
                        AdmissionNumber = data.LIN,
                    ClassId = SelectedClassId,
                    StreamId = SelectedStreamId,
                        DateOfBirth = data.DateOfBirth,
                        Gender = data.Gender,
                        // map enrollment details into the student record
                        GuardianName = data.GuardianName,
                        GuardianRelationship = data.GuardianRelationship,
                        GuardianPhone = data.GuardianPhone,
                        GuardianEmail = data.GuardianEmail,
                        GuardianAddress = data.GuardianAddress,
                        HasCustodyDocuments = data.HasCustodyDocuments,
                        ResidenceProofType = data.ResidenceProofType,
                        ResidenceDistrict = data.ResidenceDistrict,
                        ResidenceZone = data.ResidenceZone,
                        HasImmunizationCard = data.HasImmunizationCard,
                        HasMedicalExamReport = data.HasMedicalExamReport,
                        AllergiesOrConditions = data.AllergiesOrConditions,
                        HealthInsurance = data.HealthInsurance,
                        EmergencyName = data.EmergencyName,
                        EmergencyRelationship = data.EmergencyRelationship,
                        EmergencyPhone = data.EmergencyPhone,
                        AuthorizedPickupPerson = data.AuthorizedPickupPerson,
                    });
                }
                catch (System.Exception ex)
                {
                    // student creation failed (LIN may already exist or other DB issue) — surface diagnostic
                    StatusMessage = "Student record creation failed: " + ex.Message;
                    try { ErrorOccurred?.Invoke(ex.ToString()); } catch { }
                }

                StatusMessage = "Enrollment submitted successfully.";

                if (OnSubmittedAsync != null) await OnSubmittedAsync.Invoke(createdStudent);
                ResetForm();
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to submit enrollment: " + ex.Message;
                try { ErrorOccurred?.Invoke(ex.Message); } catch { }
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        private void ResetForm()
        {
            FullName = string.Empty;
            Lin = string.Empty;
            DateOfBirth = DateTime.Today;
            Gender = string.Empty;
            Nationality = string.Empty;
            Religion = string.Empty;
            PreviousSchool = string.Empty;
            GuardianName = string.Empty;
            GuardianRelationship = string.Empty;
            GuardianPhone = string.Empty;
            GuardianEmail = string.Empty;
            GuardianAddress = string.Empty;
            HasCustodyDocuments = false;
            ResidenceProofType = string.Empty;
            ResidenceDistrict = string.Empty;
            ResidenceZone = string.Empty;
            HasImmunizationCard = false;
            HasMedicalExamReport = false;
            AllergiesOrConditions = string.Empty;
            HealthInsurance = string.Empty;
            EmergencyName = string.Empty;
            EmergencyRelationship = string.Empty;
            EmergencyPhone = string.Empty;
            AuthorizedPickupPerson = string.Empty;
        }
    }
}