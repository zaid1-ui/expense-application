namespace ExpenseApp.DTOs
{
    public class ExpenseFormCreateDto
    {
        public string Currency { get; set; } = string.Empty;
        public List<ExpenseItemDto> Items { get; set; } = new();
    }
}