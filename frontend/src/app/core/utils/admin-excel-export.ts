import ExcelJS from 'exceljs';

export type ExportColumn<T> = {
  header: string;
  value: (row: T) => unknown;
  type?: 'datetime' | 'number' | 'currency_paise';
};

function formatCellValue(value: unknown, type?: ExportColumn<unknown>['type']): string | number {
  if (value == null) return '';
  if (type === 'currency_paise' && typeof value === 'number') {
    return Math.round(value) / 100;
  }
  if (type === 'number' && typeof value === 'number') return value;
  if (type === 'datetime' && value) {
    const d = new Date(String(value));
    return Number.isNaN(d.getTime()) ? String(value) : d.toLocaleString('en-IN');
  }
  return String(value);
}

export async function exportToExcel<T>(
  filename: string,
  columns: ExportColumn<T>[],
  rows: T[],
  opts?: { sheetTitle?: string },
): Promise<void> {
  const workbook = new ExcelJS.Workbook();
  const sheet = workbook.addWorksheet((opts?.sheetTitle ?? 'Export').slice(0, 31));
  sheet.addRow(columns.map((c) => c.header));
  for (const row of rows) {
    sheet.addRow(columns.map((c) => formatCellValue(c.value(row), c.type)));
  }

  const buffer = await workbook.xlsx.writeBuffer();
  const stamp = new Date().toISOString().slice(0, 10);
  const blob = new Blob([buffer], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = `${filename}-${stamp}.xlsx`;
  anchor.click();
  URL.revokeObjectURL(url);
}
