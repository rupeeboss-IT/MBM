/** Context-specific fallback messages when the API returns nothing safe. */
export const API_USER_MESSAGES = {
  generic: 'Something went wrong. Please try again later.',
  genericAction: 'Something went wrong while processing your request.',
  network: 'Unable to reach the server. Check your connection and try again.',
  login: 'Unable to sign in. Please verify your credentials.',
  register: 'Unable to complete registration. Please try again.',
  forgotPassword: 'Unable to process your password reset request. Please try again.',
  resetPassword: 'Unable to reset your password. Please try again.',
  profile: 'Unable to load your profile. Please try again.',
  myPlan: 'Unable to load your plan details. Please try again.',
  membership: 'Unable to load membership options. Please try again.',
  payment: 'Unable to process payment. Please try again.',
  invoiceList: 'Unable to load invoices. Please try again.',
  invoiceDownload: 'Unable to download the invoice. Please try again.',
  reportHistory: 'Unable to load report history. Please try again later.',
  reportUpload: 'Unable to upload the report. Please try again.',
  reportDownload: 'Unable to download the report. Please try again.',
  reportSearch: 'Unable to retrieve search results. Please try again.',
  sdrGenerate: 'Unable to generate the scheme discovery report. Please try again.',
  dashboard: 'Unable to load dashboard data. Please try again.',
  save: 'Unable to save changes. Please try again.',
  delete: 'Unable to delete the record. Please try again.',
  search: 'Unable to retrieve results. Please try again.',
  loanApplication: 'Unable to submit your loan application. Please try again.',
  contact: 'Unable to send your message. Please try again.',
  schemeDiscovery: 'Unable to load scheme discovery status. Please try again later.',
  unauthorized: 'Your session has expired. Please sign in again.',
  forbidden: 'You do not have permission to perform this action.',
  notFound: 'The requested information was not found.',
} as const;

export function resolveApiFallback(url: string, method: string): string {
  const path = url.toLowerCase();
  const m = method.toUpperCase();

  if (path.includes('/admin/reports/sdr')) return API_USER_MESSAGES.sdrGenerate;
  if (path.includes('/admin/reports/history')) return API_USER_MESSAGES.reportHistory;
  if (path.includes('/admin/reports/upload')) return API_USER_MESSAGES.reportUpload;
  if (path.includes('/admin/reports') && path.includes('/customers/search')) return API_USER_MESSAGES.reportSearch;
  if (path.includes('/customer/reports') && path.includes('/download')) return API_USER_MESSAGES.reportDownload;
  if (path.includes('/customer/reports')) return API_USER_MESSAGES.reportHistory;
  if (path.includes('/payment') && path.includes('/download')) return API_USER_MESSAGES.invoiceDownload;
  if (path.includes('/payment') && path.includes('/invoices')) return API_USER_MESSAGES.invoiceList;
  if (path.includes('/payment') && m === 'POST') return API_USER_MESSAGES.payment;
  if (path.includes('/loans/apply')) return API_USER_MESSAGES.loanApplication;
  if (path.includes('/contact/submit')) return API_USER_MESSAGES.contact;
  if (path.includes('/scheme-discovery')) return API_USER_MESSAGES.schemeDiscovery;
  if (path.includes('/user/login')) return API_USER_MESSAGES.login;
  if (path.includes('/user/register')) return API_USER_MESSAGES.register;
  if (path.includes('/password/forgot')) return API_USER_MESSAGES.forgotPassword;
  if (path.includes('/password/reset')) return API_USER_MESSAGES.resetPassword;
  if (path.includes('/user/me')) return API_USER_MESSAGES.profile;
  if (path.includes('/my-plan')) return API_USER_MESSAGES.myPlan;
  if (path.includes('/membership') || path.includes('/payment/plans')) return API_USER_MESSAGES.membership;
  if (path.includes('/admin/dashboard')) return API_USER_MESSAGES.dashboard;
  if (path.includes('/admin') && m === 'DELETE') return API_USER_MESSAGES.delete;
  if (path.includes('/admin') && (m === 'POST' || m === 'PATCH' || m === 'PUT')) return API_USER_MESSAGES.save;
  if (path.includes('search')) return API_USER_MESSAGES.search;

  return API_USER_MESSAGES.generic;
}
