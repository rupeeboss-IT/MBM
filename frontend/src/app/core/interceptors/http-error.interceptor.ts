import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { getHttpErrorMessage } from '../utils/http-error-message';
import { isApiRequest } from '../utils/api-url';
/**
 * Ensures all /api/* failures expose only user-friendly `error.error.message` values.
 */
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApiRequest(req.url)) {
    return next(req);
  }

  return next(req).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse)) {
        return throwError(() => err);
      }

      const friendly = getHttpErrorMessage(err, undefined, {
        url: err.url ?? req.url,
        method: req.method,
      });

      const existing =
        err.error && typeof err.error === 'object' && err.error !== null
          ? (err.error as Record<string, unknown>)
          : {};

      return throwError(
        () =>
          new HttpErrorResponse({
            error: { ...existing, success: existing['success'] ?? false, message: friendly },
            headers: err.headers,
            status: err.status,
            statusText: err.statusText,
            url: err.url ?? undefined,
          })
      );
    })
  );
};
