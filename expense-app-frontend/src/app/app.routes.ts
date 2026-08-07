import { Routes } from '@angular/router';
import { Login } from './components/login/login';
import { EmployeeDashboard } from './components/employee-dashboard/employee-dashboard';
import { ManagerDashboard } from './components/manager-dashboard/manager-dashboard';
import { AccountantDashboard } from './components/accountant-dashboard/accountant-dashboard';
import { AdminDashboard } from './components/admin-dashboard/admin-dashboard';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: Login },
  {
    path: 'employee',
    component: EmployeeDashboard,
    canActivate: [authGuard],
    data: { roles: ['Employee'] },
  },
  {
    path: 'manager',
    component: ManagerDashboard,
    canActivate: [authGuard],
    data: { roles: ['Manager'] },
  },
  {
    path: 'accountant',
    component: AccountantDashboard,
    canActivate: [authGuard],
    data: { roles: ['Accountant'] },
  },
  {
    path: 'admin',
    component: AdminDashboard,
    canActivate: [authGuard],
    data: { roles: ['Admin'] },
  },
];
