import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Expense, ExpenseFormResponse, ExpenseItem } from '../../services/expense';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employee-dashboard.html',
  styleUrl: './employee-dashboard.css',
})
export class EmployeeDashboard implements OnInit {
  forms: ExpenseFormResponse[] = [];
  currency = 'PKR';
  items: ExpenseItem[] = [{ expenseDate: '', purpose: '', category: '', amount: 0 }];
  message = '';
  errorMessage = '';

  filterStatus = '';
  filterCurrency = '';

  editingFormId: number | null = null;

  constructor(
    private expenseService: Expense,
    public auth: Auth,
  ) {}

  ngOnInit(): void {
    this.loadForms();
  }

  get totalAmount(): number {
    return this.items.reduce((sum, i) => sum + Number(i.amount || 0), 0);
  }

  addItem(): void {
    this.items.push({ expenseDate: '', purpose: '', category: '', amount: 0 });
  }

  removeItem(index: number): void {
    this.items.splice(index, 1);
  }

  loadForms(): void {
    this.expenseService.getMyForms(this.filterStatus, this.filterCurrency).subscribe({
      next: (data) => (this.forms = data),
      error: () => (this.errorMessage = 'Failed to load forms.'),
    });
  }

  submitForm(): void {
    this.errorMessage = '';
    this.message = '';

    const invalidItem = this.items.some((i) => i.amount > 5000);
    if (invalidItem) {
      this.errorMessage = 'Each expense amount must not exceed 5000.';
      return;
    }

    const payload = { currency: this.currency, items: this.items };

    if (this.editingFormId) {
      this.expenseService.editExpenseForm(this.editingFormId, payload).subscribe({
        next: () => {
          this.message = 'Expense updated successfully.';
          this.resetForm();
          this.loadForms();
        },
        error: (err) => (this.errorMessage = err.error?.message || 'Update failed.'),
      });
    } else {
      this.expenseService.createExpenseForm(payload).subscribe({
        next: () => {
          this.message = 'Expense submitted successfully.';
          this.resetForm();
          this.loadForms();
        },
        error: (err) => (this.errorMessage = err.error?.message || 'Submission failed.'),
      });
    }
  }

  editForm(form: ExpenseFormResponse): void {
    this.editingFormId = form.id;
    this.currency = form.currency;
    this.items = form.items.map((i) => ({ ...i }));
  }

  cancelEdit(): void {
    this.resetForm();
  }

  resetForm(): void {
    this.editingFormId = null;
    this.currency = 'PKR';
    this.items = [{ expenseDate: '', purpose: '', category: '', amount: 0 }];
  }

  canEdit(status: string): boolean {
    return status === 'PendingApproval' || status === 'ChangeRequested';
  }

  logout(): void {
    this.auth.logout();
  }
}
