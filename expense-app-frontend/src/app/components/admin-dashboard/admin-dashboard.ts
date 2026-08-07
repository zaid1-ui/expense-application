import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Expense } from '../../services/expense';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css',
})
export class AdminDashboard implements OnInit {
  activeTab: 'transactions' | 'history' | 'reports' = 'transactions';

  transactions: any[] = [];
  history: any[] = [];
  byStatus: any[] = [];
  byEmployee: any[] = [];
  byCategory: any[] = [];
  monthly: any[] = [];

  filterStatus = '';
  filterEmployee = '';
  errorMessage = '';

  constructor(
    private expenseService: Expense,
    public auth: Auth,
  ) {}

  ngOnInit(): void {
    this.loadTransactions();
  }

  setTab(tab: 'transactions' | 'history' | 'reports'): void {
    this.activeTab = tab;
    if (tab === 'transactions') this.loadTransactions();
    if (tab === 'history') this.loadHistory();
    if (tab === 'reports') this.loadReports();
  }

  loadTransactions(): void {
    this.expenseService.getAllTransactions(this.filterStatus, this.filterEmployee).subscribe({
      next: (data) => (this.transactions = data),
      error: () => (this.errorMessage = 'Failed to load transactions.'),
    });
  }

  loadHistory(): void {
    this.expenseService.getApprovalHistory().subscribe({
      next: (data) => (this.history = data),
      error: () => (this.errorMessage = 'Failed to load history.'),
    });
  }

  loadReports(): void {
    this.expenseService.getReportByStatus().subscribe((data) => (this.byStatus = data));
    this.expenseService.getReportByEmployee().subscribe((data) => (this.byEmployee = data));
    this.expenseService.getReportByCategory().subscribe((data) => (this.byCategory = data));
    this.expenseService.getMonthlyReport().subscribe((data) => (this.monthly = data));
  }

  logout(): void {
    this.auth.logout();
  }
}
