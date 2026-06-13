import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { parseApiLocalDate } from '../utils/parse-api-local-date';

/**
 * Formats API timestamps as server local wall-clock time (matches GETDATE / DateTime.Now).
 */
@Pipe({
  name: 'localDate',
  standalone: true,
})
export class LocalDatePipe implements PipeTransform {
  private readonly datePipe = new DatePipe('en-IN');

  transform(
    value: string | Date | null | undefined,
    format = 'dd MMM yyyy',
  ): string | null {
    const parsed = parseApiLocalDate(value);
    if (!parsed) return null;
    return this.datePipe.transform(parsed, format);
  }
}
