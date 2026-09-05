using System;

namespace AutoTable.Models
{
    /// <summary>
    /// Read model for a fee payment persisted in the database (FeePayments table).
    /// </summary>
    public class FeePaymentSummary
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public double Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public int? TermId { get; set; }
        public string TermName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class FeeRecord
    {
        public int RowNumber { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public decimal ExpectedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance => ExpectedAmount - PaidAmount;
        public string PaymentStatus => Balance <= 0 ? "Paid" : PaidAmount > 0 ? "Partial" : "Unpaid";
        public string Term { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
    }

    public class BudgetLine
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Budgeted { get; set; }
        public decimal Spent { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public decimal Remaining => Budgeted - Spent;
        public double UtilisationPercent => Budgeted == 0 ? 0 : (double)(Spent / Budgeted * 100);
        public string Status => UtilisationPercent >= 100 ? "Over Budget" : UtilisationPercent >= 80 ? "Near Limit" : "On Track";
    }

    public class Transaction
    {
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentDate { get; set; } = string.Empty;
        public string Status { get; set; } = "Confirmed";
    }

    /// <summary>
    /// A student who owes money for a specific term/class.
    /// Used by the Defaulters analytics page.
    /// </summary>
    public class DefaulterRecord
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public int? ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public decimal ExpectedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance => ExpectedAmount - PaidAmount;
        public double PercentPaid => ExpectedAmount == 0 ? 100 : Math.Min(100, Math.Round((double)(PaidAmount / ExpectedAmount * 100), 1));
        public string Status => Balance <= 0 ? "Paid" : PaidAmount > 0 ? "Partial" : "Unpaid";
        public string TermName { get; set; } = string.Empty;
        public int DaysSinceEnrollment { get; set; }
        // Display properties for x:Bind (WinUI 3 doesn't support StringFormat)
        public string ExpectedDisplay => ExpectedAmount.ToString("N0");
        public string PaidDisplay => PaidAmount.ToString("N0");
        public string BalanceDisplay => Balance.ToString("N0");
    }

    /// <summary>
    /// Aggregated cohort-level finance summary for a class or school.
    /// </summary>
    public class CohortSummary
    {
        public string Label { get; set; } = string.Empty; // class name or "School-wide"
        public int TotalStudents { get; set; }
        public int PaidCount { get; set; }
        public int PartialCount { get; set; }
        public int UnpaidCount { get; set; }
        public decimal TotalExpected { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding => TotalExpected - TotalCollected;
        public double CollectionRate => TotalExpected == 0 ? 100 : Math.Round((double)(TotalCollected / TotalExpected * 100), 1);
        public double PaidPercent => TotalStudents == 0 ? 0 : Math.Round(PaidCount * 100.0 / TotalStudents, 1);
        // Display properties for x:Bind
        public string CollectionRateDisplay => $"{CollectionRate:F0}%";
    }

    /// <summary>
    /// An overpayment credit carried forward from one term to the next.
    /// </summary>
    public class StudentCredit
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int FromTermId { get; set; }
        public string FromTermName { get; set; } = string.Empty;
        public int? AppliedToTermId { get; set; }
        public string AppliedToTermName { get; set; } = string.Empty;
        public double Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AppliedAt { get; set; }
        public string? Description { get; set; }
        public bool IsApplied => AppliedAt.HasValue;
        public string AmountDisplay => Amount.ToString("N0");
    }
}
