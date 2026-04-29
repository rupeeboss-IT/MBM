import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-partners',
  imports: [CommonModule, RouterLink],
  templateUrl: './partners.html',
  styleUrl: './partners.css',
})
export class Partners {
  readonly active = signal<'all' | 'west' | 'north' | 'south' | 'east'>('all');

  setFilter(zone: 'all' | 'west' | 'north' | 'south' | 'east') {
    this.active.set(zone);
  }

  show(zone: 'west' | 'north' | 'south' | 'east') {
    const a = this.active();
    return a === 'all' || a === zone;
  }
}
