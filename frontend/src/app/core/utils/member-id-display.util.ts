const MEMBER_PREFIX = 'MBM';
const PARTNER_PREFIX = 'PMBM';

export type MemberPartnerRole = 'member' | 'partner' | string | null | undefined;

export function getIdLabel(role: MemberPartnerRole): string {
  const r = (role ?? '').toLowerCase().trim();
  return r === 'partner' ? 'Partner ID' : 'Member ID';
}

/**
 * Formats the identifier shown to the user.
 *
 * Rules:
 * - Members show MBMxxxxx
 * - Partners show PMBMxxxxx
 * - If data is already in the correct prefix, it is preserved.
 * - If prefixes are mixed in the database, the output is corrected for display only.
 */
export function formatMemberPartnerId(memberId: string | null | undefined, role: MemberPartnerRole): string | null {
  if (!memberId) return null;

  const raw = memberId.trim().toUpperCase();
  if (!raw) return null;

  const r = (role ?? '').toLowerCase().trim();

  if (r === 'partner') {
    if (raw.startsWith(PARTNER_PREFIX)) return raw;
    if (raw.startsWith(MEMBER_PREFIX)) return `${PARTNER_PREFIX}${raw.substring(MEMBER_PREFIX.length)}`;
    return raw;
  }

  // Default to Member display.
  if (raw.startsWith(MEMBER_PREFIX)) return raw;
  if (raw.startsWith(PARTNER_PREFIX)) return `${MEMBER_PREFIX}${raw.substring(PARTNER_PREFIX.length)}`;
  return raw;
}

