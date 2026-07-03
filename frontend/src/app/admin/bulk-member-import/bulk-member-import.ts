import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import {
  BulkMemberImportService,
  normalizeImportRow,
  normalizeImportSummary,
  type BulkImportBatchSummary,
  type BulkImportEmailRowPayload,
  type BulkImportRowResult,
} from '../../core/services/bulk-member-import.service';
import { AdminSessionService } from '../../core/services/admin-session.service';
import { ToastService } from '../../core/services/toast.service';
import { getHttpErrorMessage } from '../../core/utils/http-error-message';
import {
  downloadBulkMemberImportSample,
  exportBulkImportErrorReport,
  parseBulkMemberImportExcel,
  type ParsedImportRow,
} from '../../core/utils/bulk-member-import-excel';

type ImportPhase =
  | 'idle'
  | 'reading'
  | 'validating'
  | 'importing'
  | 'sendingEmails'
  | 'completed'
  | 'failed';

const CREATE_BATCH_SIZE = 50;
const EMAIL_BATCH_SIZE = 15;
const CREATE_TIMEOUT_MS = 180_000;
const EMAIL_TIMEOUT_MS = 600_000;

@Component({
  selector: 'app-bulk-member-import',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './bulk-member-import.html',
  styleUrls: ['./bulk-member-import.css', '../admin-shared.css'],
})
export class BulkMemberImport {
  private readonly api = inject(BulkMemberImportService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly adminSession = inject(AdminSessionService);

  readonly phase = signal<ImportPhase>('idle');
  readonly progressPct = signal(0);
  readonly progressLabel = signal('');
  readonly importing = computed(() => {
    const p = this.phase();
    return p === 'importing' || p === 'reading' || p === 'validating' || p === 'sendingEmails';
  });
  readonly selectedFileName = signal<string | null>(null);
  readonly partialFailure = signal(false);
  readonly pendingEmailRows = signal<BulkImportEmailRowPayload[]>([]);

  readonly totalRecords = signal(0);
  readonly summary = signal<BulkImportBatchSummary | null>(null);
  readonly errorRows = signal<BulkImportRowResult[]>([]);
  readonly successRows = signal<BulkImportRowResult[]>([]);

  readonly canResendEmails = computed(() => this.pendingEmailRows().length > 0 && !this.importing());

  readonly phaseLabel = computed(() => {
    switch (this.phase()) {
      case 'reading':
        return 'Reading Excel…';
      case 'validating':
        return 'Validating Records…';
      case 'importing':
        return 'Creating Members…';
      case 'sendingEmails':
        return 'Sending Emails…';
      case 'completed':
        return this.partialFailure() ? 'Completed with errors' : 'Completed';
      case 'failed':
        return 'Import Failed';
      default:
        return '';
    }
  });

  onFileChange(ev: Event) {
    if (this.importing()) return;
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (!file) {
      this.selectedFileName.set(null);
      return;
    }
    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      this.toast.error('Only Excel (.xlsx) files are accepted.');
      input.value = '';
      this.selectedFileName.set(null);
      return;
    }
    this.selectedFileName.set(file.name);
    this.resetResults();
  }

  async downloadSample() {
    try {
      await downloadBulkMemberImportSample();
    } catch {
      this.toast.error('Unable to download sample file. Please try again.');
    }
  }

