namespace ExpenseApp.Enums
{
    public enum ExpenseStatus
    {
        Draft,
        PendingApproval,
        ChangeRequested,   // Manager ne reject with reason
        Approved,           // Manager approved, Accountant ko jayega
        Paid                // Accountant ne pay kar diya
    }
}