export const UDYAM_PREFIX = 'UDYAM-';

export const UDYAM_COMPLETE_PATTERN = /^UDYAM-[A-Z]{2}-\d{2}-\d{7}$/;

/** User-editable suffix: XX + 00 + 0000000 (11 alphanumeric). */
export const UDYAM_SUFFIX_ALNUM_LENGTH = 11;

/** Full URN without hyphens: UDYAM + suffix (16 alphanumeric). */
export const UDYAM_TOTAL_ALNUM_LENGTH = 16;

/** Editable display max length: XX-00-0000000 */
export const UDYAM_EDITABLE_MAX_LENGTH = 13;

export type UdyamPayload = {
  state: string;
  mid: string;
  serial: string;
};

export function parseUdyamPayload(raw: string): UdyamPayload {
  let s = (raw ?? '').trim().toUpperCase();
  if (s.startsWith('UDYAM')) {
    s = s.slice(5).replace(/^-+/, '');
  }

  const chars = s.replace(/[^A-Z0-9]/g, '');
  const state: string[] = [];
  const mid: string[] = [];
  const serial: string[] = [];

  let i = 0;
  while (i < chars.length && state.length < 2) {
    const ch = chars[i];
    if (/[A-Z]/.test(ch)) state.push(ch);
    i++;
  }
  while (i < chars.length && mid.length < 2) {
    const ch = chars[i];
    if (/\d/.test(ch)) mid.push(ch);
    i++;
  }
  while (i < chars.length && serial.length < 7) {
    const ch = chars[i];
    if (/\d/.test(ch)) serial.push(ch);
    i++;
  }

  return {
    state: state.join(''),
    mid: mid.join(''),
    serial: serial.join(''),
  };
}

export function payloadCharCount(payload: UdyamPayload): number {
  return payload.state.length + payload.mid.length + payload.serial.length;
}

export function isUdyamPayloadComplete(payload: UdyamPayload): boolean {
  return payload.state.length === 2 && payload.mid.length === 2 && payload.serial.length === 7;
}

/** Map caret in editable display to 0..11 index in XX00YYYYYYY. */
export function cursorToPayloadIndex(editable: string, cursor: number): number {
  let index = 0;
  const limit = Math.max(0, Math.min(cursor, editable.length));
  for (let i = 0; i < limit; i++) {
    if (editable[i] !== '-') index++;
  }
  return index;
}

export function shouldAllowEditableChar(
  editable: string,
  cursor: number,
  char: string,
  selectionLength = 0,
): boolean {
  if (!char || char.length !== 1) return true;

  const payload = parseUdyamPayload(`${UDYAM_PREFIX}${editable}`);
  const atCapacity = payloadCharCount(payload) >= UDYAM_SUFFIX_ALNUM_LENGTH;

  if (selectionLength > 0) {
    return /[A-Z0-9]/i.test(char);
  }

  if (atCapacity && /[A-Z0-9]/i.test(char)) {
    return false;
  }

  const payloadIdx = cursorToPayloadIndex(editable, cursor);
  const upper = char.toUpperCase();

  if (/[A-Z]/.test(upper)) {
    return payloadIdx < 2 && payload.state.length < 2;
  }

  if (/\d/.test(char)) {
    return payloadIdx >= 2 || payload.state.length >= 2;
  }

  return false;
}

export function formatEditableDisplay(payload: UdyamPayload): string {
  if (!payload.state && !payload.mid && !payload.serial) return '';

  let display = payload.state;
  if (payload.state.length === 2) {
    display += `-${payload.mid}`;
    if (payload.mid.length === 2) {
      display += `-${payload.serial}`;
    }
  }
  return display;
}

export function formatUdyamFull(payload: UdyamPayload): string {
  if (!payload.state && !payload.mid && !payload.serial) return '';
  const editable = formatEditableDisplay(payload);
  return editable ? `${UDYAM_PREFIX}${editable}` : '';
}

export function formatUdyamFromRaw(raw: string): string {
  if (!(raw ?? '').trim()) return '';
  return formatUdyamFull(parseUdyamPayload(raw));
}

export function udyamToEditableDisplay(full: string): string {
  if (!(full ?? '').trim()) return '';
  return formatEditableDisplay(parseUdyamPayload(full));
}

export function applyUdyamEditableChange(
  editable: string,
  cursor: number,
): { editable: string; full: string; cursor: number } {
  const previous = editable;
  const payload = parseUdyamPayload(`${UDYAM_PREFIX}${editable}`);
  const nextEditable = formatEditableDisplay(payload);
  const full = formatUdyamFull(payload);

  let nextCursor = cursor;
  const grew = nextEditable.length > previous.length;
  const shrank = nextEditable.length < previous.length;

  if (grew && cursor >= previous.length) {
    nextCursor = nextEditable.length;
  } else if (grew) {
    nextCursor = Math.min(cursor + (nextEditable.length - previous.length), nextEditable.length);
  } else if (shrank) {
    nextCursor = Math.min(cursor, nextEditable.length);
  }

  nextCursor = Math.max(0, Math.min(nextCursor, nextEditable.length));
  return { editable: nextEditable, full, cursor: nextCursor };
}
