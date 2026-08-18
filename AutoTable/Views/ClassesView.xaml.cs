using AutoTable.Models;
using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class ClassesView : Page
    {
        private readonly ClassesViewModel _vm;

        public ClassesView()
        {
            this.InitializeComponent();
            _vm = new ClassesViewModel();
            DataContext = _vm;
            Loaded += ClassesView_Loaded;
        }

        private async void ClassesView_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadAsync();
            ClassesList.ItemsSource = _vm.Classes;
            SubjectPicker.ItemsSource = _vm.AllSubjects;
        }

        private async void CreateClass_Click(object sender, RoutedEventArgs e)
        {
            // simple prompt - in real app use ContentDialog
            var name = "New Class" + System.Guid.NewGuid().ToString().Substring(0, 4);
            await _vm.CreateClassAsync(name);
            await _vm.LoadAsync();
            ClassesList.ItemsSource = _vm.Classes;
        }

        private async void CreateSubject_Click(object sender, RoutedEventArgs e)
        {
            var name = NewSubjectName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            await _vm.CreateSubjectAsync(name);
            await _vm.LoadAsync();
            SubjectPicker.ItemsSource = _vm.AllSubjects;
        }

        private async void ClassesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is AutoTable.Models.SimpleLookup cls)
            {
                await _vm.LoadSubjectsForClassAsync(cls.Id);
                SelectedClassTitle.Text = cls.Name;
                AssignedSubjectsList.ItemsSource = _vm.SubjectsForClass;
                SubjectPicker.ItemsSource = _vm.AllSubjects;
            }
        }

        private async void AssignSubject_Click(object sender, RoutedEventArgs e)
        {
            if (ClassesList.SelectedItem is not AutoTable.Models.SimpleLookup cls) return;
            if (SubjectPicker.SelectedItem is not AutoTable.Models.SimpleLookup subj) return;
            await _vm.AssignSubjectToClassAsync(cls.Id, subj.Id);
            await _vm.LoadSubjectsForClassAsync(cls.Id);
            AssignedSubjectsList.ItemsSource = _vm.SubjectsForClass;
        }

        private async void RemoveSubject_Click(object sender, RoutedEventArgs e)
        {
            if (ClassesList.SelectedItem is not AutoTable.Models.SimpleLookup cls) return;
            if ((sender as Button)?.DataContext is not AutoTable.Models.SimpleLookup subj) return;
            await _vm.RemoveSubjectFromClassAsync(cls.Id, subj.Id);
            await _vm.LoadSubjectsForClassAsync(cls.Id);
            AssignedSubjectsList.ItemsSource = _vm.SubjectsForClass;
        }
    }
}
