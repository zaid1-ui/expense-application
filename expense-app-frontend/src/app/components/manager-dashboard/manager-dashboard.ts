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
  expandedFormId: number | null = null;

  constructor(
    private expenseService: Expense,
    public auth: Auth,
  ) {}

  ngOnInit(): void {
    this.loadForms();
  }

  loadForms(): void {
    this.expenseService.getAwaitingApproval(this.filterCurrency, this.filterEmployee).subscribe({
      next: (data) => (this.forms = data),
      error: () => (this.errorMessage = 'Failed to load forms.'),
    });
  }

  toggleDetails(id: number): void {
    this.expandedFormId = this.expandedFormId === id ? null : id;
  }

  approve(id: number): void {
    this.expenseService.approveForm(id).subscribe({
      next: () => {
        this.message = 'Form approved.';
        this.loadForms();
      },
      error: (err) => (this.errorMessage = err.error?.message || 'Approval failed.'),
    });
  }

  requestChange(id: number): void {
    const reason = this.reasonInputs[id];
    if (!reason || !reason.trim()) {
      this.errorMessage = 'Reason is required to request a change.';
      return;
    }
    this.expenseService.requestChange(id, reason).subscribe({
      next: () => {
        this.message = 'Change requested.';
        this.loadForms();
      },
      error: (err) => (this.errorMessage = err.error?.message || 'Request failed.'),
    });
  }

  logout(): void {
    this.auth.logout();
  }
}
