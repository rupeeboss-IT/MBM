import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthSessionService } from '../core/services/auth-session.service';

@Component({
  selector: 'app-header',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  private readonly session = inject(AuthSessionService);
  readonly isLoggedIn = this.session.isLoggedIn;
  readonly userId = this.session.userId;

  readonly userMenuOpen = signal(false);

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

  toggleUserMenu() {
    this.userMenuOpen.set(!this.userMenuOpen());
  }

  closeUserMenu() {
    this.userMenuOpen.set(false);
  }

  logout() {
    this.session.logout();
    this.closeUserMenu();
    this.router.navigateByUrl('/home');
  }
}