  async startImport() {
    if (this.importing()) return;

    const input = document.getElementById('bulk-import-file') as HTMLInputElement | null;
    const file = input?.files?.[0];
    if (!file) {
      this.toast.error('Please choose an Excel file to import.');
      return;
    }

    this.resetResults();
    let parsed: ParsedImportRow[] = [];

    const aggregated = emptySummary();
    const allResults: BulkImportRowResult[] = [];
    let rowsProcessed = 0;

    try {
      this.phase.set('reading');
      this.progressPct.set(5);
      this.progressLabel.set('Reading Excel…');
      parsed = await parseBulkMemberImportExcel(file);
      if (parsed.length === 0) {
        this.toast.error('No data rows found in the Excel file.');
        this.phase.set('idle');
        return;
      }

      this.phase.set('validating');
      this.progressPct.set(10);
      this.progressLabel.set('Validating Records…');

      this.totalRecords.set(parsed.length);
      const importId = crypto.randomUUID();
      const createBatches = chunk(parsed, CREATE_BATCH_SIZE);

      this.phase.set('importing');
      for (let i = 0; i < createBatches.length; i++) {
        const batch = createBatches[i];
        const batchEnd = rowsProcessed + batch.length;
        this.progressPct.set(10 + Math.round((batchEnd / parsed.length) * 55));
        this.progressLabel.set(`Creating Members… (${batchEnd} / ${parsed.length})`);

        const res = await firstValueFrom(
          this.api
            .processBatch({
              rows: batch.map((r) => ({
                rowNumber: r.rowNumber,
                fullName: r.name,
                email: r.email || null,
                phone: r.mobile || null,
                createdAt: r.createdAtIso,
              })),
              importId,
              batchIndex: i,
              totalBatches: createBatches.length,
              totalRecords: parsed.length,
              sendWelcomeEmails: false,
            })
            .pipe(timeout(CREATE_TIMEOUT_MS)),
        );

        if (!res?.success) {
          throw new Error(res?.message || 'Import batch failed.');
        }

        mergeSummary(aggregated, normalizeImportSummary(res.summary as Record<string, unknown>));
        if (res.results?.length) {
          allResults.push(...res.results.map((r) => normalizeImportRow(r as Record<string, unknown>)));
        }
        rowsProcessed += batch.length;
      }

      const emailQueue = buildEmailQueue(parsed, allResults);
      await this.runEmailBatches(emailQueue, importId, aggregated, allResults);

      this.finalizeImport(aggregated, allResults, emailQueue, false);
      this.toast.success('Bulk member import completed.');
    } catch (e: unknown) {
      const emailQueue = buildEmailQueue(parsed, allResults);
      const hasPartial = allResults.length > 0 || aggregated.imported > 0;
      if (hasPartial) {
        this.finalizeImport(aggregated, allResults, emailQueue, true);
        this.toast.error(
          getHttpErrorMessage(
            e,
            `Import stopped after ${aggregated.imported} member(s). Use "Send welcome emails" to retry.`,
          ),
        );
      } else {
        this.phase.set('failed');
        this.progressLabel.set('Import Failed');
        this.toast.error(getHttpErrorMessage(e, 'Import failed. Please try again.'));
      }
    }
  }

  /** Resend welcome emails for members already in the system (re-upload same Excel). */
  async sendEmailsFromFile() {
    if (this.importing()) return;

    const input = document.getElementById('bulk-import-file') as HTMLInputElement | null;
    const file = input?.files?.[0];
    if (!file) {
      this.toast.error('Please choose an Excel file.');
      return;
    }

    const aggregated = emptySummary();
    const allResults: BulkImportRowResult[] = [];

    try {
      this.phase.set('reading');
      this.progressPct.set(10);
      this.progressLabel.set('Reading Excel…');
      const parsed = await parseBulkMemberImportExcel(file);
      const withContact = parsed.filter((r) => (r.email ?? '').trim() || (r.mobile ?? '').trim());
      if (withContact.length === 0) {
        this.toast.error('No rows with email or mobile found.');
        this.phase.set('idle');
        return;
      }

      this.totalRecords.set(withContact.length);
      const importId = crypto.randomUUID();
      const batches = chunk(withContact, EMAIL_BATCH_SIZE);

      this.phase.set('sendingEmails');
      for (let i = 0; i < batches.length; i++) {
        const batch = batches[i];
        const done = Math.min((i + 1) * EMAIL_BATCH_SIZE, withContact.length);
        this.progressPct.set(20 + Math.round((done / withContact.length) * 75));
        this.progressLabel.set(`Sending Emails… (${done} / ${withContact.length})`);

        const res = await firstValueFrom(
          this.api
            .sendEmailsLookupBatch({
              rows: batch.map((r) => ({
                rowNumber: r.rowNumber,
                customerName: r.name,
                email: r.email || null,
                phone: r.mobile || null,
              })),
              importId,
              batchIndex: i,
              totalBatches: batches.length,
            })
            .pipe(timeout(EMAIL_TIMEOUT_MS)),
        );

        if (!res?.success) throw new Error(res?.message || 'Email batch failed.');

        if (res.results?.length) {
          allResults.push(...res.results.map((r) => normalizeImportRow(r as Record<string, unknown>)));
        }
        const batchSummary = normalizeImportSummary(res.summary as Record<string, unknown>);
        if (batchSummary) {
          aggregated.emailsSent += batchSummary.emailsSent;
          aggregated.emailFailed += batchSummary.emailFailed;
          aggregated.skipped += batchSummary.skipped;
        }
      }

      aggregated.imported = allResults.filter((r) => r.status === 'Imported').length;
      this.finalizeImport(aggregated, allResults, [], false);
      this.toast.success(`Welcome emails sent: ${aggregated.emailsSent} successful.`);
    } catch (e: unknown) {
      this.phase.set('failed');
      this.toast.error(getHttpErrorMessage(e, 'Failed to send welcome emails.'));
    }
  }

