import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-schemes',
  imports: [CommonModule, RouterLink],
  templateUrl: './schemes.html',
  styleUrl: './schemes.css',
})
export class Schemes {
  readonly active = signal<'all' | 'central' | 'credit' | 'technology' | 'marketing'>('all');

  setFilter(cat: 'all' | 'central' | 'credit' | 'technology' | 'marketing') {
    this.active.set(cat);
  }

  show(cat: 'central' | 'credit' | 'technology' | 'marketing') {
    const a = this.active();
    return a === 'all' || a === cat;
  }
}
