import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiService } from '../services/api.service';

export const AuthInterceptor: HttpInterceptorFn = (request, next) => {
  const apiService = inject(ApiService);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 || error.status === 403) {
        console.log('Authentication error detected, logging out user');
        apiService.logout();
        
        // Force page reload to reset app state
        if (typeof window !== 'undefined') {
          window.location.reload();
        }
      }
      return throwError(() => error);
    })
  );
}; 