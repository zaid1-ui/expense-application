namespace ExpenseApp.DTOs
{
    public class ExpenseItemDto
    {
        public DateTime ExpenseDate { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}