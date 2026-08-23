using AutoTable.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoTable.Views
{
    public sealed partial class TeachersView : Page
    {
        private readonly TeachersViewModel _vm;

        public TeachersView()
        {
            InitializeComponent();
            _vm = new TeachersViewModel();
            DataContext = _vm;
        }

        private void DeleteTeacher_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int teacherId)
            {
                _vm.DeleteTeacherCommand.Execute(teacherId);
            }
        }
    }
}