import {
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter, firstValueFrom, Subscription, timeout } from 'rxjs';
import { MBM_LOGO_ALT, MBM_LOGO_SRC } from '../core/brand';
import { AuthService } from '../core/services/auth.service';
import { AuthSessionService } from '../core/services/auth-session.service';
import { FREE_REGISTER_QUERY_PARAMS } from '../core/utils/registration-mode.util';

@Component({
  selector: 'app-header',
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header implements OnDestroy {
  readonly logoSrc = MBM_LOGO_SRC;
  readonly logoAlt = MBM_LOGO_ALT;

  private readonly session = inject(AuthSessionService);
  private readonly auth = inject(AuthService);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly router = inject(Router);
  private navSub?: Subscription;
  private nameLoadGen = 0;
  private lastLoadedNameUserId: string | null = null;

  readonly isLoggedIn = this.session.isLoggedIn;
  readonly userId = this.session.userId;
  private readonly fullName = signal<string | null>(null);
  readonly userFirstName = computed(() => {
    const first = displayFirstName(this.fullName());
    return first ?? 'My Account';
  });

  readonly userMenuOpen = signal(false);
  readonly mobileMenuOpen = signal(false);
  readonly servicesMenuOpen = signal(false);
  readonly moreMenuOpen = signal(false);

  readonly guestRegisterLabel = 'Register Free';

  readonly guestRegisterQueryParams = FREE_REGISTER_QUERY_PARAMS;

  searchOpen = false;
  searchQuery = '';

  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  constructor() {
    effect(() => {
      const id = this.userId();
      if (!id) {
        this.fullName.set(null);
        this.lastLoadedNameUserId = null;
        return;
      }
      if (this.lastLoadedNameUserId === id) return;
      void this.loadUserName();
    });

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
    if (this.servicesMenuOpen()) {
      this.moreMenuOpen.set(false);
    }
  }

  toggleMoreMenu(): void {
    this.moreMenuOpen.set(!this.moreMenuOpen());
    if (this.moreMenuOpen()) {
      this.servicesMenuOpen.set(false);
    }
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
    try {
      document.body.classList.toggle('nav-scroll-lock', lock);
    } catch {
      // Server-side prerendering: document is not a real DOM, ignore
    }
  }

  private async loadUserName(): Promise<void> {
    const gen = ++this.nameLoadGen;
    try {
      const data = await firstValueFrom(this.auth.me().pipe(timeout(15000)));
      if (gen !== this.nameLoadGen) return;
      this.fullName.set((data.fullName ?? '').trim() || null);
      this.lastLoadedNameUserId = this.userId();
    } catch {
      if (gen !== this.nameLoadGen) return;
      this.fullName.set(null);
    }
  }
}

function displayFirstName(fullName: string | null | undefined): string | null {
  const trimmed = (fullName ?? '').trim();
  if (!trimmed) return null;
  return trimmed.split(/\s+/)[0] ?? null;
}
