import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  PLATFORM_ID,
  ViewChild,
  forwardRef,
  inject,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { CommonModule, isPlatformBrowser } from '@angular/common';

interface ToolbarButton {
  type: 'cmd' | 'block' | 'link' | 'sep' | 'table' | 'image';
  cmd?: string;
  arg?: string;
  label: string;
  title: string;
  styleClass?: string;
}

const TOOLBAR: ToolbarButton[] = [
  { type: 'block', arg: 'h2', label: 'H2', title: 'Heading 2' },
  { type: 'block', arg: 'h3', label: 'H3', title: 'Heading 3' },
  { type: 'block', arg: 'p', label: 'P', title: 'Paragraph' },
  { type: 'sep', label: '', title: 'sep1' },
  { type: 'cmd', cmd: 'bold', label: 'B', title: 'Bold', styleClass: 're-btn--bold' },
  { type: 'cmd', cmd: 'italic', label: 'I', title: 'Italic', styleClass: 're-btn--italic' },
  { type: 'cmd', cmd: 'underline', label: 'U', title: 'Underline', styleClass: 're-btn--underline' },
  { type: 'sep', label: '', title: 'sep2' },
  { type: 'cmd', cmd: 'insertUnorderedList', label: 'List', title: 'Bullet list' },
  { type: 'cmd', cmd: 'insertOrderedList', label: '1–9', title: 'Numbered list' },
  { type: 'sep', label: '', title: 'sep3' },
  { type: 'block', arg: 'blockquote', label: 'Quote', title: 'Blockquote' },
  { type: 'link', label: 'Link', title: 'Insert link' },
  { type: 'table', label: 'Table', title: 'Insert table' },
  { type: 'image', label: 'Image', title: 'Insert image URL' },
  { type: 'sep', label: '', title: 'sep4' },
  { type: 'cmd', cmd: 'removeFormat', label: 'Clear', title: 'Clear formatting' },
];

