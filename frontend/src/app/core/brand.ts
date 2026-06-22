/** Public path to MBM logo (file: frontend/public/assets/mbmlogo.png). */
export const MBM_LOGO_SRC = '/assets/mbmlogo.png';
export const MBM_LOGO_ALT = 'MSME Bharat Manch';

/** WhatsApp Business number (E.164 without +, for wa.me links). */
export const MBM_WHATSAPP_NUMBER = '917977724440';

/** Human-readable WhatsApp display (7977724440). */
export const MBM_WHATSAPP_DISPLAY = '+91 79777 24440';

export function mbmWhatsAppUrl(message?: string): string {
  const base = `https://wa.me/${MBM_WHATSAPP_NUMBER}`;
  return message ? `${base}?text=${encodeURIComponent(message)}` : base;
}

/** Public path to Infomerics Ratings logo (file: frontend/public/assets/Infomerics_ratings_logo.png). */
export const INFOMERICS_RATINGS_LOGO_SRC = '/assets/Infomerics_ratings_logo.png';
export const INFOMERICS_RATINGS_LOGO_ALT = 'Infomerics Ratings';
