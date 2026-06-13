import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-admin-list-toolbar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-list-toolbar.html',
  styleUrl: './admin-list-toolbar.css',
})
export class AdminListToolbar {
  readonly panelTitle = input('Filters');
  readonly dateFrom = input('');
  readonly dateTo = input('');
  readonly sortPreset = input('latest');
  readonly showSort = input(true);
  readonly showExport = input(true);
  readonly showSearch = input(false);
  readonly disabled = input(false);

  readonly dateFromChange = output<string>();
  readonly dateToChange = output<string>();
  readonly sortPresetChange = output<string>();
  readonly applyFilters = output<void>();
  readonly exportClick = output<void>();

  onSortChange(value: string) {
    this.sortPresetChange.emit(value);
  }
}
