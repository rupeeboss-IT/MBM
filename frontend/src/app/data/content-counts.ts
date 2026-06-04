import { ARTICLES_DATA } from './articles.data';
import { EVENTS_DATA } from './events.data';
import { SCHEMES_DATA } from './schemes.data';

/** Counts for static site content shown on the admin dashboard. */
export const CONTENT_COUNTS = {
  blogs: Object.keys(ARTICLES_DATA).length,
  events: Object.keys(EVENTS_DATA).length,
  schemes: Object.keys(SCHEMES_DATA).length,
} as const;
