using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoTable.Models
{
    /// <summary>
    /// One editable row in the Marks Entry grid. Implements INotifyPropertyChanged so
    /// typed marks flow from the UI back into the model (two-way) and the computed
    /// grade updates live as the user types.
    /// </summary>
    public class StudentMarkRow : INotifyPropertyChanged
    {
        private double? _mark;
        private string _grade = "-";
        private string _remarks = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public bool IsEditable { get; set; } = true;

        /// <summary>The numeric mark (null when not yet entered).</summary>
        public double? Mark
        {
            get => _mark;
            set
            {
                if (_mark == value) return;
                _mark = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MarkDisplay));
            }
        }

        /// <summary>
        /// Two-way text used by the mark TextBox. Parses user input into <see cref="Mark"/>;
        /// blank or non-numeric input clears the mark so drafts can be corrected later.
        /// </summary>
        public string MarkText
        {
            get => Mark.HasValue ? Mark.Value.ToString("0.#") : string.Empty;
            set
            {
                var trimmed = value?.Trim() ?? string.Empty;
                if (trimmed.Length == 0)
                {
                    Mark = null;
                    return;
                }

                if (double.TryParse(trimmed, out var parsed))
                {
                    // Clamp to a sensible 0-100 range so typos don't corrupt the gradebook.
                    if (parsed < 0) parsed = 0;
                    if (parsed > 100) parsed = 100;
                    Mark = parsed;
                }
                // Non-numeric input is ignored (keeps the last valid mark).
            }
        }

        /// <summary>Read-only projection of the mark for display bindings.</summary>
        public string MarkDisplay => Mark.HasValue ? Mark.Value.ToString("0.#") : string.Empty;

        public string Grade
        {
            get => _grade;
            set
            {
                if (_grade == value) return;
                _grade = value;
                OnPropertyChanged();
            }
        }

        public string Remarks
        {
            get => _remarks;
            set
            {
                if (_remarks == value) return;
                _remarks = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Recomputes the letter grade from the current mark.</summary>
        public void RefreshGrade()
        {
            Grade = Mark switch
            {
                null => "-",
                >= 80 => "A",
                >= 70 => "B",
                >= 60 => "C",
                >= 50 => "D",
                >= 40 => "E",
                _ => "F"
            };
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