@Component({
  selector: 'app-rich-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RichEditor),
      multi: true,
    },
  ],
  template: `
    <div class="re-wrap" [class.re-wrap--focus]="focused()">
      <div class="re-toolbar" role="toolbar" aria-label="Text formatting" (mousedown)="$event.preventDefault()">
        @for (btn of toolbar; track btn.title) {
          @if (btn.type === 'sep') {
            <span class="re-sep" aria-hidden="true"></span>
          } @else if (btn.type === 'cmd') {
            <button
              type="button"
              class="re-btn"
              [class.re-btn--bold]="btn.styleClass === 're-btn--bold'"
              [class.re-btn--italic]="btn.styleClass === 're-btn--italic'"
              [class.re-btn--underline]="btn.styleClass === 're-btn--underline'"
              [title]="btn.title"
              [attr.aria-label]="btn.title"
              (click)="execCmd(btn.cmd!)"
            >{{ btn.label }}</button>
          } @else if (btn.type === 'block') {
            <button
              type="button"
              class="re-btn"
              [title]="btn.title"
              [attr.aria-label]="btn.title"
              (click)="execBlock(btn.arg!)"
            >{{ btn.label }}</button>
          } @else if (btn.type === 'link') {
            <button
              type="button"
              class="re-btn"
              [title]="btn.title"
              [attr.aria-label]="btn.title"
              (click)="insertLink()"
            >{{ btn.label }}</button>
          } @else if (btn.type === 'table') {
            <button
              type="button"
              class="re-btn"
              [title]="btn.title"
              [attr.aria-label]="btn.title"
              (click)="insertTable()"
            >{{ btn.label }}</button>
          } @else if (btn.type === 'image') {
            <button
              type="button"
              class="re-btn"
              [title]="btn.title"
              [attr.aria-label]="btn.title"
              (click)="insertImage()"
            >{{ btn.label }}</button>
          }
        }
      </div>

      <div
        #editor
        class="re-editor"
        contenteditable="true"
        [attr.data-placeholder]="placeholder"
        (input)="onInput()"
        (focus)="focused.set(true)"
        (blur)="onBlur()"
        (paste)="onPaste($event)"
        (keydown)="onKeydown($event)"
      ></div>
    </div>
  `,
  styles: [`
    :host { display: block; }

    .re-wrap {
      border: 1px solid var(--admin-border, #e5e7eb);
      border-radius: 12px;
      overflow: hidden;
      background: #fff;
      transition: border-color 0.15s ease, box-shadow 0.15s ease;
    }

    .re-wrap--focus {
      border-color: var(--admin-navy-mid, #0f3460);
      box-shadow: var(--admin-focus-ring, 0 0 0 3px rgba(15, 52, 96, 0.1));
    }

    .re-toolbar {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 4px;
      padding: 10px 12px;
      background: #fafbfc;
      border-bottom: 1px solid var(--admin-border, #e5e7eb);
    }

    .re-sep {
      width: 1px;
      height: 18px;
      margin: 0 4px;
      background: #e5e7eb;
      flex-shrink: 0;
    }

    .re-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      height: 32px;
      min-width: 32px;
      padding: 0 10px;
      border: none;
      border-radius: 7px;
      background: transparent;
      cursor: pointer;
      font-size: 0.78rem;
      font-weight: 600;
      letter-spacing: 0.01em;
      color: #374151;
      font-family: inherit;
      transition: background 0.12s ease, color 0.12s ease;
      user-select: none;
    }

    .re-btn:hover {
      background: #eef1f5;
      color: #111827;
    }

    .re-btn:active {
      background: #e5e9f0;
    }

    .re-btn--bold { font-weight: 800; }
    .re-btn--italic { font-style: italic; }
    .re-btn--underline { text-decoration: underline; text-underline-offset: 2px; }

    .re-editor {
      min-height: 420px;
      max-height: 640px;
      padding: 24px 28px;
      outline: none;
      font-size: 0.95rem;
      line-height: 1.75;
      color: #1f2937;
      overflow-y: auto;
      word-break: break-word;
    }

    .re-editor:empty::before {
      content: attr(data-placeholder);
      color: #9ca3af;
      pointer-events: none;
    }

    .re-editor h2 {
      font-size: 1.45rem;
      font-weight: 750;
      letter-spacing: -0.02em;
      margin: 1.4rem 0 0.55rem;
      color: #0f172a;
    }
    .re-editor h3 {
      font-size: 1.15rem;
      font-weight: 700;
      margin: 1.2rem 0 0.45rem;
      color: #0f172a;
    }
    .re-editor p { margin: 0 0 0.85rem; }
    .re-editor ul, .re-editor ol {
      padding-left: 1.4rem;
      margin: 0.4rem 0 0.85rem;
    }
    .re-editor li { margin-bottom: 0.25rem; }
    .re-editor strong, .re-editor b { font-weight: 700; }
    .re-editor em, .re-editor i { font-style: italic; }
    .re-editor u { text-decoration: underline; }
    .re-editor blockquote {
      border-left: 3px solid var(--admin-navy-mid, #0f3460);
      padding: 0.35rem 0 0.35rem 1rem;
      margin: 1rem 0;
      color: #4b5563;
    }
    .re-editor a {
      color: var(--admin-navy-mid, #0f3460);
      text-decoration: underline;
      text-underline-offset: 2px;
    }
    .re-editor img {
      max-width: 100%;
      height: auto;
      border-radius: 8px;
      margin: 0.75rem 0;
    }
    .re-editor table {
      width: 100%;
      border-collapse: collapse;
      margin: 0.85rem 0;
    }
    .re-editor th,
    .re-editor td {
      border: 1px solid #d1d5db;
      padding: 0.45rem 0.65rem;
      text-align: left;
    }
  `],
})
export class RichEditor implements AfterViewInit, ControlValueAccessor {
  @ViewChild('editor') editorRef!: ElementRef<HTMLDivElement>;

  private readonly platformId = inject(PLATFORM_ID);
  private onChange: (val: string) => void = () => {};
  private onTouched: () => void = () => {};
  private pendingValue: string | null = null;

  readonly focused = signal(false);
  readonly toolbar = TOOLBAR;
  readonly placeholder = 'Start writing… You can paste from Word or Google Docs.';

