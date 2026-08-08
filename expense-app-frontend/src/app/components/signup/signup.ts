import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth, ManagerOption } from '../../services/auth';

const USERNAME_PATTERN = /^[a-zA-Z0-9_]{3,20}$/;

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
  submitAttempted = false;
  showPassword = false;
  showConfirmPassword = false;

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

  get usernameInvalid(): boolean {
    return !USERNAME_PATTERN.test(this.username.trim());
  }

  get fullNameInvalid(): boolean {
    return this.fullName.trim().length < 2;
  }

  get passwordInvalid(): boolean {
    return this.password.length < 6;
  }

  get confirmPasswordInvalid(): boolean {
    return this.confirmPassword !== this.password;
  }

  get managerInvalid(): boolean {
    return !this.managerId;
  }

  private firstValidationError(): string | null {
    if (this.fullNameInvalid) return 'Please enter your full name.';
    if (this.usernameInvalid) {
      return 'Username must be 3-20 characters: letters, numbers, and underscores only.';
    }
    if (this.managerInvalid) return 'Please select a manager.';
    if (this.passwordInvalid) return 'Password must be at least 6 characters.';
    if (this.confirmPasswordInvalid) return 'Passwords do not match.';
    return null;
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  onSubmit(): void {
    this.errorMessage = '';
    this.submitAttempted = true;

    const validationError = this.firstValidationError();
    if (validationError) {
      this.errorMessage = validationError;
      return;
    }

    this.submitting = true;
    this.auth
      .register({
        username: this.username.trim(),
        password: this.password,
        fullName: this.fullName.trim(),
        managerId: this.managerId!,
      })
      .subscribe({
        next: () => {
          // Account created — log the new employee straight in.
          this.auth.login({ username: this.username.trim(), password: this.password }).subscribe({
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
