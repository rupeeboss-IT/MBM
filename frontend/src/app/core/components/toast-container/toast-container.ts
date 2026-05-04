import { Component, inject } from '@angular/core';
import { ToastService, type ToastItem } from '../../services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.css',
})
export class ToastContainer {
  private readonly toast = inject(ToastService);
  readonly toasts = this.toast.toasts;

  toastClasses(t: ToastItem): string {
    return `toast toast--${t.variant}`;
  }

  dismiss(id: string) {
    this.toast.dismiss(id);
  }
}
