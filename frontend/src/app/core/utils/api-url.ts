import { environment } from '../../../environments/environment';

/**
 * Strip a leading /api segment so paths work whether callers pass
 * '/user/login' or '/api/user/login' when apiBaseUrl already ends with /api.
 */
function stripApiPrefix(path: string): string {
  const p = path.startsWith('/') ? path : `/${path}`;
  if (p === '/api') return '/';
  if (p.startsWith('/api/')) return p.slice('/api'.length);
  return p;
}

/**
 * Build request URL from environment.apiBaseUrl and an API route.
 * Local: apiBaseUrl='/api' + '/user/login' or '/api/user/login' => '/api/user/login'.
 */
export function apiUrl(path: string): string {
  const base = environment.apiBaseUrl.replace(/\/$/, '');
  const route = stripApiPrefix(path);
  const suffix = route.startsWith('/') ? route : `/${route}`;

  if (!base) {
    return `/api${suffix}`;
  }

  if (base.endsWith('/api')) {
    return `${base}${suffix}`;
  }

  if (base.includes('://')) {
    return `${base}${suffix}`;
  }

  return `${base}/api${suffix}`;
}

/** Pathname for interceptors (works with relative and absolute request URLs). */
export function getApiPath(url: string): string {
  if (url.includes('://')) {
    try {
      return new URL(url).pathname;
    } catch {
      return url.split('?')[0] ?? url;
    }
  }
  return url.split('?')[0] ?? url;
}

export function isApiRequest(url: string): boolean {
  const path = getApiPath(url);
  return path.includes('/api/') || path.endsWith('/api');
}

/** True when the request should carry the admin JWT (admin panel APIs). */
export function isAdminApiRequest(url: string): boolean {
  const path = getApiPath(url);
  return (
    path.startsWith('/api/admin') ||
    path.startsWith('/api/blogs/admin') ||
    path.startsWith('/api/blog-categories/admin') ||
    path.startsWith('/api/blog-badges/admin') ||
    path.startsWith('/api/events/admin') ||
    path.startsWith('/api/event-categories/admin') ||
    path.startsWith('/api/event-cities/admin') ||
    path.startsWith('/api/schemes/admin') ||
    path.startsWith('/api/scheme-categories/admin') ||
    path.startsWith('/api/team-members/admin') ||
    path.startsWith('/api/plans/admin')
  );
}
