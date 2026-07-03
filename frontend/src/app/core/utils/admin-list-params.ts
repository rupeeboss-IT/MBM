/** Default page size for all admin paginated data grids. */
export const ADMIN_DEFAULT_PAGE_SIZE = 100;

export type AdminListQueryOpts = {
  days?: number;
  page?: number;
  pageSize?: number;
  search?: string;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  status?: string;
  export?: boolean;
};

export function appendAdminListParams(
  params: Record<string, string>,
  opts?: AdminListQueryOpts,
): Record<string, string> {
  if (!opts) return params;
  if (opts.days != null) params['days'] = String(opts.days);
  if (opts.page != null) params['page'] = String(opts.page);
  if (opts.pageSize != null) params['pageSize'] = String(opts.pageSize);
  if (opts.search?.trim()) params['search'] = opts.search.trim();
  if (opts.dateFrom?.trim()) params['dateFrom'] = opts.dateFrom.trim();
  if (opts.dateTo?.trim()) params['dateTo'] = opts.dateTo.trim();
  if (opts.sortBy?.trim()) params['sortBy'] = opts.sortBy.trim();
  if (opts.sortDir) params['sortDir'] = opts.sortDir;
  if (opts.status?.trim()) params['status'] = opts.status.trim();
  if (opts.export) params['export'] = 'true';
  return params;
}

export function sortIndicator(
  sortBy: string,
  sortDir: 'asc' | 'desc',
  column: string,
): string {
  if (sortBy !== column) return '';
  return sortDir === 'asc' ? ' ▲' : ' ▼';
}

export function toggleColumnSort(
  sortBy: string,
  sortDir: 'asc' | 'desc',
  column: string,
): { sortBy: string; sortDir: 'asc' | 'desc' } {
  if (sortBy === column) {
    return { sortBy: column, sortDir: sortDir === 'asc' ? 'desc' : 'asc' };
  }
  return { sortBy: column, sortDir: 'asc' };
}
