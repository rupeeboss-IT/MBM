import { Component, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-header',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  searchOpen = false;
  searchQuery = '';

  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  constructor(private router: Router) {}

  toggleSearch() {
    this.searchOpen = !this.searchOpen;
    if (this.searchOpen) {
      setTimeout(() => this.searchInput?.nativeElement?.focus(), 0);
    }
  }

  submitSearch() {
    const q = this.searchQuery.trim();
    if (!q) return;
    this.router.navigate(['/search'], { queryParams: { q } });
    if (this.searchOpen) this.toggleSearch();
  }
}
