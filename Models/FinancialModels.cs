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
}
