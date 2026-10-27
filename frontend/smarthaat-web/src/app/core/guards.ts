import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.signedIn) return true;
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const roleGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const roles = route.data['roles'] as string[] | undefined;
  return !!roles?.some(role => auth.hasRole(role)) || inject(Router).createUrlTree(['/']);
};
