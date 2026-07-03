import ExcelJS from 'exceljs';

export type ParsedImportRow = {
  rowNumber: number;
  name: string;
  mobile: string;
  email: string;
  createdAt: string | null;
  createdAtIso: string | null;
};

const EXPECTED_HEADERS = ['name', 'mobile number', 'email', 'created at'];

export async function parseBulkMemberImportExcel(file: File): Promise<ParsedImportRow[]> {
  const buffer = await file.arrayBuffer();
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.load(buffer);

  const sheet = workbook.worksheets[0];
  if (!sheet) throw new Error('The Excel file does not contain any worksheets.');

  const headerRow = sheet.getRow(1);
  const headerMap = new Map<number, string>();
  headerRow.eachCell((cell, col) => {
    const label = String(cell.value ?? '')
      .trim()
      .toLowerCase();
    if (label) headerMap.set(col, label);
  });

  const mapped = [...headerMap.values()];
  const hasRequired = EXPECTED_HEADERS.every((h) => mapped.includes(h));
  if (!hasRequired) {
    throw new Error('Invalid template. Required columns: Name, Mobile Number, Email, Created At.');
  }

  const colByHeader = new Map<string, number>();
  for (const [col, label] of headerMap.entries()) {
    colByHeader.set(label, col);
  }

  const rows: ParsedImportRow[] = [];
  const nameCol = colByHeader.get('name')!;
  const mobileCol = colByHeader.get('mobile number')!;
  const emailCol = colByHeader.get('email')!;
  const createdCol = colByHeader.get('created at')!;

  sheet.eachRow((row, rowNumber) => {
    if (rowNumber === 1) return;

    const name = cellText(row.getCell(nameCol));
    const mobile = cellText(row.getCell(mobileCol));
    const email = cellText(row.getCell(emailCol));
    const createdRaw = cellText(row.getCell(createdCol));
    const createdAtIso = parseCreatedAt(createdRaw, row.getCell(createdCol).value);

    if (!name && !mobile && !email && !createdRaw) return;

    rows.push({
      rowNumber,
      name,
      mobile,
      email,
      createdAt: createdRaw || null,
      createdAtIso,
    });
  });

  return rows;
}

function cellText(cell: ExcelJS.Cell): string {
  const value = cell.value;
  if (value == null) return '';
  if (typeof value === 'object' && value !== null && 'text' in value) {
    return String((value as { text?: string }).text ?? '').trim();
  }
  if (value instanceof Date) return value.toLocaleString('en-IN');
  return String(value).trim();
}

function parseCreatedAt(raw: string, cellValue: ExcelJS.CellValue): string | null {
  if (cellValue instanceof Date && !Number.isNaN(cellValue.getTime())) {
    return formatLocalIso(cellValue);
  }

  const text = raw.trim();
  if (!text) return null;

  const direct = new Date(text);
  if (!Number.isNaN(direct.getTime())) return formatLocalIso(direct);

  const patterns = [
    /^(\d{1,2})\s+([A-Za-z]{3})\s+(\d{4}),?\s+(\d{1,2}):(\d{2})\s*(AM|PM)$/i,
  ];
  const match = text.match(patterns[0]);
  if (match) {
    const [, day, mon, year, hour, minute, ampm] = match;
    const monthIndex = new Date(`${mon} 1, 2000`).getMonth();
    let h = Number(hour);
    const isPm = ampm.toUpperCase() === 'PM';
    if (isPm && h < 12) h += 12;
    if (!isPm && h === 12) h = 0;
    const d = new Date(Number(year), monthIndex, Number(day), h, Number(minute), 0);
    if (!Number.isNaN(d.getTime())) return formatLocalIso(d);
  }

  return null;
}

function formatLocalIso(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

export async function downloadBulkMemberImportSample(): Promise<void> {
  const workbook = new ExcelJS.Workbook();
  const sheet = workbook.addWorksheet('Bulk Member Import');
  sheet.addRow(['Name', 'Mobile Number', 'Email', 'Created At']);
  sheet.addRow(['Sharad Elikapeli', '9876543210', 'abc@gmail.com', '01 Feb 2025, 08:28 AM']);
  sheet.addRow(['Priya Sharma', '9123456780', 'priya@example.com', '15 Mar 2024, 10:15 AM']);
  sheet.addRow(['Ramesh Kumar', '9988776655', '', '20 Jan 2025, 02:00 PM']);
  sheet.addRow(['Anita Desai', '', 'anita@example.com', '05 Dec 2024, 09:45 AM']);

  sheet.getRow(1).font = { bold: true };
  sheet.columns = [{ width: 24 }, { width: 18 }, { width: 28 }, { width: 26 }];

  const buffer = await workbook.xlsx.writeBuffer();
  const blob = new Blob([buffer], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = 'bulk-member-import-sample.xlsx';
  anchor.click();
  URL.revokeObjectURL(url);
}

export async function exportBulkImportErrorReport(
  rows: Array<{
    rowNumber: number;
    customerName?: string | null;
    email?: string | null;
    mobile?: string | null;
    reason?: string | null;
  }>,
): Promise<void> {
  const workbook = new ExcelJS.Workbook();
  const sheet = workbook.addWorksheet('Import Errors');
  sheet.addRow(['Row Number', 'Customer Name', 'Email', 'Mobile', 'Reason']);
  for (const row of rows) {
    sheet.addRow([
      row.rowNumber,
      row.customerName ?? '',
      row.email ?? '',
      row.mobile ?? '',
      row.reason ?? '',
    ]);
  }
  sheet.getRow(1).font = { bold: true };
  sheet.columns = [{ width: 12 }, { width: 28 }, { width: 28 }, { width: 16 }, { width: 36 }];

  const buffer = await workbook.xlsx.writeBuffer();
  const stamp = new Date().toISOString().slice(0, 10);
  const blob = new Blob([buffer], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = `bulk-member-import-errors-${stamp}.xlsx`;
  anchor.click();
  URL.revokeObjectURL(url);
}
