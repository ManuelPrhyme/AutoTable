using AutoTable.Models;
using AutoTable.Services;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace AutoTable.Views
{
    public sealed partial class ClassesView : Page
    {
        private readonly ClassesViewModel _vm;

        public ClassesView()
        {
            InitializeComponent();
            _vm = new ClassesViewModel();
            DataContext = _vm;
            Loaded += ClassesView_Loaded;
        }

        private async void ClassesView_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
            SelectedClassTitle.Text = "Select a class";
        }

        private async void CreateClass_Click(object sender, RoutedEventArgs e)
        {
            var nameBox = new TextBox { Header = "Class name", PlaceholderText = "e.g. P4", Width = 280 };
            var dialog = new ContentDialog
            {
                Title = "New Class",
                Content = nameBox,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
                IsPrimaryButtonEnabled = true
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var name = nameBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name)) return;

                try
                {
                    await _vm.CreateClassAsync(name);
                    await _vm.LoadAsync();
                }
                catch (System.Exception ex)
                {
                    await ShowErrorAsync("Unable to create class.", ex.Message);
                }
            }
        }

        private async void ClassesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is SimpleLookup cls)
            {
                await _vm.LoadSubjectsForClassAsync(cls.Id);
                SelectedClassTitle.Text = cls.Name;
            }
        }

        private async void AssignSubject_Click(object sender, RoutedEventArgs e)
        {
            if (ClassesList.SelectedItem is not SimpleLookup cls) return;
            if (SubjectPicker.SelectedItem is not SimpleLookup subj) return;
            try
            {
                await _vm.AssignSubjectToClassAsync(cls.Id, subj.Id);
                await _vm.LoadSubjectsForClassAsync(cls.Id);
            }
            catch (System.Exception ex)
            {
                await ShowErrorAsync("Unable to assign subject.", ex.Message);
            }
        }

        private async void CreateSubject_Click(object sender, RoutedEventArgs e)
        {
            var name = NewSubjectName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                await _vm.CreateSubjectAsync(name);
                await _vm.LoadAsync();
                NewSubjectName.Text = string.Empty;
            }
            catch (System.Exception ex)
            {
                await ShowErrorAsync("Unable to create subject.", ex.Message);
            }
        }

        private async void RemoveSubject_Click(object sender, RoutedEventArgs e)
        {
            if (ClassesList.SelectedItem is not SimpleLookup cls) return;
            if ((sender as Button)?.DataContext is not SimpleLookup subj) return;
            try
            {
                await _vm.RemoveSubjectFromClassAsync(cls.Id, subj.Id);
                await _vm.LoadSubjectsForClassAsync(cls.Id);
            }
            catch (System.Exception ex)
            {
                await ShowErrorAsync("Unable to remove subject.", ex.Message);
            }
        }

        private async Task ShowErrorAsync(string header, string message)
        {
            var dialog = new ContentDialog
            {
                Title = header,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
