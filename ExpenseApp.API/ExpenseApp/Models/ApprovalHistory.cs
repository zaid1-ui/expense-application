namespace ExpenseApp.Models
{
    public class ApprovalHistory
    {
        public int Id { get; set; }
        public int ExpenseFormId { get; set; }
        public ExpenseForm ExpenseForm { get; set; } = null!;

        public int ActionByUserId { get; set; }
        public User ActionByUser { get; set; } = null!;

        public string Action { get; set; } = string.Empty; // "Created", "Submitted", "Approved", "ChangeRequested", "Paid"
        public string? Reason { get; set; }
        public DateTime ActionDate { get; set; } = DateTime.Now;
    }
}