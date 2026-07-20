/** Admin auth pages — no sidebar shell. */
const ADMIN_AUTH_PREFIXES = ['/admin-login', '/admin-forgot-password'] as const;

/** Logged-in admin app routes that use the sidebar shell. */
const ADMIN_SHELL_PREFIXES = ['/admin-dashboard', '/admin-reports', '/admin/'] as const;

export function isAdminAuthRoute(url: string): boolean {
  const path = normalizePath(url);
  return ADMIN_AUTH_PREFIXES.some(
    (prefix) => path === prefix || path.startsWith(`${prefix}/`),
  );
}

export function isAdminShellRoute(url: string): boolean {
  const path = normalizePath(url);
  if (isAdminAuthRoute(path)) return false;
  return ADMIN_SHELL_PREFIXES.some(
    (prefix) => path === prefix || path.startsWith(`${prefix}/`) || path.startsWith(prefix),
  );
}

function normalizePath(url: string): string {
  return (url.split('?')[0]?.split('#')[0] ?? '/').trim() || '/';
}
