/**
 * Parse API date strings as server local clock time (GETDATE / DateTime.Now).
 * Avoids browser timezone shifts on values that have no timezone suffix.
 */
export function parseApiLocalDate(value: string | Date | null | undefined): Date | null {
  if (value == null || value === '') return null;
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }

  const raw = value.trim();
  if (!raw) return null;

  const withoutZ = raw.endsWith('Z') ? raw.slice(0, -1) : raw;
  const withoutMs = withoutZ.replace(/\.\d+$/, '');
  const match = withoutMs.match(/^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2})(?::(\d{2}))?)?/);
  if (match) {
    const year = Number(match[1]);
    const month = Number(match[2]) - 1;
    const day = Number(match[3]);
    const hour = Number(match[4] ?? 0);
    const minute = Number(match[5] ?? 0);
    const second = Number(match[6] ?? 0);
    const d = new Date(year, month, day, hour, minute, second);
    return Number.isNaN(d.getTime()) ? null : d;
  }

  const fallback = new Date(raw);
  return Number.isNaN(fallback.getTime()) ? null : fallback;
}

/** Compare API local date string to current moment (membership expiry checks). */
export function isApiLocalDateOnOrBeforeNow(value: string | null | undefined): boolean {
  const d = parseApiLocalDate(value);
  if (!d) return false;
  return d.getTime() <= Date.now();
}
