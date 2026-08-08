import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth, ManagerOption } from '../../services/auth';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './signup.html',
  styleUrl: './signup.css',
})
export class Signup implements OnInit {
  username = '';
  password = '';
  confirmPassword = '';
  fullName = '';
  managerId: number | null = null;

  managers: ManagerOption[] = [];
  errorMessage = '';
  submitting = false;

  constructor(
    private auth: Auth,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.auth.getManagers().subscribe({
      next: (data) => (this.managers = data),
      error: () => (this.errorMessage = 'Could not load the list of managers. Please try again later.'),
    });
  }

  onSubmit(): void {
    this.errorMessage = '';

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }
    if (!this.managerId) {
      this.errorMessage = 'Please select a manager.';
      return;
    }

    this.submitting = true;
    this.auth
      .register({
        username: this.username,
        password: this.password,
        fullName: this.fullName,
        managerId: this.managerId,
      })
      .subscribe({
        next: () => {
          // Account created — log the new employee straight in.
          this.auth.login({ username: this.username, password: this.password }).subscribe({
            next: () => this.router.navigate(['/employee']),
            error: () => {
              this.submitting = false;
              this.router.navigate(['/login']);
            },
          });
        },
        error: (err) => {
          this.submitting = false;
          this.errorMessage = err.error?.message || 'Could not create the account.';
        },
      });
  }
}
