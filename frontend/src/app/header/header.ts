import {
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { MBM_LOGO_ALT, MBM_LOGO_SRC } from '../core/brand';
import { AuthSessionService } from '../core/services/auth-session.service';

@Component({
  selector: 'app-header',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header implements OnDestroy {
  readonly logoSrc = MBM_LOGO_SRC;
  readonly logoAlt = MBM_LOGO_ALT;

  private readonly session = inject(AuthSessionService);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly router = inject(Router);
  private navSub?: Subscription;

  readonly isLoggedIn = this.session.isLoggedIn;
  readonly userId = this.session.userId;

  readonly userMenuOpen = signal(false);
  readonly mobileMenuOpen = signal(false);
  readonly servicesMenuOpen = signal(false);
  readonly moreMenuOpen = signal(false);

  searchOpen = false;
  searchQuery = '';

  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  constructor() {
    this.navSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => {
        this.closeMobileMenu();
        this.closeDropdowns();
        if (this.searchOpen) this.searchOpen = false;
      });
  }

  ngOnDestroy(): void {
    this.navSub?.unsubscribe();
    this.setBodyScrollLock(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    if (!target || this.host.nativeElement.contains(target)) return;
    this.closeMobileMenu();
    this.closeDropdowns();
    if (this.searchOpen) this.searchOpen = false;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMobileMenu();
    this.closeDropdowns();
    if (this.searchOpen) this.searchOpen = false;
  }

  toggleMobileMenu(): void {
    const next = !this.mobileMenuOpen();
    this.mobileMenuOpen.set(next);
    this.setBodyScrollLock(next);
    if (next) {
      this.closeDropdowns();
    }
  }

  closeMobileMenu(): void {
    if (!this.mobileMenuOpen()) return;
    this.mobileMenuOpen.set(false);
    this.servicesMenuOpen.set(false);
    this.moreMenuOpen.set(false);
    this.setBodyScrollLock(false);
  }

  toggleServicesMenu(): void {
    this.servicesMenuOpen.set(!this.servicesMenuOpen());
    if (this.servicesMenuOpen()) this.moreMenuOpen.set(false);
  }

  toggleMoreMenu(): void {
    this.moreMenuOpen.set(!this.moreMenuOpen());
    if (this.moreMenuOpen()) this.servicesMenuOpen.set(false);
  }

  closeDropdowns(): void {
    this.servicesMenuOpen.set(false);
    this.moreMenuOpen.set(false);
    this.userMenuOpen.set(false);
  }

  toggleSearch(): void {
    this.searchOpen = !this.searchOpen;
    if (this.searchOpen) {
      setTimeout(() => this.searchInput?.nativeElement?.focus(), 0);
    }
  }

  submitSearch(): void {
    const q = this.searchQuery.trim();
    if (!q) return;
    this.router.navigate(['/search'], { queryParams: { q } });
    if (this.searchOpen) this.toggleSearch();
  }

  toggleUserMenu(): void {
    this.userMenuOpen.set(!this.userMenuOpen());
  }

  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  logout(): void {
    this.session.logout();
    this.closeDropdowns();
    this.closeMobileMenu();
    this.router.navigateByUrl('/home');
  }

  private setBodyScrollLock(lock: boolean): void {
    document.body.classList.toggle('nav-scroll-lock', lock);
  }
}
