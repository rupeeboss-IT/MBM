import { CommonModule } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-admin-pagination',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-pagination.html',
  styleUrl: './admin-pagination.css',
})
export class AdminPagination {
  readonly page = input(1);
  readonly pageSize = input(10);
  readonly total = input(0);
  readonly disabled = input(false);

  readonly pageChange = output<number>();

  readonly totalPages = computed(() => {
    const size = Math.max(1, this.pageSize());
    return Math.max(1, Math.ceil(this.total() / size));
  });

  readonly rangeLabel = computed(() => {
    const total = this.total();
    if (total <= 0) return '0 records';
    const size = Math.max(1, this.pageSize());
    const start = (this.page() - 1) * size + 1;
    const end = Math.min(total, this.page() * size);
    return `${start}–${end} of ${total}`;
  });

  goPrev() {
    if (this.disabled() || this.page() <= 1) return;
    this.pageChange.emit(this.page() - 1);
  }

  goNext() {
    if (this.disabled() || this.page() >= this.totalPages()) return;
    this.pageChange.emit(this.page() + 1);
  }
}
