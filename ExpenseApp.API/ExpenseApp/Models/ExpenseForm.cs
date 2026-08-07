using ExpenseApp.Enums;

namespace ExpenseApp.Models
{
    public class ExpenseForm
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public User Employee { get; set; } = null!;

        public string Currency { get; set; } = string.Empty; // TL, EUR, USD, PKR, INR
        public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? SubmittedDate { get; set; }

        public string? RejectionReason { get; set; } // Manager's reason for change request

        public ICollection<ExpenseItem> ExpenseItems { get; set; } = new List<ExpenseItem>();

        // Computed property — total nahi store karenge DB mein, calculate karenge
        public decimal TotalAmount => ExpenseItems?.Sum(e => e.Amount) ?? 0;
    }
}