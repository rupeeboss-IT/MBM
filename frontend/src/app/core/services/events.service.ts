import { Injectable } from '@angular/core';
import { EVENTS_DATA, type EventModel, type EventSlug } from '../../data/events.data';

@Injectable({ providedIn: 'root' })
export class EventsService {
  getEventBySlug(slug: string): EventModel | null {
    return (EVENTS_DATA as Record<string, EventModel>)[slug] ?? null;
  }

  getAllEvents(): Array<{ slug: EventSlug; data: EventModel }> {
    return (Object.keys(EVENTS_DATA) as EventSlug[]).map((slug) => ({
      slug,
      data: EVENTS_DATA[slug]
    }));
  }
}

