import { CommonModule } from '@angular/common';
import {
  AfterViewChecked,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { AuthSessionService } from '../../services/auth-session.service';
import { captureContactLeadSourceFromUrl } from '../../utils/contact-lead-source.util';
import { AnalyticsService } from '../../services/analytics.service';
import {
  CHIPS_MAIN,
  KB,
  MBM_CHAT_CONFIG,
  matchIntent,
} from './msme-saathi-chat.config';

/** Chip actions shown under bot replies (includes in-app routes for logged-in users). */
export interface SaathiChatChip {
  label: string;
  intent?: string;
  url?: string;
  route?: string;
}

const BACK_TO_MAIN_MENU_CHIP: SaathiChatChip = {
  label: 'Back to main menu',
  intent: 'welcome',
};

interface ChatMessage {
  role: 'user' | 'bot';
  text?: string;
  html?: string;
}

@Component({
  selector: 'app-msme-saathi-chat',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './msme-saathi-chat.html',
  styleUrl: './msme-saathi-chat.css',
})
export class MsmeSaathiChat implements OnDestroy, AfterViewChecked {
  private readonly router = inject(Router);
  private readonly session = inject(AuthSessionService);
  private readonly analytics = inject(AnalyticsService);
  private navSub?: Subscription;
  private shouldScroll = false;
  private typingTimer?: ReturnType<typeof setTimeout>;

  readonly config = MBM_CHAT_CONFIG;
  readonly isOpen = signal(false);
  readonly started = signal(false);
  readonly showRing = signal(true);
  readonly isTyping = signal(false);
  readonly messages = signal<ChatMessage[]>([]);
  readonly chips = signal<SaathiChatChip[]>([]);
  readonly inputValue = signal('');
  readonly visible = signal(true);

  @ViewChild('bodyEl') private bodyEl?: ElementRef<HTMLElement>;
  @ViewChild('inputEl') private inputEl?: ElementRef<HTMLInputElement>;

  constructor() {
    this.updateVisibility(this.router.url);
    this.navSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => this.updateVisibility(e.urlAfterRedirects));
  }

  ngOnDestroy(): void {
    this.navSub?.unsubscribe();
    if (this.typingTimer) clearTimeout(this.typingTimer);
  }

  ngAfterViewChecked(): void {
    if (!this.shouldScroll) return;
    this.shouldScroll = false;
    const el = this.bodyEl?.nativeElement;
    if (el) el.scrollTop = el.scrollHeight;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen()) this.closeChat();
  }

  get sendDisabled(): boolean {
    return this.inputValue().trim() === '';
  }

  onInput(value: string): void {
    this.inputValue.set(value);
  }

  toggleChat(): void {
    if (this.isOpen()) this.closeChat();
    else this.openChat();
  }

  openChat(): void {
    this.session.refreshFromStorage();
    this.isOpen.set(true);
    this.showRing.set(false);
    if (!this.started()) {
      this.started.set(true);
      this.analytics.trackChatbotOpen();
      this.showIntent('welcome');
    }
    setTimeout(() => this.inputEl?.nativeElement.focus(), 300);
  }

  closeChat(): void {
    this.isOpen.set(false);
  }

  submit(event?: Event): void {
    event?.preventDefault();
    const value = this.inputValue().trim();
    if (!value) return;
    this.inputValue.set('');
    this.handleText(value);
  }

  onChipClick(chip: SaathiChatChip): void {
    if (chip.url || chip.route) return;
    if (chip.intent) this.handleIntent(chip.intent, chip.label);
  }

  onRouteChip(chip: SaathiChatChip): void {
    const route = chip.route?.trim();
    if (!route) return;

    const queryIndex = route.indexOf('?');
    if (queryIndex >= 0) {
      captureContactLeadSourceFromUrl(route.slice(queryIndex));
    }

    this.closeChat();
    void this.router.navigateByUrl(route);
  }

  private updateVisibility(url: string): void {
    const path = url.split('?')[0];
    const hide =
      path.startsWith('/admin-dashboard') ||
      path.startsWith('/admin/') ||
      path === '/admin-login' ||
      path === '/admin-forgot-password' ||
      path.startsWith('/admin-reports');
    this.visible.set(!hide);
  }

  private handleIntent(key: string, label?: string): void {
    if (label && key !== 'welcome') {
      this.appendUser(label);
    }
    this.showIntent(key);
  }

  private handleText(text: string): void {
    this.appendUser(text);
    this.showIntent(matchIntent(text));
  }

  private appendUser(text: string): void {
    this.messages.update((m) => [...m, { role: 'user', text }]);
    this.chips.set([]);
    this.shouldScroll = true;
  }

  private showIntent(key: string): void {
    this.session.refreshFromStorage();
    const node = KB[key] ?? KB['fallback'];
    const loggedIn = this.session.isLoggedIn();
    const { reply, chips: resolvedChips } = this.resolveIntentContent(key, node.reply, node.chips ?? [], loggedIn);
    const chips = this.withMainMenuChip(key, resolvedChips);

    this.chips.set([]);
    this.isTyping.set(true);
    this.shouldScroll = true;

    const delay = Math.min(900, 350 + reply.length * 1.2);
    if (this.typingTimer) clearTimeout(this.typingTimer);

    this.typingTimer = setTimeout(() => {
      this.isTyping.set(false);
      this.messages.update((m) => [...m, { role: 'bot', html: reply }]);
      this.chips.set(chips);
      this.shouldScroll = true;
    }, delay);
  }

  private resolveIntentContent(
    key: string,
    reply: string,
    chips: SaathiChatChip[],
    loggedIn: boolean,
  ): { reply: string; chips: SaathiChatChip[] } {
    if (!loggedIn) return { reply, chips };

    if (key === 'join') {
      return {
        reply:
          'You\'re already signed in. 🎉<br><br>' +
          'Reach out to our team below, or open <b>My plan</b> to see your current subscription.',
        chips: [
          { label: 'Contact us', route: '/contact' },
          { label: 'My plan', route: '/my-plan' },
        ],
      };
    }

    return { reply, chips: this.adaptChipsForLoggedIn(chips) };
  }

  /** Ensures every sub-menu chip row ends with a way back to CHIPS_MAIN (welcome). */
  private withMainMenuChip(intentKey: string, chips: SaathiChatChip[]): SaathiChatChip[] {
    if (intentKey === 'welcome' || this.isMainMenuChips(chips)) return chips;
    if (chips.some((c) => c.intent === 'welcome')) return chips;
    return [...chips, BACK_TO_MAIN_MENU_CHIP];
  }

  private isMainMenuChips(chips: SaathiChatChip[]): boolean {
    if (chips.length !== CHIPS_MAIN.length) return false;
    return chips.every(
      (chip, i) => chip.label === CHIPS_MAIN[i].label && chip.intent === CHIPS_MAIN[i].intent,
    );
  }

  private adaptChipsForLoggedIn(chips: SaathiChatChip[]): SaathiChatChip[] {
    const hasContactChip = chips.some(
      (c) => c.label === 'Contact us' || (c.route?.includes('/contact') ?? false),
    );

    return chips.flatMap((chip) => {
      if (chip.url === MBM_CHAT_CONFIG.registerUrl || chip.intent === 'join') {
        if (hasContactChip) {
          return [{ label: 'My plan', route: '/my-plan' }];
        }
        return [{ label: 'Contact us', route: '/contact?source=MSMECHATBOT' }];
      }
      return [chip];
    });
  }
}
