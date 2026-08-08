/*
    Expense Application — stored procedures.

    Run this AFTER Database/InitialSetup.sql. Every query and write the
    backend performs now goes through one of these procedures — the C#
    controllers no longer contain LINQ or inline SQL, only calls to these
    names via Dapper.

    Re-runnable: each CREATE OR ALTER PROCEDURE overwrites the previous
    definition, so running this script again after an edit is safe.

    Enum values used as plain INTs below (must match the C# enums):
      UserRole:      0=Employee, 1=Manager, 2=Accountant, 3=Admin
      ExpenseStatus: 0=Draft, 1=PendingApproval, 2=ChangeRequested, 3=Approved, 4=Paid
*/

USE ExpenseAppDB;
GO

-- ============================================================
-- Auth
-- ============================================================

CREATE OR ALTER PROCEDURE sp_GetUserByUsername
    @Username NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, PasswordHash, FullName, Role, ManagerId
    FROM Users
    WHERE Username = @Username;
END
GO

CREATE OR ALTER PROCEDURE sp_GetManagers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, FullName
    FROM Users
    WHERE Role = 1 -- Manager
    ORDER BY FullName;
END
GO

CREATE OR ALTER PROCEDURE sp_UsernameExists
    @Username NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CASE WHEN EXISTS (SELECT 1 FROM Users WHERE Username = @Username) THEN 1 ELSE 0 END;
END
GO

CREATE OR ALTER PROCEDURE sp_GetManagerById
    @ManagerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, FullName
    FROM Users
    WHERE Id = @ManagerId AND Role = 1; -- Manager
END
GO

CREATE OR ALTER PROCEDURE sp_InsertUser
    @Username NVARCHAR(450),
    @PasswordHash NVARCHAR(MAX),
    @FullName NVARCHAR(MAX),
    @Role INT,
    @ManagerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Users (Username, PasswordHash, FullName, Role, ManagerId)
    VALUES (@Username, @PasswordHash, @FullName, @Role, @ManagerId);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

CREATE OR ALTER PROCEDURE sp_GetUserCount
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM Users;
END
GO

-- ============================================================
-- Shared expense-form writes (used by Employee submit/edit,
-- Manager approve/reject, Accountant pay)
-- ============================================================

CREATE OR ALTER PROCEDURE sp_InsertExpenseForm
    @EmployeeId INT,
    @Currency NVARCHAR(10),
    @Status INT,
    @CreatedDate DATETIME2,
    @SubmittedDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ExpenseForms (EmployeeId, Currency, Status, CreatedDate, SubmittedDate, RejectionReason)
    VALUES (@EmployeeId, @Currency, @Status, @CreatedDate, @SubmittedDate, NULL);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

CREATE OR ALTER PROCEDURE sp_InsertExpenseItem
    @ExpenseFormId INT,
    @ExpenseDate DATETIME2,
    @Purpose NVARCHAR(200),
    @Category NVARCHAR(100),
    @Amount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ExpenseItems (ExpenseFormId, ExpenseDate, Purpose, Category, Amount)
    VALUES (@ExpenseFormId, @ExpenseDate, @Purpose, @Category, @Amount);
END
GO

CREATE OR ALTER PROCEDURE sp_InsertApprovalHistory
    @ExpenseFormId INT,
    @ActionByUserId INT,
    @Action NVARCHAR(50),
    @Reason NVARCHAR(MAX) = NULL,
    @ActionDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ApprovalHistories (ExpenseFormId, ActionByUserId, Action, Reason, ActionDate)
    VALUES (@ExpenseFormId, @ActionByUserId, @Action, @Reason, @ActionDate);
END
GO

CREATE OR ALTER PROCEDURE sp_DeleteExpenseItemsByForm
    @FormId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM ExpenseItems WHERE ExpenseFormId = @FormId;
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateExpenseForm
    @FormId INT,
    @Currency NVARCHAR(10),
    @Status INT,
    @RejectionReason NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ExpenseForms
    SET Currency = @Currency, Status = @Status, RejectionReason = @RejectionReason
    WHERE Id = @FormId;
END
GO

