/*
    Expense Application — schema + seed data.

    Run this once against your SQL Server instance. It replaces EF Core's
    code-first migration (the app used to build these tables itself via
    `db.Database.Migrate()` on startup) — the app now expects the schema
    to already exist and connects to it directly through EF Core, which
    is still used for every query (LINQ), just not for creating tables.

    How to run:
      - SSMS: open this file, connect to your server, press Execute (F5).
      - sqlcmd: sqlcmd -S <server> -i InitialSetup.sql

    Safe to re-run: table creation is guarded with IF NOT EXISTS, and the
    seed insert is guarded so it won't duplicate users on a second run.
*/

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'ExpenseAppDB')
BEGIN
    CREATE DATABASE ExpenseAppDB;
END
GO

USE ExpenseAppDB;
GO

-- ============================================================
-- Users
-- Self-referencing ManagerId: an Employee's ManagerId points at
-- another row in this same table (a user with Role = 1/Manager).
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Username     NVARCHAR(450)     NOT NULL,
        PasswordHash NVARCHAR(MAX)     NOT NULL,
        FullName     NVARCHAR(MAX)     NOT NULL,
        Role         INT               NOT NULL,   -- 0=Employee, 1=Manager, 2=Accountant, 3=Admin
        ManagerId    INT               NULL,
        CONSTRAINT FK_Users_Users_ManagerId
            FOREIGN KEY (ManagerId) REFERENCES Users(Id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_Users_Username ON Users(Username);
    CREATE INDEX IX_Users_ManagerId ON Users(ManagerId);
END
GO

-- ============================================================
-- ExpenseForms
-- One row per submitted form. Currency is fixed per form; the
-- total is computed from ExpenseItems, never stored here.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExpenseForms')
BEGIN
    CREATE TABLE ExpenseForms (
        Id              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeId      INT               NOT NULL,
        Currency        NVARCHAR(MAX)     NOT NULL,
        Status          INT               NOT NULL,  -- 0=Draft,1=PendingApproval,2=ChangeRequested,3=Approved,4=Paid
        CreatedDate     DATETIME2         NOT NULL,
        SubmittedDate   DATETIME2         NULL,
        RejectionReason NVARCHAR(MAX)     NULL,
        CONSTRAINT FK_ExpenseForms_Users_EmployeeId
            FOREIGN KEY (EmployeeId) REFERENCES Users(Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_ExpenseForms_EmployeeId ON ExpenseForms(EmployeeId);
END
GO

-- ============================================================
-- ExpenseItems
-- Line items within a form. Deleting a form deletes its items
-- (ON DELETE CASCADE) — the only cascade in this schema.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExpenseItems')
BEGIN
    CREATE TABLE ExpenseItems (
        Id            INT IDENTITY(1,1)  NOT NULL PRIMARY KEY,
        ExpenseFormId INT                NOT NULL,
        ExpenseDate   DATETIME2          NOT NULL,
        Purpose       NVARCHAR(MAX)      NOT NULL,
        Category      NVARCHAR(MAX)      NOT NULL,
        Amount        DECIMAL(18,2)      NOT NULL,
        CONSTRAINT FK_ExpenseItems_ExpenseForms_ExpenseFormId
            FOREIGN KEY (ExpenseFormId) REFERENCES ExpenseForms(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_ExpenseItems_ExpenseFormId ON ExpenseItems(ExpenseFormId);
END
GO

-- ============================================================
-- ApprovalHistories
-- Append-only audit trail: one row per Submitted/Approved/
-- ChangeRequested/Paid action, used by the Admin dashboard.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApprovalHistories')
BEGIN
    CREATE TABLE ApprovalHistories (
        Id             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ExpenseFormId  INT               NOT NULL,
        ActionByUserId INT               NOT NULL,
        Action         NVARCHAR(MAX)     NOT NULL,  -- "Submitted" | "Approved" | "ChangeRequested" | "Paid"
        Reason         NVARCHAR(MAX)     NULL,
        ActionDate     DATETIME2         NOT NULL,
        CONSTRAINT FK_ApprovalHistories_ExpenseForms_ExpenseFormId
            FOREIGN KEY (ExpenseFormId) REFERENCES ExpenseForms(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_ApprovalHistories_Users_ActionByUserId
            FOREIGN KEY (ActionByUserId) REFERENCES Users(Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_ApprovalHistories_ExpenseFormId ON ApprovalHistories(ExpenseFormId);
    CREATE INDEX IX_ApprovalHistories_ActionByUserId ON ApprovalHistories(ActionByUserId);
END
GO

-- ============================================================
-- Seed data — same 6 test accounts the app used to create in C#
-- on first run. Password hashes below are real bcrypt hashes for
-- the plaintext passwords shown in each comment; login works
-- exactly as before.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Users)
BEGIN
    DECLARE @Manager1Id INT, @Manager2Id INT;

    INSERT INTO Users (Username, PasswordHash, FullName, Role, ManagerId)
    VALUES ('manager1', '$2a$11$yiAyum1WCCGt/e4ff7RnwuiaEH36nh3I9hhZcvm4K6zjUKukY.SYq', 'Ali Manager', 1, NULL); -- Manager@123
    SET @Manager1Id = SCOPE_IDENTITY();

    INSERT INTO Users (Username, PasswordHash, FullName, Role, ManagerId)
    VALUES ('manager2', '$2a$11$yiAyum1WCCGt/e4ff7RnwuiaEH36nh3I9hhZcvm4K6zjUKukY.SYq', 'Sara Manager', 1, NULL); -- Manager@123
    SET @Manager2Id = SCOPE_IDENTITY();

    INSERT INTO Users (Username, PasswordHash, FullName, Role, ManagerId)
    VALUES ('employee1', '$2a$11$7WA9D7SjbiB.Un56NYg1Q.2/8qsooVM94FYLJaMRcWbabxbq1aFFS', 'Zaid Employee', 0, @Manager1Id); -- Employee@123

    INSERT INTO Users (Username, PasswordHash, FullName, Role, ManagerId)
    VALUES ('employee2', '$2a$11$7WA9D7SjbiB.Un56NYg1Q.2/8qsooVM94FYLJaMRcWbabxbq1aFFS', 'Bilal Employee', 0, @Manager2Id); -- Employee@123

    INSERT INTO Users (Username, PasswordHash, FullName, Role, ManagerId)
    VALUES ('accountant1', '$2a$11$5X7Y663haGFtxjTWzXSqr.u6zVBWf7paExhr9be8ZzOOQWQddoIra', 'Hina Accountant', 2, NULL); -- Accountant@123

    INSERT INTO Users (Username, PasswordHash, FullName, Role, ManagerId)
    VALUES ('admin1', '$2a$11$xekYLmNtI5Dd5Arl7di4QOjkR8Hqk/lFKyWuBL4T3RiD6W0se2wOu', 'Admin User', 3, NULL); -- Admin@123
END
GO
