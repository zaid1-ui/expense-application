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

  constructor(
    private auth: Auth,
    private router: Router,
  ) {}

  onSubmit(): void {
    this.errorMessage = '';
    this.auth.login({ username: this.username, password: this.password }).subscribe({
      next: (res) => {
        const role = res.role;
        if (role === 'Employee') this.router.navigate(['/employee']);
        else if (role === 'Manager') this.router.navigate(['/manager']);
        else if (role === 'Accountant') this.router.navigate(['/accountant']);
        else if (role === 'Admin') this.router.navigate(['/admin']);
      },
      error: () => {
        this.errorMessage = 'Invalid username or password.';
      },
    });
  }
}