  async resendPendingEmails() {
    const queue = this.pendingEmailRows();
    if (!queue.length || this.importing()) return;

    const aggregated = this.summary() ?? emptySummary();
    const allResults = [...this.successRows(), ...this.errorRows()];
    const importId = crypto.randomUUID();

    try {
      this.phase.set('sendingEmails');
      this.progressPct.set(65);
      this.progressLabel.set(`Sending Emails… (0 / ${queue.length})`);
      await this.runEmailBatches(queue, importId, aggregated, allResults);
      this.finalizeImport(aggregated, allResults, [], false);
      this.toast.success(`Welcome emails sent: ${aggregated.emailsSent} successful.`);
    } catch (e: unknown) {
      this.toast.error(getHttpErrorMessage(e, 'Failed to send welcome emails.'));
    }
  }

  async downloadErrorReport() {
    const rows = this.errorRows();
    if (!rows.length) {
      this.toast.error('No failed records to export.');
      return;
    }
    try {
      await exportBulkImportErrorReport(
        rows.map((r) => ({
          rowNumber: r.rowNumber,
          customerName: r.customerName,
          email: r.email,
          mobile: r.mobile,
          reason: r.reason,
        })),
      );
    } catch {
      this.toast.error('Unable to generate error report.');
    }
  }

  logout() {
    this.adminSession.logout();
    void this.router.navigateByUrl('/admin-login');
  }

  private async runEmailBatches(
    emailQueue: BulkImportEmailRowPayload[],
    importId: string,
    aggregated: BulkImportBatchSummary,
    allResults: BulkImportRowResult[],
  ) {
    if (emailQueue.length === 0) return;

    this.phase.set('sendingEmails');
    const emailBatches = chunk(emailQueue, EMAIL_BATCH_SIZE);
    let emailsDone = 0;

    for (let i = 0; i < emailBatches.length; i++) {
      const batch = emailBatches[i];
      emailsDone += batch.length;
      this.progressPct.set(65 + Math.round((emailsDone / emailQueue.length) * 30));
      this.progressLabel.set(`Sending Emails… (${emailsDone} / ${emailQueue.length})`);

      const res = await firstValueFrom(
        this.api
          .sendEmailsBatch({
            rows: batch,
            importId,
            batchIndex: i,
            totalBatches: emailBatches.length,
          })
          .pipe(timeout(EMAIL_TIMEOUT_MS)),
      );

      if (!res?.success) {
        throw new Error(res?.message || 'Email batch failed.');
      }

      if (res.results?.length) {
        applyEmailResults(allResults, res.results.map((r) => normalizeImportRow(r as Record<string, unknown>)));
      }
      const batchSummary = normalizeImportSummary(res.summary as Record<string, unknown>);
      if (batchSummary) {
        aggregated.emailsSent += batchSummary.emailsSent;
        aggregated.emailFailed += batchSummary.emailFailed;
      }
    }
  }

