export type AdminDropdown =
  | 'people'
  | 'crm'
  | 'finance'
  | 'partners'
  | 'content'
  | 'connect'
  | 'administration'
  | 'account';

export type AdminNavLink = {
  label: string;
  route: string;
  queryParams?: Record<string, string | number>;
  exact?: boolean;
  pathsSubset?: boolean;
  superAdminOnly?: boolean;
};

export type AdminNavSection = {
  label: string;
  children: AdminNavLink[];
};

export type AdminNavGroup = {
  id: Exclude<AdminDropdown, 'account'>;
  label: string;
  prefixes: string[];
  sections: AdminNavSection[];
};

export const ADMIN_MENU_GROUPS: AdminNavGroup[] = [
  {
    id: 'people',
    label: 'People & Membership',
    prefixes: [
      '/admin/user-management',
      '/admin-dashboard/detail/members',
      '/admin-dashboard/detail/subscriptions',
      '/admin-dashboard/detail/plans',
      '/admin-dashboard/detail/expiring',
      '/admin-dashboard/detail/expired',
    ],
    sections: [
      {
        label: 'User administration',
        children: [
          {
            label: 'Admin users',
            route: '/admin/user-management/admins',
            superAdminOnly: true,
          },
          { label: 'Partners', route: '/admin/user-management/partners' },
          { label: 'Member accounts', route: '/admin/user-management/members' },
        ],
      },
      {
        label: 'Membership operations',
        children: [
          { label: 'Members overview', route: '/admin-dashboard/detail/members' },
          { label: 'Subscriptions', route: '/admin-dashboard/detail/subscriptions' },
          { label: 'Membership plans', route: '/admin-dashboard/detail/plans' },
          {
            label: 'Expiring soon',
            route: '/admin-dashboard/detail/expiring',
            queryParams: { days: 30 },
          },
        ],
      },
    ],
  },
  {
    id: 'crm',
    label: 'CRM & Enquiries',
    prefixes: ['/admin/enquiry-management', '/admin/lead-attribution', '/admin/credit-repair'],
    sections: [
      {
        label: 'Enquiry management',
        children: [
          {
            label: 'All enquiries',
            route: '/admin/enquiry-management/enquiries',
            exact: true,
          },
          { label: 'New enquiries', route: '/admin/enquiry-management/new' },
          { label: 'Resolved enquiries', route: '/admin/enquiry-management/resolved' },
          { label: 'Closed enquiries', route: '/admin/enquiry-management/closed' },
        ],
      },
      {
        label: 'Credit repair',
        children: [
          { label: 'Credit repair leads', route: '/admin/credit-repair/leads' },
        ],
      },
      {
        label: 'Lead attribution',
        children: [
          {
            label: 'Attribution dashboard',
            route: '/admin/lead-attribution',
            exact: true,
          },
          { label: 'Lead customers', route: '/admin/lead-attribution/customers' },
        ],
      },
    ],
  },
  {
    id: 'finance',
    label: 'Finance & Reports',
    prefixes: [
      '/admin-dashboard/detail/payments',
      '/admin-dashboard/detail/payment-orders',
      '/admin-reports',
    ],
    sections: [
      {
        label: 'Payments & billing',
        children: [
          { label: 'Payments', route: '/admin-dashboard/detail/payments' },
          { label: 'Payment orders', route: '/admin-dashboard/detail/payment-orders' },
        ],
      },
      {
        label: 'Customer reports',
        children: [
          {
            label: 'Upload reports',
            route: '/admin-reports/upload',
            pathsSubset: true,
          },
          {
            label: 'Generate SDR',
            route: '/admin-reports/sdr',
            pathsSubset: true,
          },
          { label: 'Report history', route: '/admin-reports/history' },
        ],
      },
    ],
  },
  {
    id: 'partners',
    label: 'Partners & Vendors',
    prefixes: ['/admin/vendor-management'],
    sections: [
      {
        label: 'Vendor management',
        children: [
          { label: 'Vendors', route: '/admin/vendor-management/vendors' },
          { label: 'Vendor plan mapping', route: '/admin/vendor-management/plan-mapping' },
        ],
      },
    ],
  },
  {
    id: 'content',
    label: 'Content',
    prefixes: [
      '/admin-dashboard/detail/blogs',
      '/admin-dashboard/detail/events',
      '/admin-dashboard/detail/schemes',
    ],
    sections: [
      {
        label: 'Website content',
        children: [
          { label: 'Blogs & news', route: '/admin-dashboard/detail/blogs' },
          { label: 'Events', route: '/admin-dashboard/detail/events' },
          { label: 'Schemes', route: '/admin-dashboard/detail/schemes' },
        ],
      },
    ],
  },
  {
    id: 'connect',
    label: 'MSME Connect',
    prefixes: ['/admin/connect-management'],
    sections: [
      {
        label: 'Network management',
        children: [
          { label: 'Connect listings', route: '/admin/connect-management/listings' },
        ],
      },
    ],
  },
  {
    id: 'administration',
    label: 'Administration',
    prefixes: ['/admin/bulk-member-import'],
    sections: [
      {
        label: 'Data migration',
        children: [
          {
            label: 'Bulk Member Import',
            route: '/admin/bulk-member-import',
            superAdminOnly: true,
          },
        ],
      },
    ],
  },
];
