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
  readonly mobileMenuOpen = signal(false);
  readonly servicesMenuOpen = signal(false);
  readonly moreMenuOpen = signal(false);

  searchOpen = false;
  searchQuery = '';

  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  constructor(private router: Router) {}

  toggleMobileMenu() {
    this.mobileMenuOpen.set(!this.mobileMenuOpen());
  }

  closeMobileMenu() {
    this.mobileMenuOpen.set(false);
    this.servicesMenuOpen.set(false);
    this.moreMenuOpen.set(false);
  }

  toggleServicesMenu() {
    this.servicesMenuOpen.set(!this.servicesMenuOpen());
    if (this.servicesMenuOpen()) this.moreMenuOpen.set(false);
  }

  toggleMoreMenu() {
    this.moreMenuOpen.set(!this.moreMenuOpen());
    if (this.moreMenuOpen()) this.servicesMenuOpen.set(false);
  }

  closeDropdowns() {
    this.servicesMenuOpen.set(false);
    this.moreMenuOpen.set(false);
    this.userMenuOpen.set(false);
  }

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
    this.closeDropdowns();
    this.closeMobileMenu();
    this.router.navigateByUrl('/home');
  }
}
