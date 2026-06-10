import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { type BllEventMeta, resolveBllEvent } from '../../data/bll-events.data';

interface EventManifest {
  images: string[];
}

@Injectable({ providedIn: 'root' })
export class BllEventsService {
  private readonly http = inject(HttpClient);
  readonly events = signal<BllEventMeta[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    void this.loadEvents();
  }

  async loadEvents(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const manifest = await firstValueFrom(
        this.http.get<EventManifest>('/event/manifest.json')
      );
      const items = (manifest.images ?? []).map((filename) => resolveBllEvent(filename));
      this.events.set(items);
    } catch {
      this.error.set('Unable to load BLL events.');
      this.events.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  getById(id: string): BllEventMeta | undefined {
    return this.events().find((e) => e.id === id);
  }
}
