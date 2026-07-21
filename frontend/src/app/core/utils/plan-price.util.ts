/** Format plan base price in rupees from paise (Indian grouping). */
export function formatPlanBasePrice(paise: number, yearSuffix = false): string {
  const rupees = paise / 100;
  const formatted = rupees.toLocaleString('en-IN', { maximumFractionDigits: 0 });
  return yearSuffix ? `₹${formatted}/yr` : `₹${formatted}`;
}

/** Convert rupee input to paise for API. */
export function rupeesToPaise(rupees: number): number {
  if (!Number.isFinite(rupees) || rupees < 0) return 0;
  return Math.round(rupees * 100);
}

/** Convert paise to rupees for form display. */
export function paiseToRupees(paise: number): number {
  return paise / 100;
}
