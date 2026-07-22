import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  HostListener,
  OnInit,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ImageLightbox } from '../../core/components/image-lightbox/image-lightbox';
import {
  EventCategoryService,
  type EventCategoryItem,
} from '../../core/services/event-category.service';
import { EventsService, type PublicEvent } from '../../core/services/events.service';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';

export interface EventYearGroup {
  year: string;
  events: PublicEvent[];
}

export interface TimelineDateParts {
  day: string;
  month: string;
}

export interface CalendarDayCell {
  date: Date;
  label: number;
  isToday: boolean;
  isSelected: boolean;
  isInRange: boolean;
  isRangeStart: boolean;
  isRangeEnd: boolean;
  isDisabled: boolean;
  isOutsideMonth: boolean;
}

@Component({
  selector: 'app-events',
  imports: [CommonModule, RouterLink, ImageLightbox],
  templateUrl: './events.html',
  styleUrl: './events.css',
})
export class Events implements OnInit {
  private readonly eventsApi = inject(EventsService);
  private readonly categoryApi = inject(EventCategoryService);
  private searchDebounceTimer?: ReturnType<typeof setTimeout>;
  private readonly datePickerRoot = viewChild<ElementRef<HTMLElement>>('datePickerRoot');

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly events = signal<PublicEvent[]>([]);
  readonly categories = signal<EventCategoryItem[]>([]);
  readonly activeCategory = signal<string>('all');
  readonly searchQuery = signal('');
  readonly dateFrom = signal<Date | null>(null);
  readonly dateTo = signal<Date | null>(null);
  readonly calendarOpen = signal(false);
  readonly calendarViewYear = signal(new Date().getFullYear());
  readonly calendarViewMonth = signal(new Date().getMonth());
  readonly calendarFocusedDay = signal<number | null>(null);

  readonly lightboxOpen = signal(false);
  readonly lightboxSrc = signal<string | null>(null);
  readonly lightboxAlt = signal('');

