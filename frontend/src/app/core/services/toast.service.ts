import { Injectable, signal } from '@angular/core';

export type ToastVariant = 'success' | 'error' | 'info' | 'warning';

export interface ToastItem {
  id: string;
  message: string;
  variant: ToastVariant;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<ToastItem[]>([]);
  readonly toasts = this._toasts.asReadonly();

  private readonly defaultDuration = 4500;

  private nextId(): string {
    return globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`;
  }

  /** @param durationMs 0 = no auto-dismiss */
  show(message: string, variant: ToastVariant = 'info', durationMs = this.defaultDuration): string {
    const id = this.nextId();
    this._toasts.update((list) => [...list, { id, message, variant }]);
    if (durationMs > 0) {
      globalThis.setTimeout(() => this.dismiss(id), durationMs);
    }
    return id;
  }

  success(message: string, durationMs?: number) {
    return this.show(message, 'success', durationMs ?? this.defaultDuration);
  }

  error(message: string, durationMs?: number) {
    return this.show(message, 'error', durationMs ?? this.defaultDuration);
  }

  info(message: string, durationMs?: number) {
    return this.show(message, 'info', durationMs ?? this.defaultDuration);
  }

  warning(message: string, durationMs?: number) {
    return this.show(message, 'warning', durationMs ?? this.defaultDuration);
  }

  dismiss(id: string) {
    this._toasts.update((list) => list.filter((t) => t.id !== id));
  }
}
