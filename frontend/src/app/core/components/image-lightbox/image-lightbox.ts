import {
  Component,
  ElementRef,
  HostListener,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-image-lightbox',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './image-lightbox.html',
  styleUrl: './image-lightbox.css',
})
export class ImageLightbox {
  readonly imageSrc = input<string | null>(null);
  readonly alt = input('');
  readonly isOpen = input(false);
  readonly closed = output<void>();

  readonly imgRef = viewChild<ElementRef<HTMLImageElement>>('imgEl');
  readonly viewportRef = viewChild<ElementRef<HTMLDivElement>>('viewport');

  readonly scale = signal(1);
  readonly translateX = signal(0);
  readonly translateY = signal(0);

  private dragging = false;
  private dragStartX = 0;
  private dragStartY = 0;
  private dragOriginX = 0;
  private dragOriginY = 0;
  private pinchStartDistance = 0;
  private pinchStartScale = 1;
  private lastTap = 0;

  private readonly minScale = 0.5;
  private readonly maxScale = 5;

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        document.body.style.overflow = 'hidden';
        this.resetTransform();
      } else {
        document.body.style.overflow = '';
      }
    });
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(ev: KeyboardEvent) {
    if (!this.isOpen()) return;
    if (ev.key === 'Escape') {
      ev.preventDefault();
      this.close();
    }
  }

  close() {
    this.closed.emit();
  }

  zoomIn() {
    this.setScale(this.scale() + 0.25);
  }

  zoomOut() {
    this.setScale(this.scale() - 0.25);
  }

  resetTransform() {
    this.scale.set(1);
    this.translateX.set(0);
    this.translateY.set(0);
  }

  onWheel(ev: WheelEvent) {
    if (!this.isOpen()) return;
    ev.preventDefault();
    const delta = ev.deltaY > 0 ? -0.15 : 0.15;
    this.setScale(this.scale() + delta);
  }

  onDoubleClick() {
    if (this.scale() > 1) {
      this.resetTransform();
    } else {
      this.setScale(2);
    }
  }

  onPointerDown(ev: PointerEvent) {
    if (this.scale() <= 1) return;
    this.dragging = true;
    this.dragStartX = ev.clientX;
    this.dragStartY = ev.clientY;
    this.dragOriginX = this.translateX();
    this.dragOriginY = this.translateY();
    (ev.target as HTMLElement).setPointerCapture?.(ev.pointerId);
  }

  onPointerMove(ev: PointerEvent) {
    if (!this.dragging) return;
    this.translateX.set(this.dragOriginX + (ev.clientX - this.dragStartX));
    this.translateY.set(this.dragOriginY + (ev.clientY - this.dragStartY));
  }

  onPointerUp(ev: PointerEvent) {
    this.dragging = false;
    (ev.target as HTMLElement).releasePointerCapture?.(ev.pointerId);
  }

  onTouchStart(ev: TouchEvent) {
    if (ev.touches.length === 2) {
      this.pinchStartDistance = this.getTouchDistance(ev.touches);
      this.pinchStartScale = this.scale();
      ev.preventDefault();
      return;
    }

    if (ev.touches.length === 1) {
      const now = Date.now();
      if (now - this.lastTap < 300) {
        this.onDoubleClick();
        this.lastTap = 0;
      } else {
        this.lastTap = now;
      }
    }
  }

  onTouchMove(ev: TouchEvent) {
    if (ev.touches.length === 2) {
      ev.preventDefault();
      const distance = this.getTouchDistance(ev.touches);
      if (this.pinchStartDistance > 0) {
        const ratio = distance / this.pinchStartDistance;
        this.setScale(this.pinchStartScale * ratio);
      }
    } else if (ev.touches.length === 1 && this.scale() > 1 && !this.dragging) {
      const touch = ev.touches[0];
      if (this.dragStartX === 0 && this.dragStartY === 0) {
        this.dragStartX = touch.clientX;
        this.dragStartY = touch.clientY;
        this.dragOriginX = this.translateX();
        this.dragOriginY = this.translateY();
        this.dragging = true;
      } else if (this.dragging) {
        this.translateX.set(this.dragOriginX + (touch.clientX - this.dragStartX));
        this.translateY.set(this.dragOriginY + (touch.clientY - this.dragStartY));
      }
    }
  }

  onTouchEnd() {
    this.dragging = false;
    this.dragStartX = 0;
    this.dragStartY = 0;
    this.pinchStartDistance = 0;
  }

  transformStyle() {
    return {
      transform: `translate(${this.translateX()}px, ${this.translateY()}px) scale(${this.scale()})`,
    };
  }

  private setScale(next: number) {
    const clamped = Math.min(this.maxScale, Math.max(this.minScale, next));
    this.scale.set(Math.round(clamped * 100) / 100);
    if (clamped <= 1) {
      this.translateX.set(0);
      this.translateY.set(0);
    }
  }

  private getTouchDistance(touches: TouchList) {
    const dx = touches[0].clientX - touches[1].clientX;
    const dy = touches[0].clientY - touches[1].clientY;
    return Math.hypot(dx, dy);
  }
}
