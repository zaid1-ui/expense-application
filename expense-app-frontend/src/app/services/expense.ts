import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ExpenseItem {
  expenseDate: string;
  purpose: string;
  category: string;
  amount: number;
}

export interface ExpenseFormCreate {
  currency: string;
  items: ExpenseItem[];
}

export interface ExpenseFormResponse {
  id: number;
  employeeName: string;
  currency: string;
  status: string;
  totalAmount: number;
  createdDate: string;
  rejectionReason?: string;
  items: ExpenseItem[];
}

@Injectable({
  providedIn: 'root',
})
export class Expense {
  private apiUrl = 'http://localhost:5053/api/Expense';
  private adminUrl = 'http://localhost:5053/api/Admin';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: `Bearer ${token}`,
    });
  }

  // Employee
  createExpenseForm(data: ExpenseFormCreate): Observable<any> {
    return this.http.post(`${this.apiUrl}`, data, { headers: this.getHeaders() });
  }

  getMyForms(status?: string, currency?: string): Observable<ExpenseFormResponse[]> {
    let params = '';
    if (status) params += `status=${status}&`;
    if (currency) params += `currency=${currency}`;
    return this.http.get<ExpenseFormResponse[]>(`${this.apiUrl}/my-forms?${params}`, {
      headers: this.getHeaders(),
    });
  }

  editExpenseForm(id: number, data: ExpenseFormCreate): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, data, { headers: this.getHeaders() });
  }

  // Manager
  getAwaitingApproval(currency?: string, employeeName?: string): Observable<ExpenseFormResponse[]> {
    let params = '';
    if (currency) params += `currency=${currency}&`;
    if (employeeName) params += `employeeName=${employeeName}`;
    return this.http.get<ExpenseFormResponse[]>(`${this.apiUrl}/awaiting-approval?${params}`, {
      headers: this.getHeaders(),
    });
  }

  approveForm(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/approve`, {}, { headers: this.getHeaders() });
  }

  requestChange(id: number, reason: string): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/${id}/request-change`,
      { reason },
      { headers: this.getHeaders() },
    );
  }

  // Accountant
  getToBePaid(currency?: string, employeeName?: string): Observable<ExpenseFormResponse[]> {
    let params = '';
    if (currency) params += `currency=${currency}&`;
    if (employeeName) params += `employeeName=${employeeName}`;
    return this.http.get<ExpenseFormResponse[]>(`${this.apiUrl}/to-be-paid?${params}`, {
      headers: this.getHeaders(),
    });
  }

  payForm(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/pay`, {}, { headers: this.getHeaders() });
  }

  // Admin
  getAllTransactions(status?: string, employeeName?: string): Observable<any> {
    let params = '';
    if (status) params += `status=${status}&`;
    if (employeeName) params += `employeeName=${employeeName}`;
    return this.http.get(`${this.adminUrl}/transactions?${params}`, { headers: this.getHeaders() });
  }

  getApprovalHistory(action?: string, employeeName?: string): Observable<any> {
    let params = '';
    if (action) params += `action=${action}&`;
    if (employeeName) params += `employeeName=${employeeName}`;
    return this.http.get(`${this.adminUrl}/approval-history?${params}`, {
      headers: this.getHeaders(),
    });
  }

  getReportByStatus(): Observable<any> {
    return this.http.get(`${this.adminUrl}/reports/by-status`, { headers: this.getHeaders() });
  }

  getReportByEmployee(): Observable<any> {
    return this.http.get(`${this.adminUrl}/reports/by-employee`, { headers: this.getHeaders() });
  }

  getReportByCategory(): Observable<any> {
    return this.http.get(`${this.adminUrl}/reports/by-category`, { headers: this.getHeaders() });
  }

  getMonthlyReport(): Observable<any> {
    return this.http.get(`${this.adminUrl}/reports/monthly`, { headers: this.getHeaders() });
  }
}
