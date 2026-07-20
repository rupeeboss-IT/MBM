import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { adminTokenInterceptor } from './core/interceptors/admin-token.interceptor';
import { authSessionInterceptor } from './core/interceptors/auth-session.interceptor';
import { httpErrorInterceptor } from './core/interceptors/http-error.interceptor';
import { memberTokenInterceptor } from './core/interceptors/member-token.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(
      withFetch(),
      withInterceptors([
        httpErrorInterceptor,
        adminTokenInterceptor,
        memberTokenInterceptor,
        authSessionInterceptor,
      ])
    ),
    provideRouter(
      routes,
      withInMemoryScrolling({
        scrollPositionRestoration: 'enabled',
        anchorScrolling: 'enabled',
      })
    ),
    provideClientHydration(withEventReplay())
  ]
};
