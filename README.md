# Expense Application

A role-based expense management system built with **ASP.NET Core Web API**, **Angular**, and **MSSQL**. Employees submit expenses, Managers approve/reject them, Accountants process payments, and Admins view all transactions and reports.

## Tech Stack

- **Backend:** ASP.NET Core (.NET 8) Web API, Entity Framework Core
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

### 2. Backend Setup

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

**Restore packages and run migrations:**

```bash
dotnet restore
dotnet ef database update
```

**Run the backend:**

```bash
dotnet run
```

The API will start at `http://localhost:5053` (check your terminal output for the exact port). Swagger UI is available at `http://localhost:5053/swagger`.

> On first run, the app automatically seeds 6 test users into the database (see [Test Accounts](#test-accounts) below).

### 3. Frontend Setup

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

The database is seeded automatically with the following users on first run:

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
- **"Zone.js" errors in Angular:** Run `npm install zone.js` inside the `expense-app-frontend` folder.