-- Single-form lookup used by Approve / RequestChange / Pay to check status
-- and (via the employee's ManagerId) that the caller is allowed to act on it.
CREATE OR ALTER PROCEDURE sp_GetExpenseFormById
    @FormId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Id, f.EmployeeId, e.FullName AS EmployeeName, f.Currency, f.Status,
           f.CreatedDate, f.SubmittedDate, f.RejectionReason, e.ManagerId AS EmployeeManagerId
    FROM ExpenseForms f
    JOIN Users e ON e.Id = f.EmployeeId
    WHERE f.Id = @FormId;
END
GO

CREATE OR ALTER PROCEDURE sp_ApproveForm
    @FormId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ExpenseForms SET Status = 3 WHERE Id = @FormId; -- Approved
END
GO

CREATE OR ALTER PROCEDURE sp_RequestChangeForm
    @FormId INT,
    @Reason NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ExpenseForms SET Status = 2, RejectionReason = @Reason WHERE Id = @FormId; -- ChangeRequested
END
GO

CREATE OR ALTER PROCEDURE sp_PayForm
    @FormId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ExpenseForms SET Status = 4 WHERE Id = @FormId; -- Paid
END
GO

-- ============================================================
-- Employee
-- ============================================================

CREATE OR ALTER PROCEDURE sp_GetExpenseFormForEdit
    @FormId INT,
    @EmployeeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, EmployeeId, Currency, Status, CreatedDate, SubmittedDate, RejectionReason
    FROM ExpenseForms
    WHERE Id = @FormId AND EmployeeId = @EmployeeId;
END
GO

-- Two result sets: matching forms, then the items belonging to those forms.
-- The C# side reads both and groups items onto their form by ExpenseFormId.
CREATE OR ALTER PROCEDURE sp_GetMyForms
    @EmployeeId INT,
    @Status INT = NULL,
    @Currency NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT f.Id, f.EmployeeId, e.FullName AS EmployeeName, f.Currency, f.Status,
           f.CreatedDate, f.SubmittedDate, f.RejectionReason
    INTO #Forms
    FROM ExpenseForms f
    JOIN Users e ON e.Id = f.EmployeeId
    WHERE f.EmployeeId = @EmployeeId
      AND (@Status IS NULL OR f.Status = @Status)
      AND (@Currency IS NULL OR f.Currency = @Currency);

    SELECT * FROM #Forms ORDER BY CreatedDate DESC;

    SELECT i.ExpenseFormId, i.ExpenseDate, i.Purpose, i.Category, i.Amount
    FROM ExpenseItems i
    WHERE i.ExpenseFormId IN (SELECT Id FROM #Forms);

    DROP TABLE #Forms;
END
GO

-- ============================================================
-- Manager
-- ============================================================

CREATE OR ALTER PROCEDURE sp_GetAwaitingApproval
    @ManagerId INT,
    @Currency NVARCHAR(10) = NULL,
    @EmployeeName NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT f.Id, f.EmployeeId, e.FullName AS EmployeeName, f.Currency, f.Status,
           f.CreatedDate, f.SubmittedDate, f.RejectionReason
    INTO #Forms
    FROM ExpenseForms f
    JOIN Users e ON e.Id = f.EmployeeId
    WHERE f.Status = 1 -- PendingApproval
      AND e.ManagerId = @ManagerId
      AND (@Currency IS NULL OR f.Currency = @Currency)
      AND (@EmployeeName IS NULL OR e.FullName LIKE '%' + @EmployeeName + '%');

    SELECT * FROM #Forms ORDER BY CreatedDate DESC;

    SELECT i.ExpenseFormId, i.ExpenseDate, i.Purpose, i.Category, i.Amount
    FROM ExpenseItems i
    WHERE i.ExpenseFormId IN (SELECT Id FROM #Forms);

    DROP TABLE #Forms;
END
GO

-- ============================================================
-- Accountant
-- ============================================================

CREATE OR ALTER PROCEDURE sp_GetToBePaid
    @Currency NVARCHAR(10) = NULL,
    @EmployeeName NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT f.Id, f.EmployeeId, e.FullName AS EmployeeName, f.Currency, f.Status,
           f.CreatedDate, f.SubmittedDate, f.RejectionReason
    INTO #Forms
    FROM ExpenseForms f
    JOIN Users e ON e.Id = f.EmployeeId
    WHERE f.Status = 3 -- Approved
      AND (@Currency IS NULL OR f.Currency = @Currency)
      AND (@EmployeeName IS NULL OR e.FullName LIKE '%' + @EmployeeName + '%');

    SELECT * FROM #Forms ORDER BY CreatedDate DESC;

    SELECT i.ExpenseFormId, i.ExpenseDate, i.Purpose, i.Category, i.Amount
    FROM ExpenseItems i
    WHERE i.ExpenseFormId IN (SELECT Id FROM #Forms);

    DROP TABLE #Forms;
END
GO

-- ============================================================
-- Admin (all read-only)
-- ============================================================

CREATE OR ALTER PROCEDURE sp_GetAllTransactions
    @Status INT = NULL,
    @EmployeeName NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT f.Id, f.EmployeeId, e.FullName AS EmployeeName, f.Currency, f.Status,
           f.CreatedDate, f.SubmittedDate, f.RejectionReason
    INTO #Forms
    FROM ExpenseForms f
    JOIN Users e ON e.Id = f.EmployeeId
    WHERE (@Status IS NULL OR f.Status = @Status)
      AND (@EmployeeName IS NULL OR e.FullName LIKE '%' + @EmployeeName + '%');

    SELECT * FROM #Forms ORDER BY CreatedDate DESC;

    SELECT i.ExpenseFormId, i.ExpenseDate, i.Purpose, i.Category, i.Amount
    FROM ExpenseItems i
    WHERE i.ExpenseFormId IN (SELECT Id FROM #Forms);

    DROP TABLE #Forms;
END
GO

CREATE OR ALTER PROCEDURE sp_GetApprovalHistory
    @Action NVARCHAR(50) = NULL,
    @EmployeeName NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT h.Id, h.ExpenseFormId, u.FullName AS ActionBy, u.Role AS ActionByRole,
           h.Action, h.Reason, h.ActionDate
    FROM ApprovalHistories h
    JOIN Users u ON u.Id = h.ActionByUserId
    WHERE (@Action IS NULL OR h.Action = @Action)
      AND (@EmployeeName IS NULL OR u.FullName LIKE '%' + @EmployeeName + '%')
    ORDER BY h.ActionDate DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_ReportByStatus
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.Status, COUNT(DISTINCT f.Id) AS FormCount, ISNULL(SUM(i.Amount), 0) AS TotalAmount
    FROM ExpenseForms f
    LEFT JOIN ExpenseItems i ON i.ExpenseFormId = f.Id
    GROUP BY f.Status;
END
GO

CREATE OR ALTER PROCEDURE sp_ReportByEmployee
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.FullName AS EmployeeName, COUNT(DISTINCT f.Id) AS FormCount, ISNULL(SUM(i.Amount), 0) AS TotalAmount
    FROM ExpenseForms f
    JOIN Users e ON e.Id = f.EmployeeId
    LEFT JOIN ExpenseItems i ON i.ExpenseFormId = f.Id
    GROUP BY e.FullName;
END
GO

CREATE OR ALTER PROCEDURE sp_ReportByCategory
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Category, COUNT(*) AS ItemCount, SUM(Amount) AS TotalAmount
    FROM ExpenseItems
    GROUP BY Category;
END
GO

CREATE OR ALTER PROCEDURE sp_ReportByMonthly
AS
BEGIN
    SET NOCOUNT ON;
    SELECT YEAR(f.CreatedDate) AS [Year], MONTH(f.CreatedDate) AS [Month],
           COUNT(DISTINCT f.Id) AS FormCount, ISNULL(SUM(i.Amount), 0) AS TotalAmount
    FROM ExpenseForms f
    LEFT JOIN ExpenseItems i ON i.ExpenseFormId = f.Id
    GROUP BY YEAR(f.CreatedDate), MONTH(f.CreatedDate)
    ORDER BY [Year] DESC, [Month] DESC;
END
GO
