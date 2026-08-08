# Expense Application

A role-based expense management system built with **ASP.NET Core Web API**, **Angular**, and **MSSQL**. Employees submit expenses, Managers approve/reject them, Accountants process payments, and Admins view all transactions and reports.

## Tech Stack

- **Backend:** ASP.NET Core (.NET 8) Web API, Dapper + stored procedures (no ORM/EF — schema and every query live in `Database/`)
- **Database:** MSSQL Server
- **Frontend:** Angular (latest, standalone components)
- **Auth:** JWT (JSON Web Tokens)

## Prerequisites

Before running this project, make sure you have installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org) (LTS version) + npm
- [Angular CLI](https://angular.dev) — install globally: `npm install -g @angular/cli`
- SQL Server (Developer Edition or SQL Server Express)
- SQL Server Management Studio (SSMS) — optional, for viewing the database

## Project Structure

```
ExpenseApplication/
├── Database/
│   ├── InitialSetup.sql       # Schema + seed data — run first
│   └── StoredProcedures.sql   # Every query/write the backend performs — run second
├── ExpenseApp.API/
│   └── ExpenseApp/          # .NET Web API backend
├── expense-app-frontend/    # Angular frontend
└── README.md
```

## Setup Instructions

### 1. Clone the repository

```bash
git clone https://github.com/zaid1-ui/expense-application.git
cd expense-application
```

### 2. Database Setup

Schema, seed data, and all data access are plain SQL — no EF migrations, no ORM. The backend calls stored procedures via Dapper for every read and write; it never generates or sends ad-hoc SQL text.

Run these two scripts **in order** against your SQL Server instance, either in SSMS or via `sqlcmd -S YOUR_SERVER_NAME -i <file>`:

1. **`Database/InitialSetup.sql`** — creates the `ExpenseAppDB` database, all four tables with their constraints/indexes, and the 6 test accounts below.
2. **`Database/StoredProcedures.sql`** — creates the ~24 stored procedures the backend calls (one per query/write: login, submit/edit expense, approve/reject, pay, admin reports, etc.).

Both are idempotent — safe to re-run after making a change (table creation is `IF NOT EXISTS`-guarded, procedures use `CREATE OR ALTER`).

### 3. Backend Setup

Navigate to the backend project:

```bash
cd ExpenseApp.API/ExpenseApp
```

**Configure the database connection:**

Open `appsettings.json` and update the connection string with your SQL Server instance name:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=ExpenseAppDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

> To find your SQL Server instance name, open SSMS and check the "Server name" shown on the connect screen (e.g. `localhost`, `localhost\SQLEXPRESS`, or `YOUR-PC\SQLEXPRESS`).

**Restore packages and run:**

```bash
dotnet restore
dotnet run
```

The API will start at `http://localhost:5053` (check your terminal output for the exact port). Swagger UI is available at `http://localhost:5053/swagger`.

> On startup, the app calls `sp_GetUserCount` and logs a warning (to the console and `Logs/`) if it comes back empty or unreachable — that means one of the two SQL scripts hasn't been run yet, or the connection string is wrong. The app still starts either way; only requests that touch the database will fail until it's fixed.

### 4. Frontend Setup

Open a **new terminal**, navigate to the frontend project:

```bash
cd expense-app-frontend
```

**Install dependencies:**

```bash
npm install
```

**Run the frontend:**

```bash
ng serve
```

The app will be available at `http://localhost:4200`.

> Make sure the backend is running before using the frontend, since the frontend calls the API at `http://localhost:5053`.

## Test Accounts

`Database/InitialSetup.sql` seeds the following users:

| Username      | Password         | Role                           |
| ------------- | ---------------- | ------------------------------ |
| `manager1`    | `Manager@123`    | Manager                        |
| `manager2`    | `Manager@123`    | Manager                        |
| `employee1`   | `Employee@123`   | Employee (reports to manager1) |
| `employee2`   | `Employee@123`   | Employee (reports to manager2) |
| `accountant1` | `Accountant@123` | Accountant                     |
| `admin1`      | `Admin@123`      | Admin                          |

## Application Flow

1. **Employee** logs in → submits an expense form with one or more items (max 5000 per item) → sends for approval.
2. **Manager** logs in → sees expenses awaiting approval from their own employees → approves the form, or requests a change (with a mandatory reason).
3. **Accountant** logs in → sees approved forms awaiting payment → marks them as paid.
4. **Admin** logs in → views all transactions, full approval history, and reports (by status, by employee, by category, monthly summary).

## Features

- Role-based JWT authentication (Employee / Manager / Accountant / Admin)
- Employees can edit forms while Pending or Change-Requested
- Managers only see their own employees' expenses
- Multi-currency support (PKR, USD, EUR, TL, INR)
- Real-time total calculation while entering expense items
- Filterable listing screens (status, currency, employee name)
- Full audit trail (approval history) for Admin
- Error logging on the backend

## Running Both Servers Together

You'll need **two terminals** running simultaneously:

**Terminal 1 (Backend):**

```bash
cd ExpenseApp.API/ExpenseApp
dotnet run
```

**Terminal 2 (Frontend):**

```bash
cd expense-app-frontend
ng serve
```

Then open your browser at `http://localhost:4200`.

## Troubleshooting

- **CORS errors:** Make sure the backend is running and `Program.cs` has CORS configured for `http://localhost:4200`.
- **Database connection errors:** Confirm your SQL Server instance name matches the connection string in `appsettings.json`, and that SQL Server Browser service is running if using a named instance (e.g. `SQLEXPRESS`).
- **"No users found" / "Could not reach the database" warning on startup:** run `Database/InitialSetup.sql` then `Database/StoredProcedures.sql` against the server your connection string points to.
- **"Could not find stored procedure 'sp_...'" errors while using the app:** `Database/StoredProcedures.sql` hasn't been run, or was run against a different database than the one in your connection string.
- **"Zone.js" errors in Angular:** Run `npm install zone.js` inside the `expense-app-frontend` folder.
