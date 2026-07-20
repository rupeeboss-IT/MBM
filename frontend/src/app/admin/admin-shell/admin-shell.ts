import {
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { MBM_LOGO_ALT, MBM_LOGO_SRC } from '../../core/brand';
import { AdminSessionService } from '../../core/services/admin-session.service';
import {
  ADMIN_MENU_GROUPS,
  type AdminDropdown,
  type AdminNavGroup,
} from '../../header/admin-nav.config';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-shell.html',
  styleUrl: './admin-shell.css',
})
export class AdminShell implements OnDestroy {
  readonly logoSrc = MBM_LOGO_SRC;
  readonly logoAlt = MBM_LOGO_ALT;

  private readonly adminSession = inject(AdminSessionService);
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef<HTMLElement>);
  private navSub?: Subscription;

  readonly isSuperAdmin = this.adminSession.isSuperAdmin;
  readonly adminMenuGroups = ADMIN_MENU_GROUPS;
  readonly adminUrl = signal(this.router.url);
  readonly sidebarOpen = signal(false);
  readonly accountMenuOpen = signal(false);

  private readonly expandedGroups = signal<Record<AdminDropdown, boolean>>({
    people: false,
    crm: false,
    finance: false,
    partners: false,
    content: false,
    connect: false,
    administration: false,
    account: false,
  });

  readonly expandedGroupState = computed(() => this.expandedGroups());

  constructor() {
    this.syncExpandedGroups(this.router.url);

    this.navSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        const url = e.urlAfterRedirects || e.url;
        this.adminUrl.set(url);
        this.closeSidebar();
        this.syncExpandedGroups(url);
      });
  }

  ngOnDestroy(): void {
    this.navSub?.unsubscribe();
    this.setBodyScrollLock(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeSidebar();
    this.closeAccountMenu();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.accountMenuOpen()) return;

    const target = event.target as Node | null;
    const accountRoot = this.host.nativeElement.querySelector('.admin-sidebar__account');
    if (target && accountRoot?.contains(target)) return;

    this.closeAccountMenu();
  }

  isAccountActive(): boolean {
    return this.matchesAdminPrefix(['/admin-forgot-password']);
  }

  toggleAccountMenu(): void {
    this.accountMenuOpen.update((open) => !open);
  }

  closeAccountMenu(): void {
    if (!this.accountMenuOpen()) return;
    this.accountMenuOpen.set(false);
  }

  isGroupExpanded(id: AdminDropdown): boolean {
    return this.expandedGroupState()[id];
  }

  isAdminGroupActive(group: AdminNavGroup): boolean {
    return this.matchesAdminPrefix(group.prefixes);
  }

  toggleGroup(id: AdminDropdown): void {
    this.closeAccountMenu();
    this.expandedGroups.update((state) => ({
      ...state,
      [id]: !state[id],
    }));
  }

  closeSidebar(): void {
    if (!this.sidebarOpen()) return;
    this.sidebarOpen.set(false);
    this.setBodyScrollLock(false);
  }

  toggleSidebar(): void {
    const next = !this.sidebarOpen();
    this.sidebarOpen.set(next);
    this.setBodyScrollLock(next);
  }

  adminLogout(): void {
    this.closeSidebar();
    this.adminSession.logout();
    void this.router.navigateByUrl('/admin-login');
  }

  onNavClick(): void {
    this.closeSidebar();
    this.closeAccountMenu();
  }

  private syncExpandedGroups(url: string): void {
    const activeGroup = ADMIN_MENU_GROUPS.find((group) =>
      group.prefixes.some(
        (prefix) => url === prefix || url.startsWith(`${prefix}/`) || url.startsWith(prefix),
      ),
    );

    this.expandedGroups.update((state) => {
      const next = { ...state };
      if (activeGroup) {
        next[activeGroup.id] = true;
      }
      return next;
    });
  }

  private matchesAdminPrefix(prefixes: string[]): boolean {
    const url = this.adminUrl();
    return prefixes.some(
      (prefix) => url === prefix || url.startsWith(`${prefix}/`) || url.startsWith(prefix),
    );
  }

  private setBodyScrollLock(lock: boolean): void {
    try {
      document.body.classList.toggle('nav-scroll-lock', lock);
    } catch {
      // SSR: ignore
    }
  }
}
