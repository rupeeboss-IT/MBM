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
import {
  KB,
  MBM_CHAT_CONFIG,
  type MbmChip,
  matchIntent,
} from './msme-saathi-chat.config';

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
  private navSub?: Subscription;
  private shouldScroll = false;
  private typingTimer?: ReturnType<typeof setTimeout>;

  readonly config = MBM_CHAT_CONFIG;
  readonly isOpen = signal(false);
  readonly started = signal(false);
  readonly showRing = signal(true);
  readonly isTyping = signal(false);
  readonly messages = signal<ChatMessage[]>([]);
  readonly chips = signal<MbmChip[]>([]);
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
    this.isOpen.set(true);
    this.showRing.set(false);
    if (!this.started()) {
      this.started.set(true);
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

  onChipClick(chip: MbmChip): void {
    if (chip.url) return;
    if (chip.intent) this.handleIntent(chip.intent, chip.label);
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
    const node = KB[key] ?? KB['fallback'];
    this.chips.set([]);
    this.isTyping.set(true);
    this.shouldScroll = true;

    const delay = Math.min(900, 350 + node.reply.length * 1.2);
    if (this.typingTimer) clearTimeout(this.typingTimer);

    this.typingTimer = setTimeout(() => {
      this.isTyping.set(false);
      this.messages.update((m) => [...m, { role: 'bot', html: node.reply }]);
      this.chips.set(node.chips ?? []);
      this.shouldScroll = true;
    }, delay);
  }
}
