import { Routes } from '@angular/router';
import { About } from './pages/about/about';
import { Home } from './pages/home/home';
import { OurServices } from './pages/OurServices/our-services/our-services';
import { ServicesDetails } from './pages/OurServices/services-details/services-details';
import { Schemes } from './pages/schemes/schemes';
import { SchemeDetail } from './pages/schemes/scheme-detail/scheme-detail';
import { Events } from './pages/events/events';
import { EventDetail } from './pages/events/event-detail/event-detail';
import { Loans } from './pages/loans/loans';
import { CreditRebuild } from './pages/credit-rebuild/credit-rebuild';
// import { Partners } from './pages/partners/partners';
import { Contact } from './pages/More/contact/contact';
import { Search } from './pages/More/search/search';
import { Membership } from './pages/More/membership/membership';
import { Connect } from './pages/connect/connect';
import { NewsBlog } from './pages/More/news-blog/news-blog';
import { Careers } from './pages/More/careers/careers';
import { MediaKit } from './pages/More/media-kit/media-kit';
import { Privacy } from './pages/privacy/privacy';
import { Terms } from './pages/terms/terms';
import { Disclaimer } from './pages/disclaimer/disclaimer';
import { Login as UserLogin } from './user/login/login';
import { Register } from './user/register/register';
import { Profile } from './user/profile/profile';
import { MyPlan } from './user/my-plan/my-plan';
import { authGuard } from './core/guards/auth.guard';
import { ArticleDetail } from './pages/More/news-blog/article-detail/article-detail';
import { ForgotPassword as UserForgotPassword } from './user/forgot-password/forgot-password';
import { Login as AdminLogin } from './admin/login/login';
import { ForgotPassword as AdminForgotPassword } from './admin/forgot-password/forgot-password';
import { OfferingDetails } from './pages/offering-details/offering-details';
import { ErrorPage } from './error-page/error-page';
import { adminGuard } from './core/guards/admin.guard';
import { AdminDashboard } from './admin/dashboard/dashboard';
import { AdminDashboardDetail } from './admin/dashboard-detail/dashboard-detail';
import { AdminUploadReports } from './admin/reports/upload-reports/upload-reports';
import { AdminGenerateSdr } from './admin/reports/generate-sdr/generate-sdr';
import { AdminViewReports } from './admin/reports/view-reports/view-reports';
import { UserManagementList } from './admin/user-management/user-management-list';
import { UserManagementDetail } from './admin/user-management/user-management-detail';
import { VendorList } from './admin/vendor-management/vendor-list/vendor-list';
import { VendorDetail } from './admin/vendor-management/vendor-detail/vendor-detail';
import { VendorPlanMapping } from './admin/vendor-management/vendor-plan-mapping/vendor-plan-mapping';
import { LeadDashboard } from './admin/lead-attribution/lead-dashboard/lead-dashboard';
import { LeadCustomerList } from './admin/lead-attribution/lead-customer-list/lead-customer-list';
import { LeadCustomerDetail } from './admin/lead-attribution/lead-customer-detail/lead-customer-detail';
import { EnquiryList } from './admin/enquiry-management/enquiry-list/enquiry-list';
import { EnquiryDetail } from './admin/enquiry-management/enquiry-detail/enquiry-detail';
import { CreditRepairList } from './admin/credit-repair/credit-repair-list/credit-repair-list';
import { ConnectList } from './admin/connect-management/connect-list/connect-list';
import { BulkMemberImport } from './admin/bulk-member-import/bulk-member-import';
import { superAdminGuard } from './core/guards/super-admin.guard';
import { BlogList } from './admin/blog-management/blog-list/blog-list';
import { BlogForm } from './admin/blog-management/blog-form/blog-form';
import { BlogCategoryList } from './admin/blog-management/blog-category-list/blog-category-list';
import { BlogCategoryForm } from './admin/blog-management/blog-category-form/blog-category-form';
import { BlogBadgeList } from './admin/blog-management/blog-badge-list/blog-badge-list';
import { BlogBadgeForm } from './admin/blog-management/blog-badge-form/blog-badge-form';
import { EventList } from './admin/event-management/event-list/event-list';
import { EventForm } from './admin/event-management/event-form/event-form';
import { EventCategoryList } from './admin/event-management/event-category-list/event-category-list';
import { EventCategoryForm } from './admin/event-management/event-category-form/event-category-form';
import { EventCityList } from './admin/event-management/event-city-list/event-city-list';
import { EventCityForm } from './admin/event-management/event-city-form/event-city-form';
import { SchemeList } from './admin/scheme-management/scheme-list/scheme-list';
import { SchemeForm } from './admin/scheme-management/scheme-form/scheme-form';
import { SchemeCategoryList } from './admin/scheme-management/scheme-category-list/scheme-category-list';
import { SchemeCategoryForm } from './admin/scheme-management/scheme-category-form/scheme-category-form';

