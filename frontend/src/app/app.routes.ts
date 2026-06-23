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
import { ConnectList } from './admin/connect-management/connect-list/connect-list';

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
        pathMatch: 'full'
    },
    {
        path: 'scheme/:slug',
        component: SchemeDetail
    },
    {
        path: 'events',
        component: Events,
        pathMatch: 'full'
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
        pathMatch: 'full'
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
        pathMatch: 'full'
    },
    {
        path: 'search',
        component: Search,
        pathMatch: 'full'
    },
    {
        path: 'membership',
        component: Membership,
        pathMatch: 'full'
    },
    {
        path: 'connect',
        component: Connect,
        pathMatch: 'full'
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
        pathMatch: 'full'
    },
    {
        path: 'article/:slug',
        component: ArticleDetail
    },
    {
        path: 'careers',
        component: Careers,
        pathMatch: 'full'
    },
    {
        path: 'media',
        component: MediaKit,
        pathMatch: 'full'
    },
    {
        path: 'privacy',
        redirectTo: 'privacy-policy',
        pathMatch: 'full'
    },
    {
        path: 'privacy-policy',
        component: Privacy,
        pathMatch: 'full'
    },
    {
        path: 'terms',
        component: Terms,
        pathMatch: 'full'
    },
    {
        path: 'disclaimer',
        component: Disclaimer,
        pathMatch: 'full'
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
