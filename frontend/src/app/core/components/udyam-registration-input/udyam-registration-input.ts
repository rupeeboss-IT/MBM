import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  Input,
  Output,
  EventEmitter,
  ViewChild,
  forwardRef,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import {
  UDYAM_PREFIX,
  UDYAM_EDITABLE_MAX_LENGTH,
  applyUdyamEditableChange,
  formatUdyamFromRaw,
  parseUdyamPayload,
  shouldAllowEditableChar,
  udyamToEditableDisplay,
} from '../../utils/udyam-input.util';

@Component({
  selector: 'app-udyam-registration-input',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './udyam-registration-input.css',
  template: `
    <div class="sd-udyam-input-wrap" [class.sd-udyam-input-wrap--invalid]="invalid">
      <span class="sd-udyam-input__prefix" aria-hidden="true">{{ prefix }}</span>
      <input
        #field
        class="sd-udyam-input__field"
        type="text"
        [id]="inputId"
        [attr.name]="name"
        [value]="editableDisplay"
        [attr.placeholder]="placeholder"
        [attr.required]="required ? '' : null"
        [attr.aria-invalid]="invalid"
        [attr.aria-describedby]="describedBy || null"
        [disabled]="disabled"
        autocomplete="off"
        [attr.inputmode]="inputMode"
        [attr.maxlength]="editableMaxLength"
        spellcheck="false"
        (input)="onInput($event)"
        (keydown)="onKeydown($event)"
        (paste)="onPaste($event)"
        (blur)="onTouched()" />
    </div>
  `,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UdyamRegistrationInputComponent),
      multi: true,
    },
  ],
})
export class UdyamRegistrationInputComponent implements ControlValueAccessor {
  readonly prefix = UDYAM_PREFIX;
  readonly editableMaxLength = UDYAM_EDITABLE_MAX_LENGTH;

  @Input() inputId = 'udyam-registration-number';
  @Input() name = 'udyam';
  @Input() placeholder = 'XX-00-0000000';
  @Input() invalid = false;
  @Input() required = false;
  @Input() describedBy: string | null = null;
  @Input() disabled = false;

  @Output() readonly valueChange = new EventEmitter<string>();

  @ViewChild('field') private fieldRef?: ElementRef<HTMLInputElement>;

  editableDisplay = '';

  get inputMode(): 'text' | 'numeric' {
    const payload = parseUdyamPayload(`${UDYAM_PREFIX}${this.editableDisplay}`);
    return payload.state.length < 2 ? 'text' : 'numeric';
  }

  private fullValue = '';
  private onChange: (value: string) => void = () => {};
  private onTouchedCallback: () => void = () => {};

  writeValue(value: string | null): void {
    this.fullValue = formatUdyamFromRaw(value ?? '');
    this.editableDisplay = udyamToEditableDisplay(this.fullValue);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouchedCallback = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onTouched(): void {
    this.onTouchedCallback();
  }

  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const cursor = input.selectionStart ?? input.value.length;
    this.applyChange(input.value, cursor, input);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.ctrlKey || event.metaKey || event.altKey) return;

    const input = event.target as HTMLInputElement;
    const cursor = input.selectionStart ?? 0;
    const selectionLength = (input.selectionEnd ?? cursor) - cursor;

    if (
      event.key.length === 1 &&
      !shouldAllowEditableChar(input.value, cursor, event.key, selectionLength)
    ) {
      event.preventDefault();
      return;
    }

    if (event.key === 'Backspace' && cursor > 0 && input.value[cursor - 1] === '-') {
      event.preventDefault();
      const next = `${input.value.slice(0, cursor - 2)}${input.value.slice(cursor)}`;
      this.applyChange(next, Math.max(0, cursor - 2), input);
      return;
    }

    if (event.key === '-') {
      event.preventDefault();
      const end = input.selectionEnd ?? cursor;
      const next = `${input.value.slice(0, cursor)}-${input.value.slice(end)}`;
      this.applyChange(next, cursor + 1, input);
    }
  }

  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const input = event.target as HTMLInputElement;
    const pasted = event.clipboardData?.getData('text') ?? '';
    const start = input.selectionStart ?? input.value.length;
    const end = input.selectionEnd ?? start;
    const merged = `${input.value.slice(0, start)}${pasted}${input.value.slice(end)}`;
    this.applyChange(merged, start + pasted.length, input);
  }

  private applyChange(rawEditable: string, cursor: number, input: HTMLInputElement): void {
    const result = applyUdyamEditableChange(rawEditable, cursor);
    this.editableDisplay = result.editable;
    this.fullValue = result.full;
    this.onChange(result.full);
    this.valueChange.emit(result.full);

    requestAnimationFrame(() => {
      input.setSelectionRange(result.cursor, result.cursor);
    });
  }
}
