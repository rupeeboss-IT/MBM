import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-events',
  imports: [CommonModule, RouterLink],
  templateUrl: './events.html',
  styleUrl: './events.css',
})
export class Events {
  readonly active = signal<'all' | 'summit' | 'webinar' | 'workshop'>('all');

  setFilter(cat: 'all' | 'summit' | 'webinar' | 'workshop') {
    this.active.set(cat);
  }

  show(cat: 'summit' | 'webinar' | 'workshop') {
    const a = this.active();
    return a === 'all' || a === cat;
  }
}
