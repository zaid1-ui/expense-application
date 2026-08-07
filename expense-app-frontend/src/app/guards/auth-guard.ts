import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from '../services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(Auth);
  const router = inject(Router);

  if (auth.isLoggedIn()) {
    // Optional: role-based route protection
    const allowedRoles = route.data?.['roles'] as string[] | undefined;
    if (allowedRoles && !allowedRoles.includes(auth.getRole() || '')) {
      router.navigate(['/login']);
      return false;
    }
    return true;
  }

  router.navigate(['/login']);
  return false;
};
