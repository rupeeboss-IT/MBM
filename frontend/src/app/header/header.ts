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
import { AdminSessionService } from '../core/services/admin-session.service';
import { AuthService } from '../core/services/auth.service';
import { AuthSessionService } from '../core/services/auth-session.service';
import {
  ADMIN_MENU_GROUPS,
  type AdminDropdown,
  type AdminNavGroup,
} from './admin-nav.config';

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
  private readonly adminSession = inject(AdminSessionService);
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
  readonly isSuperAdmin = this.adminSession.isSuperAdmin;
  readonly showAdminNav = computed(
    () => this.adminSession.isLoggedIn() && this.adminSession.hasAdminAccess(),
  );

  readonly userMenuOpen = signal(false);
  readonly mobileMenuOpen = signal(false);
  readonly servicesMenuOpen = signal(false);
  readonly moreMenuOpen = signal(false);
  readonly adminPeopleOpen = signal(false);
  readonly adminCrmOpen = signal(false);
  readonly adminFinanceOpen = signal(false);
  readonly adminPartnersOpen = signal(false);
  readonly adminContentOpen = signal(false);
  readonly adminConnectOpen = signal(false);
  readonly adminAccountOpen = signal(false);
  readonly adminUrl = signal(this.router.url);
  readonly adminMenuGroups = ADMIN_MENU_GROUPS;

  isAdminGroupActive(group: AdminNavGroup): boolean {
    return this.matchesAdminPrefix(group.prefixes);
  }

  adminDropdownOpen(menu: AdminDropdown): boolean {
    return this.adminDropdownSignals[menu]();
  }

  private readonly adminDropdownSignals: Record<AdminDropdown, ReturnType<typeof signal<boolean>>> = {
    people: this.adminPeopleOpen,
    crm: this.adminCrmOpen,
    finance: this.adminFinanceOpen,
    partners: this.adminPartnersOpen,
    content: this.adminContentOpen,
    connect: this.adminConnectOpen,
    account: this.adminAccountOpen,
  };

  readonly isAccountActive = computed(() =>
    this.matchesAdminPrefix(['/admin-forgot-password']),
  );

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
      .subscribe((e) => {
        this.adminUrl.set(e.urlAfterRedirects || e.url);
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
    this.closeAdminDropdowns();
    this.setBodyScrollLock(false);
  }

  toggleServicesMenu(): void {
    this.servicesMenuOpen.set(!this.servicesMenuOpen());
    if (this.servicesMenuOpen()) {
      this.moreMenuOpen.set(false);
      this.closeAdminDropdowns();
    }
  }

  toggleMoreMenu(): void {
    this.moreMenuOpen.set(!this.moreMenuOpen());
    if (this.moreMenuOpen()) {
      this.servicesMenuOpen.set(false);
      this.closeAdminDropdowns();
    }
  }

  toggleAdminDropdown(menu: AdminDropdown): void {
    const current = this.adminDropdownSignals[menu];
    const next = !current();
    this.closeDropdowns();
    current.set(next);
  }

  closeDropdowns(): void {
    this.servicesMenuOpen.set(false);
    this.moreMenuOpen.set(false);
    this.userMenuOpen.set(false);
    this.closeAdminDropdowns();
  }

  closeAdminDropdowns(): void {
    for (const sig of Object.values(this.adminDropdownSignals)) {
      sig.set(false);
    }
  }

  closeAdminDropdown(menu: AdminDropdown): void {
    this.adminDropdownSignals[menu].set(false);
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
    if (this.userMenuOpen()) this.closeAdminDropdowns();
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

  adminLogout(): void {
    this.adminSession.logout();
    this.closeDropdowns();
    this.closeMobileMenu();
    this.router.navigateByUrl('/admin-login');
  }

  private matchesAdminPrefix(prefixes: string[]): boolean {
    const url = this.adminUrl();
    return prefixes.some((prefix) => url === prefix || url.startsWith(`${prefix}/`) || url.startsWith(prefix));
  }

  private setBodyScrollLock(lock: boolean): void {
    document.body.classList.toggle('nav-scroll-lock', lock);
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
