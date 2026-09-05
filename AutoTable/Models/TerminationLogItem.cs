using System;

namespace AutoTable.Models
{
    public class TerminationLogItem
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime TerminationDate { get; set; }
        public bool Anonymized { get; set; }
        public DateTime LoggedAt { get; set; }

        public string TerminationDateDisplay => TerminationDate.ToString("dd MMM yyyy");
        public string LoggedAtDisplay => LoggedAt.ToString("dd MMM yyyy HH:mm");
        public string AnonymizedDisplay => Anonymized ? "Anonymized" : "Not anonymized";
    }
}