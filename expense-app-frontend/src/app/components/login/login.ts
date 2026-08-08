import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  username = '';
  password = '';
  errorMessage = '';
  submitting = false;
  submitAttempted = false;

  constructor(
    private auth: Auth,
    private router: Router,
  ) {}

  onSubmit(): void {
    this.errorMessage = '';
    this.submitAttempted = true;

    if (!this.username.trim() || !this.password) {
      this.errorMessage = 'Please enter both username and password.';
      return;
    }

    this.submitting = true;
    this.auth.login({ username: this.username.trim(), password: this.password }).subscribe({
      next: (res) => {
        const role = res.role;
        if (role === 'Employee') this.router.navigate(['/employee']);
        else if (role === 'Manager') this.router.navigate(['/manager']);
        else if (role === 'Accountant') this.router.navigate(['/accountant']);
        else if (role === 'Admin') this.router.navigate(['/admin']);
      },
      error: () => {
        this.submitting = false;
        this.errorMessage = 'Invalid username or password.';
      },
    });
  }
}
