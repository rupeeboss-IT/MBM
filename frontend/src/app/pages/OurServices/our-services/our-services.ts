import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-our-services',
  imports: [CommonModule, RouterLink],
  templateUrl: './our-services.html',
  styleUrl: './our-services.css',
})
export class OurServices {
  readonly active = signal<'all' | 'finance' | 'legal' | 'technology' | 'marketing' | 'growth'>('all');

  setFilter(cat: 'all' | 'finance' | 'legal' | 'technology' | 'marketing' | 'growth') {
    this.active.set(cat);
  }

  show(cat: 'finance' | 'legal' | 'technology' | 'marketing' | 'growth') {
    const a = this.active();
    return a === 'all' || a === cat;
  }
}
