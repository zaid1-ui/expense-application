namespace ExpenseApp.Data.Rows
{
    // Plain mapping targets for Dapper — shaped to match exactly what each
    // stored procedure SELECTs, nothing more. Not EF entities: no tracking,
    // no navigation properties, no attributes needed.

    public class FormRow
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string? RejectionReason { get; set; }
    }

    // sp_GetExpenseFormById adds the employee's ManagerId, used to check a
    // Manager is only acting on their own employees' forms.
    public class FormDetailRow : FormRow
    {
        public int? EmployeeManagerId { get; set; }
    }

    public class ItemRow
    {
        public int ExpenseFormId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class UserRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int Role { get; set; }
        public int? ManagerId { get; set; }
    }

    public class ManagerRow
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public class HistoryRow
    {
        public int Id { get; set; }
        public int ExpenseFormId { get; set; }
        public string ActionBy { get; set; } = string.Empty;
        public int ActionByRole { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime ActionDate { get; set; }
    }

    public class StatusReportRow
    {
        public int Status { get; set; }
        public int FormCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class EmployeeReportRow
    {
        public string EmployeeName { get; set; } = string.Empty;
        public int FormCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class CategoryReportRow
    {
        public string Category { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class MonthlyReportRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int FormCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
