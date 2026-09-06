import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();
  const authenticatedRequest = token && !request.url.endsWith('/api/auth/login')
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  const isAuthRequest = request.url.includes('/api/auth/');
  return next(authenticatedRequest).pipe(
    catchError(error => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isAuthRequest && auth.session()?.refreshToken) {
        return auth.refresh().pipe(
          switchMap(response => next(request.clone({
            setHeaders: { Authorization: `Bearer ${response.accessToken}` }
          }))),
          catchError(refreshError => {
            auth.logout();
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
