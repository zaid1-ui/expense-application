import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Expense, ExpenseFormResponse } from '../../services/expense';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-accountant-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './accountant-dashboard.html',
  styleUrl: './accountant-dashboard.css',
})
export class AccountantDashboard implements OnInit {
  forms: ExpenseFormResponse[] = [];
  filterCurrency = '';
  filterEmployee = '';
  message = '';
  errorMessage = '';
  expandedFormId: number | null = null;
  loading = false;
  busyIds = new Set<number>();

  constructor(
    private expenseService: Expense,
    public auth: Auth,
  ) {}

  ngOnInit(): void {
    this.loadForms();
  }

  loadForms(): void {
    this.loading = true;
    this.expenseService.getToBePaid(this.filterCurrency, this.filterEmployee).subscribe({
      next: (data) => {
        this.forms = data;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load forms.';
        this.loading = false;
      },
    });
  }

  toggleDetails(id: number): void {
    this.expandedFormId = this.expandedFormId === id ? null : id;
  }

  isBusy(id: number): boolean {
    return this.busyIds.has(id);
  }

  pay(id: number): void {
    if (this.isBusy(id)) return;
    this.errorMessage = '';
    this.busyIds.add(id);
    this.expenseService.payForm(id).subscribe({
      next: () => {
        this.message = 'Expense marked as paid.';
        this.busyIds.delete(id);
        this.loadForms();
      },
      error: (err) => {
        this.busyIds.delete(id);
        this.errorMessage = err.error?.message || 'Payment failed.';
      },
    });
  }

  logout(): void {
    this.auth.logout();
  }
}
