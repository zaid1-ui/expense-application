import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Expense, ExpenseFormResponse, ExpenseItem } from '../../services/expense';
import { Auth } from '../../services/auth';

const EMPTY_ITEM = (): ExpenseItem => ({ expenseDate: '', purpose: '', category: '', amount: 0 });

@Component({
  selector: 'app-employee-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employee-dashboard.html',
  styleUrl: './employee-dashboard.css',
})
export class EmployeeDashboard implements OnInit {
  categories = ['Taxi', 'Food', 'Gas', 'Hotel', 'Transport', 'Office Supplies', 'Other'];
  maxAmount = 5000;
  today = new Date().toISOString().slice(0, 10);

  forms: ExpenseFormResponse[] = [];
  currency = 'PKR';
  items: ExpenseItem[] = [EMPTY_ITEM()];
  message = '';
  errorMessage = '';
  loading = false;
  submitting = false;
  submitAttempted = false;

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

  get pendingCount(): number {
    return this.forms.filter((f) => f.status === 'PendingApproval').length;
  }

  get changeRequestedCount(): number {
    return this.forms.filter((f) => f.status === 'ChangeRequested').length;
  }

  get approvedCount(): number {
    return this.forms.filter((f) => f.status === 'Approved').length;
  }

  get paidCount(): number {
    return this.forms.filter((f) => f.status === 'Paid').length;
  }

  addItem(): void {
    this.items.push(EMPTY_ITEM());
  }

  removeItem(index: number): void {
    this.items.splice(index, 1);
  }

  loadForms(): void {
    this.loading = true;
    this.expenseService.getMyForms(this.filterStatus, this.filterCurrency).subscribe({
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

  // ---- Field-level validation, used for both live highlighting and submit gating ----

  isDateInvalid(item: ExpenseItem): boolean {
    return !item.expenseDate || item.expenseDate > this.today;
  }

  isPurposeInvalid(item: ExpenseItem): boolean {
    return !item.purpose || !item.purpose.trim();
  }

  isCategoryInvalid(item: ExpenseItem): boolean {
    return !item.category;
  }

  isAmountInvalid(item: ExpenseItem): boolean {
    return !item.amount || item.amount <= 0 || item.amount > this.maxAmount;
  }

  private firstValidationError(): string | null {
    if (!this.currency) return 'Please select a currency.';
    if (this.items.length === 0) return 'Add at least one expense item.';

    for (const item of this.items) {
      if (this.isDateInvalid(item)) {
        return item.expenseDate
          ? 'Expense date cannot be in the future.'
          : 'Every expense item needs a date.';
      }
      if (this.isPurposeInvalid(item)) return 'Every expense item needs a purpose.';
      if (this.isCategoryInvalid(item)) return 'Every expense item needs a category.';
      if (this.isAmountInvalid(item)) {
        return !item.amount || item.amount <= 0
          ? 'Every expense amount must be greater than 0.'
          : `Each expense amount must not exceed ${this.maxAmount}.`;
      }
    }
    return null;
  }

  get formInvalid(): boolean {
    return this.firstValidationError() !== null;
  }

  submitForm(): void {
    this.errorMessage = '';
    this.message = '';
    this.submitAttempted = true;

    const validationError = this.firstValidationError();
    if (validationError) {
      this.errorMessage = validationError;
      return;
    }

    const payload = {
      currency: this.currency,
      items: this.items.map((i) => ({ ...i, purpose: i.purpose.trim() })),
    };
    this.submitting = true;

    if (this.editingFormId) {
      this.expenseService.editExpenseForm(this.editingFormId, payload).subscribe({
        next: () => {
          this.message = 'Expense updated successfully.';
          this.submitting = false;
          this.resetForm();
          this.loadForms();
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err.error?.message || 'Update failed.';
        },
      });
    } else {
      this.expenseService.createExpenseForm(payload).subscribe({
        next: () => {
          this.message = 'Expense submitted successfully.';
          this.submitting = false;
          this.resetForm();
          this.loadForms();
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err.error?.message || 'Submission failed.';
        },
      });
    }
  }

  editForm(form: ExpenseFormResponse): void {
    this.editingFormId = form.id;
    this.currency = form.currency;
    this.items = form.items.map((i) => ({ ...i }));
    this.message = '';
    this.errorMessage = '';
  }

  cancelEdit(): void {
    this.resetForm();
  }

  resetForm(): void {
    this.editingFormId = null;
    this.currency = 'PKR';
    this.items = [EMPTY_ITEM()];
    this.submitAttempted = false;
  }

  canEdit(status: string): boolean {
    return status === 'PendingApproval' || status === 'ChangeRequested';
  }

  statusLabel(status: string): string {
    return status === 'PendingApproval'
      ? 'Pending Approval'
      : status === 'ChangeRequested'
        ? 'Change Requested'
        : status;
  }

  statusClass(status: string): string {
    return 'status-badge status-' + status.toLowerCase();
  }

  logout(): void {
    this.auth.logout();
  }
}
