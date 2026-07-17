import { ApplicationConfig } from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideClientHydration } from '@angular/platform-browser';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { routes } from './app.routes';
import { serverRoutes } from './app.routes.server';
import { adminTokenInterceptor } from './core/interceptors/admin-token.interceptor';
import { authSessionInterceptor } from './core/interceptors/auth-session.interceptor';
import { httpErrorInterceptor } from './core/interceptors/http-error.interceptor';
import { memberTokenInterceptor } from './core/interceptors/member-token.interceptor';

/**
 * Server-only application config used for static prerendering.
 *
 * Built from scratch (not via mergeApplicationConfig) to intentionally exclude
 * browser-only providers such as provideBrowserGlobalErrorListeners() and
 * withEventReplay(), which access window/document globals and throw NG0401
 * ("Browser globals are missing") in the Node.js prerender environment.
 *
 * The client bootstrap (main.ts → app.config.ts) retains all browser-specific
 * providers for the interactive Angular app that hydrates over the prerendered HTML.
 */
export const AppServerConfig: ApplicationConfig = {
  providers: [
    // SSR rendering engine + per-route render mode configuration
    provideServerRendering(withRoutes(serverRoutes)),

    // Router — identical feature set as the browser config
    provideRouter(
      routes,
      withInMemoryScrolling({
        scrollPositionRestoration: 'enabled',
        anchorScrolling: 'enabled',
      }),
    ),

    // HTTP client with the same interceptor chain used in the browser
    provideHttpClient(
      withInterceptors([
        httpErrorInterceptor,
        adminTokenInterceptor,
        memberTokenInterceptor,
        authSessionInterceptor,
      ]),
    ),

    // Hydration transfer state — writes server state that the browser app
    // reads back on first load (without withEventReplay which is browser-only)
    provideClientHydration(),
  ],
};
