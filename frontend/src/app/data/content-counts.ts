import { EVENTS_DATA } from './events.data';
import { SCHEMES_DATA } from './schemes.data';

/**
 * Counts for static site content shown on the admin dashboard.
 * Blogs are now stored in the database — their count comes from the API.
 */
export const CONTENT_COUNTS = {
  events: Object.keys(EVENTS_DATA).length,
  schemes: Object.keys(SCHEMES_DATA).length,
} as const;