  ngAfterViewInit() {
    if (!isPlatformBrowser(this.platformId)) return;
    const el = this.editorRef.nativeElement;
    if (this.pendingValue !== null) {
      el.innerHTML = this.pendingValue;
      this.pendingValue = null;
    }
    if (!el.innerHTML.trim()) {
      el.innerHTML = '<p><br></p>';
    }
  }

  writeValue(value: string | null) {
    const html = value ?? '';
    if (!isPlatformBrowser(this.platformId)) return;
    if (!this.editorRef) {
      this.pendingValue = html;
      return;
    }
    this.editorRef.nativeElement.innerHTML = html || '<p><br></p>';
  }

  registerOnChange(fn: (val: string) => void) {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void) {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean) {
    if (!this.editorRef) return;
    this.editorRef.nativeElement.contentEditable = disabled ? 'false' : 'true';
  }

  onInput() {
    const el = this.editorRef.nativeElement;
    const html = el.innerHTML === '<p><br></p>' ? '' : el.innerHTML;
    this.onChange(html);
  }

  onBlur() {
    this.focused.set(false);
    this.onTouched();
  }

  onKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) {
      const sel = window.getSelection();
      if (!sel || !sel.rangeCount) return;
      const node = sel.getRangeAt(0).startContainer;
      const block =
        (node as Element).closest?.('h2,h3,h4') ??
        node.parentElement?.closest('h2,h3,h4');
      if (block) {
        e.preventDefault();
        document.execCommand('insertParagraph');
        document.execCommand('formatBlock', false, 'p');
      }
    }
  }

  onPaste(e: ClipboardEvent) {
    const cd = e.clipboardData;
    if (!cd) return;

    const html = cd.getData('text/html');
    const plain = cd.getData('text/plain');

    if (html && html.trim()) return;

    if (plain && plain.trim()) {
      e.preventDefault();
      const paragraphs = plain
        .split(/\n{2,}/)
        .map((p) =>
          p
            .split(/\n/)
            .map((line) => escapeHtml(line.trim()))
            .filter(Boolean)
            .join('<br>'),
        )
        .filter(Boolean);

      const htmlContent = paragraphs.length
        ? paragraphs.map((p) => `<p>${p}</p>`).join('')
        : `<p>${escapeHtml(plain)}</p>`;

      document.execCommand('insertHTML', false, htmlContent);
      this.onInput();
    }
  }

  execCmd(cmd: string) {
    this.editorRef.nativeElement.focus();
    document.execCommand(cmd, false);
    this.onInput();
  }

  execBlock(tag: string) {
    this.editorRef.nativeElement.focus();
    document.execCommand('formatBlock', false, tag);
    this.onInput();
  }

  insertLink() {
    const sel = window.getSelection();
    const currentLink =
      sel?.anchorNode?.parentElement?.closest('a')?.getAttribute('href') ?? '';
    const url = prompt('Enter URL:', currentLink || 'https://');
    if (url === null) return;
    this.editorRef.nativeElement.focus();
    if (url.trim()) {
      document.execCommand('createLink', false, url.trim());
    } else {
      document.execCommand('unlink');
    }
    this.onInput();
  }

  insertTable() {
    this.editorRef.nativeElement.focus();
    const html =
      '<table><thead><tr><th>Column 1</th><th>Column 2</th></tr></thead>' +
      '<tbody><tr><td>&nbsp;</td><td>&nbsp;</td></tr><tr><td>&nbsp;</td><td>&nbsp;</td></tr></tbody></table><p><br></p>';
    document.execCommand('insertHTML', false, html);
    this.onInput();
  }

  insertImage() {
    const url = prompt('Image URL (upload via Featured Image first, then paste the path):', '/uploads/');
    if (url === null || !url.trim()) return;
    this.editorRef.nativeElement.focus();
    const src = escapeHtml(url.trim());
    document.execCommand('insertHTML', false, `<p><img src="${src}" alt="" /></p>`);
    this.onInput();
  }
}

function escapeHtml(str: string): string {
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}
