import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  PLATFORM_ID,
  forwardRef,
  inject,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { CKEditorModule } from '@ckeditor/ckeditor5-angular';
import { firstValueFrom } from 'rxjs';
import { apiUrl } from '../../../../core/utils/api-url';
import type { ImageUploadResponse } from '../../../../core/services/event-management.service';

/**
 * CKEditor 5 Community (GPL) — loaded only in the browser to avoid SSR/prerender DOM errors.
 */
@Component({
  selector: 'app-ckeditor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, CKEditorModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CkEditor),
      multi: true,
    },
  ],
  template: `
    @if (ready() && Editor) {
      <div class="cke-wrap">
        <ckeditor
          [editor]="Editor"
          [config]="config"
          [data]="value"
          [disabled]="disabled"
          (change)="onEditorChange($event)"
          (blur)="onTouched()"
        />
      </div>
    } @else if (isBrowser) {
      <div class="cke-loading">Loading editor…</div>
    } @else {
      <textarea class="cke-ssr-fallback" [value]="value" disabled rows="8"></textarea>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .cke-wrap {
        border: 1px solid var(--admin-border, #e5e7eb);
        border-radius: 12px;
        overflow: hidden;
        background: #fff;
      }
      .cke-wrap :deep(.ck-editor__editable) {
        min-height: 280px;
        max-height: 560px;
      }
      .cke-loading,
      .cke-ssr-fallback {
        width: 100%;
        padding: 1rem;
        border: 1px solid #e5e7eb;
        border-radius: 12px;
        font: inherit;
        color: #6b7280;
      }
    `,
  ],
})
export class CkEditor implements OnInit, ControlValueAccessor {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly http = inject(HttpClient);
  readonly isBrowser = isPlatformBrowser(this.platformId);

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  Editor: any = null;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  config: Record<string, unknown> = {};
  readonly ready = signal(false);

  value = '';
  disabled = false;

  private onChange: (val: string) => void = () => {};
  onTouched: () => void = () => {};

  async ngOnInit() {
    if (!this.isBrowser) return;

    const {
      ClassicEditor,
      Essentials,
      Paragraph,
      Heading,
      Bold,
      Italic,
      Underline,
      List,
      Link,
      Table,
      TableToolbar,
      Image,
      ImageUpload,
      ImageToolbar,
      ImageCaption,
      ImageStyle,
      PasteFromOffice,
      GeneralHtmlSupport,
    } = await import('ckeditor5');

    const http = this.http;
    const EventUploadPlugin = (editor: {
      plugins: { get: (name: string) => { createUploadAdapter: (loader: unknown) => unknown } };
    }) => {
      editor.plugins.get('FileRepository').createUploadAdapter = (loader: unknown) => {
        const fileLoader = loader as { file: Promise<File | null> };
        return {
          upload: () =>
            fileLoader.file.then(async (file) => {
              if (!file) throw new Error('No file selected.');
              const form = new FormData();
              form.append('file', file);
              const res = await firstValueFrom(
                http.post<ImageUploadResponse>(apiUrl('/api/events/admin/upload-image'), form),
              );
              if (!res?.success || !res.imageUrl) {
                throw new Error(res?.message || 'Image upload failed.');
              }
              return { default: res.imageUrl };
            }),
          abort: () => undefined,
        };
      };
    };

    this.Editor = ClassicEditor;
    this.config = {
      licenseKey: 'GPL',
      plugins: [
        Essentials,
        Paragraph,
        Heading,
        Bold,
        Italic,
        Underline,
        List,
        Link,
        Table,
        TableToolbar,
        Image,
        ImageUpload,
        ImageToolbar,
        ImageCaption,
        ImageStyle,
        PasteFromOffice,
        GeneralHtmlSupport,
      ],
      extraPlugins: [EventUploadPlugin],
      toolbar: {
        items: [
          'heading',
          '|',
          'bold',
          'italic',
          'underline',
          '|',
          'bulletedList',
          'numberedList',
          '|',
          'link',
          'insertTable',
          'uploadImage',
          '|',
          'undo',
          'redo',
        ],
      },
      heading: {
        options: [
          { model: 'paragraph', title: 'Paragraph', class: 'ck-heading_paragraph' },
          { model: 'heading2', view: 'h2', title: 'Heading 2', class: 'ck-heading_heading2' },
          { model: 'heading3', view: 'h3', title: 'Heading 3', class: 'ck-heading_heading3' },
        ],
      },
      table: {
        contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells'],
      },
      image: {
        toolbar: [
          'imageTextAlternative',
          'imageStyle:inline',
          'imageStyle:block',
          'imageStyle:side',
        ],
      },
      htmlSupport: {
        allow: [{ name: /.*/, attributes: true, classes: true, styles: true }],
      },
    };
    this.ready.set(true);
  }

  writeValue(value: string | null): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (val: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onEditorChange(event: { editor?: { getData: () => string } }): void {
    const data = event?.editor?.getData?.() ?? '';
    this.value = data;
    this.onChange(data);
  }
}
