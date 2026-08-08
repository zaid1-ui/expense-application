import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Expense, ExpenseFormResponse } from '../../services/expense';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-manager-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manager-dashboard.html',
  styleUrl: './manager-dashboard.css',
})
export class ManagerDashboard implements OnInit {
  forms: ExpenseFormResponse[] = [];
  filterCurrency = '';
  filterEmployee = '';
  message = '';
  errorMessage = '';
  reasonInputs: { [id: number]: string } = {};
  reasonMissing: { [id: number]: boolean } = {};
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
    this.expenseService.getAwaitingApproval(this.filterCurrency, this.filterEmployee).subscribe({
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

  approve(id: number): void {
    if (this.isBusy(id)) return;
    this.errorMessage = '';
    this.busyIds.add(id);
    this.expenseService.approveForm(id).subscribe({
      next: () => {
        this.message = 'Form approved.';
        this.busyIds.delete(id);
        this.loadForms();
      },
      error: (err) => {
        this.busyIds.delete(id);
        this.errorMessage = err.error?.message || 'Approval failed.';
      },
    });
  }

  requestChange(id: number): void {
    if (this.isBusy(id)) return;
    this.errorMessage = '';
    const reason = (this.reasonInputs[id] || '').trim();

    if (!reason) {
      this.reasonMissing[id] = true;
      this.errorMessage = 'Reason is required to request a change.';
      return;
    }
    this.reasonMissing[id] = false;

    this.busyIds.add(id);
    this.expenseService.requestChange(id, reason).subscribe({
      next: () => {
        this.message = 'Change requested.';
        this.busyIds.delete(id);
        this.loadForms();
      },
      error: (err) => {
        this.busyIds.delete(id);
        this.errorMessage = err.error?.message || 'Request failed.';
      },
    });
  }

  logout(): void {
    this.auth.logout();
  }
}