export const routes: Routes = [
    {
        path: '',
        component: Home,
        pathMatch: 'full',
        data: {
            title: 'MSME Bharat Manch | Empowering 7 Crore+ MSMEs',
            description: 'India\'s #1 platform empowering 7 Crore+ MSMEs with 360° business solutions'
        }
    },
    {
        path: 'home',
        component: Home,
        pathMatch: 'full',
        data: {
            title: 'MSME Bharat Manch | Empowering 7 Crore+ MSMEs',
            description: 'India\'s #1 platform empowering 7 Crore+ MSMEs with 360° business solutions'
        }
    },
    {
        path: 'about-us',
        redirectTo: 'about',
        pathMatch: 'full'
    },
    {
        path: 'the-board',
        redirectTo: 'about',
        pathMatch: 'full'
    },
    {
        path: 'our-team',
        redirectTo: 'about',
        pathMatch: 'full'
    },
    {
        path: 'our-partners',
        redirectTo: 'about',
        pathMatch: 'full'
    },
    {
        path: 'our-mission',
        redirectTo: 'about',
        pathMatch: 'full'
    },
    {
        path: 'about',
        component: About,
        pathMatch: 'full',
        data: {
            title: 'About MSME Bharat Manch | Empowering 7 Crore+ MSMEs',
            description: 'India\'s #1 platform empowering 7 Crore+ MSMEs with 360° business solutions'
        }
    },
    {
        path: 'services',
        redirectTo: 'our-services',
        pathMatch: 'full'
    },
    {
        path: 'our-services',
        component: OurServices,
        pathMatch: 'full',
        data: {
            title: 'Our Services | MSME Bharat Manch',
            description: 'Explore our comprehensive range of 360° services designed to empower your MSME business'
        }
    },
    {
        path: 'service/human-resources',
        redirectTo: 'service/hr-recruitment',
        pathMatch: 'full'
    },
    {
        path: 'service/accounting-virtual-cfo',
        redirectTo: 'service/accounting-cfo',
        pathMatch: 'full'
    },
    {
        path: 'service/:slug',
        component: ServicesDetails
    },
    {
        path: 'services/:slug',
        redirectTo: 'service/:slug',
        pathMatch: 'full'
    },
    {
        path: 'schemes',
        component: Schemes,
        pathMatch: 'full',
        data: {
            title: 'Government Schemes for MSMEs | MSME Bharat Manch',
            description: 'Explore PMEGP, CGTMSE, MUDRA and other government schemes tailored for Indian MSMEs'
        }
    },
    {
        path: 'scheme/:slug',
        component: SchemeDetail
    },
    {
        path: 'events',
        component: Events,
        pathMatch: 'full',
        data: {
            title: 'MSME Events & Summits | MSME Bharat Manch',
            description: 'Attend exclusive MSME networking events, business summits and knowledge sessions hosted by MSME Bharat Manch'
        }
    },
    {
        path: 'event/:slug',
        component: EventDetail
    },
    {
        path: 'finance',
        redirectTo: 'loans',
        pathMatch: 'full'
    },
    {
        path: 'loans',
        component: Loans,
        pathMatch: 'full',
        data: {
            title: 'MSME Loans & Finance | MSME Bharat Manch',
            description: 'Get the right business loan for your MSME — term loans, working capital, CGTMSE-backed credit and more'
        }
    },
    {
        path: 'credit-rebuild',
        component: CreditRebuild,
        pathMatch: 'full',
        data: {
            title: 'Credit Rebuild | MSME Bharat Manch',
            description: 'RBI-compliant credit bureau correction for individuals and MSMEs — CIBIL, Experian, CRIF & Equifax'
        }
    },
    {
        path: 'partners',
        redirectTo: 'about',
        pathMatch: 'full'
    },
    {
        path: 'contact-us',
        redirectTo: 'contact',
        pathMatch: 'full'
    },
    {
        path: 'contact',
        component: Contact,
        pathMatch: 'full',
        data: {
            title: 'Contact Us | MSME Bharat Manch',
            description: 'Get in touch with the MSME Bharat Manch team for business solutions, partnerships or support'
        }
    },
    {
        path: 'search',
        component: Search,
        pathMatch: 'full'
    },
    {
        path: 'membership',
        component: Membership,
        pathMatch: 'full',
        data: {
            title: 'MSME Membership Plans | MSME Bharat Manch',
            description: 'Join MSME Bharat Manch and unlock exclusive benefits — loans, government schemes, technology solutions and expert advisory'
        }
    },
    {
        path: 'connect',
        component: Connect,
        pathMatch: 'full',
        data: {
            title: 'MSME Connect | Business Networking | MSME Bharat Manch',
            description: 'Connect with verified MSME vendors, suppliers and business partners across India on MSME Bharat Manch'
        }
    },
    {
        path: 'offering/:slug',
        component: OfferingDetails
    },
    {
        path: 'blog',
        redirectTo: 'news',
        pathMatch: 'full'
    },
    {
        path: 'news-blog',
        redirectTo: 'news',
        pathMatch: 'full'
    },
    {
        path: 'news',
        component: NewsBlog,
        pathMatch: 'full',
        data: {
            title: 'MSME News & Insights | MSME Bharat Manch',
            description: 'Stay updated with the latest MSME news, government policy updates, business tips and expert insights'
        }
    },
    {
        path: 'article/:slug',
        component: ArticleDetail
    },
    {
        path: 'careers',
        component: Careers,
        pathMatch: 'full',
        data: {
            title: 'Careers at MSME Bharat Manch | Join Our Team',
            description: 'Explore career opportunities at MSME Bharat Manch and help empower 7 Crore+ MSMEs across India'
        }
    },
    {
        path: 'media',
        component: MediaKit,
        pathMatch: 'full',
        data: {
            title: 'Media Kit | MSME Bharat Manch',
            description: 'Download the official MSME Bharat Manch media kit — brand assets, press releases and media contact'
        }
    },
    {
        path: 'privacy',
        redirectTo: 'privacy-policy',
        pathMatch: 'full'
    },
    {
        path: 'privacy-policy',
        component: Privacy,
        pathMatch: 'full',
        data: {
            title: 'Privacy Policy | MSME Bharat Manch',
            description: 'Read the MSME Bharat Manch privacy policy to understand how we collect, use and protect your data'
        }
    },
    {
        path: 'terms',
        component: Terms,
        pathMatch: 'full',
        data: {
            title: 'Terms & Conditions | MSME Bharat Manch',
            description: 'Review the terms and conditions governing your use of the MSME Bharat Manch platform and services'
        }
    },
    {
        path: 'disclaimer',
        component: Disclaimer,
        pathMatch: 'full',
        data: {
            title: 'Disclaimer | MSME Bharat Manch',
            description: 'Read the disclaimer for MSME Bharat Manch regarding information accuracy and advisory limitations'
        }
    },
    {
        path: 'member/login',
        redirectTo: '/login',
        pathMatch: 'full'
    },
    {
        path: 'login',
        component: UserLogin,
        pathMatch: 'full'
    },
    {
        path: 'forgot-password',
        component: UserForgotPassword,
        pathMatch: 'full'
    },
    {
        path: 'admin-login',
        component: AdminLogin,
        pathMatch: 'full'
    },
    {
        path: 'admin-forgot-password',
        component: AdminForgotPassword,
        pathMatch: 'full'
    },
    {
        path: 'admin-dashboard',
        component: AdminDashboard,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin-dashboard/detail/:category',
        component: AdminDashboardDetail,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/members',
        redirectTo: 'admin-dashboard/detail/members',
        pathMatch: 'full'
    },
    {
        path: 'admin/user-management/:role/:userId',
        component: UserManagementDetail,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/user-management/:role',
        component: UserManagementList,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/users',
        redirectTo: 'admin/user-management/admins',
        pathMatch: 'full'
    },
    {
        path: 'admin/users/create',
        redirectTo: 'admin/user-management/admins',
        pathMatch: 'full'
    },
    {
        path: 'admin-reports/upload',
        component: AdminUploadReports,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin-reports/sdr',
        component: AdminGenerateSdr,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin-reports/history',
        component: AdminViewReports,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin-reports',
        redirectTo: 'admin-reports/upload',
        pathMatch: 'full'
    },
    {
        path: 'admin/vendor-management/vendors/:vendorId',
        component: VendorDetail,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/vendor-management/vendors',
        component: VendorList,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/vendor-management/plan-mapping',
        component: VendorPlanMapping,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/vendor-management',
        redirectTo: 'admin/vendor-management/vendors',
        pathMatch: 'full'
    },
    {
        path: 'admin/lead-attribution/customers/:userId',
        component: LeadCustomerDetail,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/lead-attribution/customers',
        component: LeadCustomerList,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/lead-attribution',
        component: LeadDashboard,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/enquiry-management/enquiries/:enquiryId',
        component: EnquiryDetail,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/enquiry-management/enquiries',
        component: EnquiryList,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/enquiry-management/new',
        component: EnquiryList,
        canActivate: [adminGuard],
        data: { view: 'new' },
    },
    {
        path: 'admin/enquiry-management/resolved',
        component: EnquiryList,
        canActivate: [adminGuard],
        data: { view: 'resolved' },
    },
    {
        path: 'admin/enquiry-management/closed',
        component: EnquiryList,
        canActivate: [adminGuard],
        data: { view: 'closed' },
    },
    {
        path: 'admin/enquiry-management',
        redirectTo: 'admin/enquiry-management/enquiries',
        pathMatch: 'full'
    },
    {
        path: 'admin/credit-repair/leads',
        component: CreditRepairList,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/credit-repair',
        redirectTo: 'admin/credit-repair/leads',
        pathMatch: 'full',
    },
    {
        path: 'admin/connect-management/listings',
        component: ConnectList,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/connect-management',
        redirectTo: 'admin/connect-management/listings',
        pathMatch: 'full'
    },
    {
        path: 'admin/bulk-member-import',
        component: BulkMemberImport,
        canActivate: [adminGuard, superAdminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/blog-management',
        component: BlogList,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/blog-management/new',
        component: BlogForm,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/blog-management/edit/:blogId',
        component: BlogForm,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/blog-categories',
        component: BlogCategoryList,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/blog-categories/new',
        component: BlogCategoryForm,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/blog-categories/edit/:categoryId',
        component: BlogCategoryForm,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/blog-badges',
        component: BlogBadgeList,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/blog-badges/new',
        component: BlogBadgeForm,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/blog-badges/edit/:badgeId',
        component: BlogBadgeForm,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/event-management',
        component: EventList,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/event-management/new',
        component: EventForm,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/event-management/edit/:eventId',
        component: EventForm,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/event-categories',
        component: EventCategoryList,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/event-categories/new',
        component: EventCategoryForm,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/event-categories/edit/:categoryId',
        component: EventCategoryForm,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/event-cities',
        component: EventCityList,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/event-cities/new',
        component: EventCityForm,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/event-cities/edit/:cityId',
        component: EventCityForm,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/scheme-management',
        component: SchemeList,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/scheme-management/new',
        component: SchemeForm,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/scheme-management/edit/:schemeId',
        component: SchemeForm,
        canActivate: [adminGuard],
    },
    {
        path: 'admin/scheme-categories',
        component: SchemeCategoryList,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/scheme-categories/new',
        component: SchemeCategoryForm,
        canActivate: [adminGuard],
        pathMatch: 'full'
    },
    {
        path: 'admin/scheme-categories/edit/:categoryId',
        component: SchemeCategoryForm,
        canActivate: [adminGuard],
    },
    {
        path: 'register',
        component: Register,
        pathMatch: 'full'
    },
    {
        path: 'profile',
        component: Profile,
        canActivate: [authGuard],
        pathMatch: 'full'
    },
    {
        path: 'my-plan',
        component: MyPlan,
        canActivate: [authGuard],
        pathMatch: 'full'
    },
    {
        path: '**',
        component: ErrorPage
    }
];
