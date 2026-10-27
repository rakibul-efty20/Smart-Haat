import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).token();
  const headers = token ? request.headers.set('Authorization', `Bearer ${token}`) : request.headers;
  return next(request.clone({ headers, withCredentials: true }));
};
