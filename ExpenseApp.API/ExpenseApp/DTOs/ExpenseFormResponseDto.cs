namespace ExpenseApp.DTOs
{
    public class ExpenseFormResponseDto
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? RejectionReason { get; set; }
        public List<ExpenseItemDto> Items { get; set; } = new();
    }
}