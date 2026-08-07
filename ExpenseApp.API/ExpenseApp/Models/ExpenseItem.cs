namespace ExpenseApp.Models
{
    public class ExpenseItem
    {
        public int Id { get; set; }
        public int ExpenseFormId { get; set; }
        public ExpenseForm ExpenseForm { get; set; } = null!;

        public DateTime ExpenseDate { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Taxi, Food, Gas, etc.
        public decimal Amount { get; set; }
    }
}