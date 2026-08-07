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

  constructor(
    private expenseService: Expense,
    public auth: Auth,
  ) {}

  ngOnInit(): void {
    this.loadForms();
  }

  loadForms(): void {
    this.expenseService.getToBePaid(this.filterCurrency, this.filterEmployee).subscribe({
      next: (data) => (this.forms = data),
      error: () => (this.errorMessage = 'Failed to load forms.'),
    });
  }

  pay(id: number): void {
    this.expenseService.payForm(id).subscribe({
      next: () => {
        this.message = 'Expense marked as paid.';
        this.loadForms();
      },
      error: (err) => (this.errorMessage = err.error?.message || 'Payment failed.'),
    });
  }

  logout(): void {
    this.auth.logout();
  }
}