  private finalizeImport(
    aggregated: BulkImportBatchSummary,
    allResults: BulkImportRowResult[],
    _emailQueue: BulkImportEmailRowPayload[],
    partial: boolean,
  ) {
    const pending: BulkImportEmailRowPayload[] = allResults
      .filter(
        (r) =>
          r.status === 'Imported' &&
          (r.email ?? '').trim() &&
          (r.memberId ?? '').trim() &&
          !r.emailSent,
      )
      .map((r) => ({
        rowNumber: r.rowNumber,
        customerName: (r.customerName ?? '').trim(),
        email: (r.email ?? '').trim(),
        memberId: (r.memberId ?? '').trim(),
      }));

    this.pendingEmailRows.set(pending);
    this.summary.set(aggregated);
    this.errorRows.set(
      allResults.flatMap((r) => {
        if (r.status !== 'Imported') return [r];
        if ((r.email ?? '').trim() && !r.emailSent) {
          return [{ ...r, reason: 'Email failed to send' }];
        }
        return [];
      }),
    );
    this.successRows.set(allResults.filter((r) => r.status === 'Imported'));
    this.progressPct.set(100);
    this.progressLabel.set('Completed');
    this.partialFailure.set(partial);
    this.phase.set('completed');
  }

  private resetResults() {
    this.summary.set(null);
    this.errorRows.set([]);
    this.successRows.set([]);
    this.pendingEmailRows.set([]);
    this.totalRecords.set(0);
    this.progressPct.set(0);
    this.progressLabel.set('');
    this.partialFailure.set(false);
    this.phase.set('idle');
  }
}

function buildEmailQueue(
  parsed: ParsedImportRow[],
  results: BulkImportRowResult[],
): BulkImportEmailRowPayload[] {
  const parsedByRow = new Map(parsed.map((p) => [p.rowNumber, p]));
  const queue: BulkImportEmailRowPayload[] = [];

  for (const result of results) {
    if (result.status !== 'Imported') continue;
    const source = parsedByRow.get(result.rowNumber);
    const email = (result.email || source?.email || '').trim();
    const memberId = (result.memberId || '').trim();
    if (!email || !memberId) continue;

    queue.push({
      rowNumber: result.rowNumber,
      customerName: (result.customerName || source?.name || '').trim(),
      email,
      memberId,
    });
  }

  return queue;
}

function emptySummary(): BulkImportBatchSummary {
  return {
    imported: 0,
    skipped: 0,
    emailsSent: 0,
    emailFailed: 0,
    withoutEmail: 0,
    withoutMobile: 0,
    duplicateMembers: 0,
    invalidRows: 0,
  };
}

function mergeSummary(target: BulkImportBatchSummary, batch: BulkImportBatchSummary | null) {
  if (!batch) return;
  target.imported += batch.imported;
  target.skipped += batch.skipped;
  target.withoutEmail += batch.withoutEmail;
  target.withoutMobile += batch.withoutMobile;
  target.duplicateMembers += batch.duplicateMembers;
  target.invalidRows += batch.invalidRows;
}

function applyEmailResults(all: BulkImportRowResult[], emailBatch: BulkImportRowResult[]) {
  const byRow = new Map(emailBatch.map((r) => [r.rowNumber, r]));
  for (let i = 0; i < all.length; i++) {
    const update = byRow.get(all[i].rowNumber);
    if (update) {
      all[i] = { ...all[i], emailSent: update.emailSent };
    }
  }
}

function chunk<T>(items: T[], size: number): T[][] {
  const out: T[][] = [];
  for (let i = 0; i < items.length; i += size) {
    out.push(items.slice(i, i + size));
  }
  return out;
}