  readonly calendarWeekdayLabels = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];

  async ngOnInit() {
    await Promise.all([this.loadFilters(), this.loadEvents()]);
  }

  private async loadFilters() {
    try {
      const catRes = await firstValueFrom(this.categoryApi.listPublic(true));
      const cats = catRes.categories ?? [];
      this.categories.set(
        cats.length
          ? cats
          : [{ eventCategoryId: 0, slug: 'bll', name: 'BLL', sortOrder: 1, isActive: true, showInFilter: true }],
      );
    } catch {
      this.categories.set([
        { eventCategoryId: 0, slug: 'bll', name: 'BLL', sortOrder: 1, isActive: true, showInFilter: true },
      ]);
    }
  }

  async loadEvents() {
    try {
      this.loading.set(true);
      this.error.set(null);
      const search = this.searchQuery().trim();
      const res = await firstValueFrom(
        this.eventsApi.getPublished({
          category: this.activeCategory() === 'all' ? undefined : this.activeCategory(),
          search: search || undefined,
          page: 1,
          pageSize: 100,
        }),
      );
      this.events.set(res.events);
    } catch (e: unknown) {
      this.error.set(getHttpErrorMessage(e, 'Failed to load events.'));
      this.events.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  setCategory(slug: string) {
    this.activeCategory.set(slug);
    void this.loadEvents();
  }

  onSearchInput(value: string) {
    this.searchQuery.set(value);
    clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => void this.loadEvents(), 300);
  }

  toggleCalendar() {
    if (this.calendarOpen()) {
      this.closeCalendar();
      return;
    }
    const anchor = this.dateFrom() ?? new Date();
    this.calendarViewYear.set(anchor.getFullYear());
    this.calendarViewMonth.set(anchor.getMonth());
    this.calendarFocusedDay.set(anchor.getDate());
    this.calendarOpen.set(true);
  }

  closeCalendar() {
    this.calendarOpen.set(false);
  }

  clearDateFilter(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.dateFrom.set(null);
    this.dateTo.set(null);
    this.calendarFocusedDay.set(null);
    this.closeCalendar();
  }

  dateDisplayLabel(): string {
    const from = this.dateFrom();
    if (!from) {
      return 'Select Date';
    }
    const to = this.dateTo();
    if (!to || this.normalizeDate(from).getTime() === this.normalizeDate(to).getTime()) {
      return this.formatDisplayDate(from);
    }
    return `${this.formatDisplayDate(from)} – ${this.formatDisplayDate(to)}`;
  }

  calendarMonthLabel(): string {
    const date = new Date(this.calendarViewYear(), this.calendarViewMonth(), 1);
    return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }

  calendarWeeks(): CalendarDayCell[][] {
    const year = this.calendarViewYear();
    const month = this.calendarViewMonth();
    const firstOfMonth = new Date(year, month, 1);
    const startOffset = firstOfMonth.getDay();
    const gridStart = new Date(year, month, 1 - startOffset);

    const weeks: CalendarDayCell[][] = [];
    let cursor = new Date(gridStart);

    for (let week = 0; week < 6; week++) {
      const days: CalendarDayCell[] = [];
      for (let day = 0; day < 7; day++) {
        days.push(this.buildCalendarDay(new Date(cursor)));
        cursor.setDate(cursor.getDate() + 1);
      }
      weeks.push(days);
    }

    return weeks;
  }

  selectCalendarDate(date: Date) {
    if (this.isCalendarDayDisabled(date)) {
      return;
    }

    const normalized = this.normalizeDate(date);
    const from = this.dateFrom();
    const to = this.dateTo();

    if (!from || to) {
      this.dateFrom.set(normalized);
      this.dateTo.set(null);
    } else {
      this.dateTo.set(normalized);
      this.closeCalendar();
    }

    this.calendarFocusedDay.set(normalized.getDate());
  }

  isCalendarDayDisabled(date: Date): boolean {
    const from = this.dateFrom();
    const to = this.dateTo();
    if (from && !to) {
      return this.normalizeDate(date).getTime() < this.normalizeDate(from).getTime();
    }
    return false;
  }

  prevCalendarMonth() {
    const month = this.calendarViewMonth();
    const year = this.calendarViewYear();
    if (month === 0) {
      this.calendarViewMonth.set(11);
      this.calendarViewYear.set(year - 1);
    } else {
      this.calendarViewMonth.set(month - 1);
    }
  }

  nextCalendarMonth() {
    const month = this.calendarViewMonth();
    const year = this.calendarViewYear();
    if (month === 11) {
      this.calendarViewMonth.set(0);
      this.calendarViewYear.set(year + 1);
    } else {
      this.calendarViewMonth.set(month + 1);
    }
  }

  onCalendarDayKeydown(event: KeyboardEvent, cell: CalendarDayCell) {
    if (cell.isDisabled) {
      return;
    }

    const delta: Record<string, number> = {
      ArrowLeft: -1,
      ArrowRight: 1,
      ArrowUp: -7,
      ArrowDown: 7,
    };
    const dayDelta = delta[event.key];

    if (dayDelta != null) {
      event.preventDefault();
      const next = new Date(cell.date);
      next.setDate(next.getDate() + dayDelta);
      this.calendarViewYear.set(next.getFullYear());
      this.calendarViewMonth.set(next.getMonth());
      this.calendarFocusedDay.set(next.getDate());
      return;
    }

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.selectCalendarDate(cell.date);
    }
  }

  isCalendarDayFocused(cell: CalendarDayCell): boolean {
    return (
      this.calendarFocusedDay() === cell.date.getDate() &&
      this.calendarViewMonth() === cell.date.getMonth() &&
      this.calendarViewYear() === cell.date.getFullYear()
    );
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.calendarOpen()) {
      return;
    }
    const root = this.datePickerRoot()?.nativeElement;
    if (root && !root.contains(event.target as Node)) {
      this.closeCalendar();
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey() {
    this.closeCalendar();
  }

  filteredEvents(): PublicEvent[] {
    const from = this.dateFrom();
    if (!from) {
      return this.events();
    }

    const to = this.dateTo() ?? from;
    const minTs = Math.min(this.normalizeDate(from).getTime(), this.normalizeDate(to).getTime());
    const maxTs = Math.max(this.normalizeDate(from).getTime(), this.normalizeDate(to).getTime());

    return this.events().filter((ev) => {
      const eventTs = this.eventDateTimestamp(ev);
      if (eventTs == null) {
        return false;
      }
      return eventTs >= minTs && eventTs <= maxTs;
    });
  }

  groupedEventsByYear(): EventYearGroup[] {
    const sorted = [...this.filteredEvents()].sort(
      (a, b) => this.eventSortKey(b) - this.eventSortKey(a),
    );
    const groups = new Map<string, PublicEvent[]>();

    for (const ev of sorted) {
      const year = this.eventYear(ev);
      const bucket = groups.get(year);
      if (bucket) {
        bucket.push(ev);
      } else {
        groups.set(year, [ev]);
      }
    }

    return Array.from(groups.entries())
      .sort(([yearA], [yearB]) => Number(yearB) - Number(yearA))
      .map(([year, events]) => ({ year, events }));
  }

  timelineDateParts(ev: PublicEvent): TimelineDateParts | null {
    if (ev.dateISO) {
      const parsed = new Date(`${ev.dateISO}T00:00:00`);
      if (!Number.isNaN(parsed.getTime())) {
        return {
          day: String(parsed.getDate()).padStart(2, '0'),
          month: parsed.toLocaleString('en-US', { month: 'short' }).toUpperCase(),
        };
      }
    }

    const match = ev.date.match(/(\d{1,2})(?:st|nd|rd|th)?\s+([A-Za-z]{3,})/i);
    if (match) {
      return {
        day: match[1].padStart(2, '0'),
        month: match[2].slice(0, 3).toUpperCase(),
      };
    }

    return null;
  }

  eventAddress(ev: PublicEvent): string {
    return ev.location?.trim() || ev.venue?.trim() || '';
  }

  metaLine(ev: PublicEvent): string {
    const parts = [ev.date, ev.time, ev.location].filter((p) => !!p?.trim());
    return parts.join(' · ');
  }

  openLightbox(src: string, alt: string, event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.lightboxSrc.set(src);
    this.lightboxAlt.set(alt);
    this.lightboxOpen.set(true);
  }

  closeLightbox() {
    this.lightboxOpen.set(false);
  }

  private eventSortKey(ev: PublicEvent): number {
    if (ev.dateISO) {
      const parsed = new Date(`${ev.dateISO}T00:00:00`);
      if (!Number.isNaN(parsed.getTime())) {
        return parsed.getTime();
      }
    }

    const yearMatch = ev.date.match(/\b(20\d{2})\b/);
    const dayMatch = ev.date.match(/(\d{1,2})(?:st|nd|rd|th)?/);
    const monthMatch = ev.date.match(
      /\b(Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\b/i,
    );

    const year = yearMatch ? Number(yearMatch[1]) : 0;
    const month = monthMatch ? this.monthIndex(monthMatch[1]) : 0;
    const day = dayMatch ? Number(dayMatch[1]) : 0;

    return year * 10000 + month * 100 + day;
  }

  private eventYear(ev: PublicEvent): string {
    if (ev.dateISO) {
      const parsed = new Date(`${ev.dateISO}T00:00:00`);
      if (!Number.isNaN(parsed.getTime())) {
        return String(parsed.getFullYear());
      }
    }

    const yearMatch = ev.date.match(/\b(20\d{2})\b/);
    return yearMatch ? yearMatch[1] : 'Upcoming';
  }

  private monthIndex(label: string): number {
    const normalized = label.slice(0, 3).toLowerCase();
    const months = ['jan', 'feb', 'mar', 'apr', 'may', 'jun', 'jul', 'aug', 'sep', 'oct', 'nov', 'dec'];
    const index = months.indexOf(normalized);
    return index >= 0 ? index + 1 : 0;
  }

  private buildCalendarDay(date: Date): CalendarDayCell {
    const normalized = this.normalizeDate(date);
    const from = this.dateFrom();
    const to = this.dateTo();
    const fromTs = from ? this.normalizeDate(from).getTime() : null;
    const toTs = to ? this.normalizeDate(to).getTime() : fromTs;
    const minTs = fromTs != null && toTs != null ? Math.min(fromTs, toTs) : fromTs;
    const maxTs = fromTs != null && toTs != null ? Math.max(fromTs, toTs) : fromTs;
    const cellTs = normalized.getTime();
    const today = this.normalizeDate(new Date());

    const isSelected =
      (fromTs != null && cellTs === fromTs) || (toTs != null && cellTs === toTs);
    const isInRange = minTs != null && maxTs != null && cellTs >= minTs && cellTs <= maxTs;

    return {
      date: normalized,
      label: normalized.getDate(),
      isToday: cellTs === today.getTime(),
      isSelected,
      isInRange: isInRange && !isSelected,
      isRangeStart: fromTs != null && cellTs === fromTs,
      isRangeEnd: toTs != null && cellTs === toTs,
      isDisabled: this.isCalendarDayDisabled(normalized),
      isOutsideMonth:
        normalized.getMonth() !== this.calendarViewMonth() ||
        normalized.getFullYear() !== this.calendarViewYear(),
    };
  }

  formatDisplayDate(date: Date): string {
    return date.toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }

  private normalizeDate(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
  }

  private eventDateTimestamp(ev: PublicEvent): number | null {
    if (ev.dateISO) {
      const parsed = new Date(`${ev.dateISO}T00:00:00`);
      if (!Number.isNaN(parsed.getTime())) {
        return this.normalizeDate(parsed).getTime();
      }
    }

    const dayMatch = ev.date.match(/(\d{1,2})(?:st|nd|rd|th)?/);
    const monthMatch = ev.date.match(
      /\b(Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:tember)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)\b/i,
    );
    const yearMatch = ev.date.match(/\b(20\d{2})\b/);

    if (!dayMatch || !monthMatch || !yearMatch) {
      return null;
    }

    const month = this.monthIndex(monthMatch[1]) - 1;
    if (month < 0) {
      return null;
    }

    const parsed = new Date(Number(yearMatch[1]), month, Number(dayMatch[1]));
    return Number.isNaN(parsed.getTime()) ? null : this.normalizeDate(parsed).getTime();
  }
}
