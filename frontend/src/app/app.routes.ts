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
import { Partners } from './pages/partners/partners';
import { Contact } from './pages/More/contact/contact';
import { Search } from './pages/More/search/search';
import { Membership } from './pages/More/membership/membership';
import { NewsBlog } from './pages/More/news-blog/news-blog';
import { Careers } from './pages/More/careers/careers';
import { MediaKit } from './pages/More/media-kit/media-kit';
import { Privacy } from './pages/privacy/privacy';
import { Terms } from './pages/terms/terms';
import { Disclaimer } from './pages/disclaimer/disclaimer';
import { Login as UserLogin } from './user/login/login';
import { Register } from './user/register/register';
import { Profile } from './user/profile/profile';
import { authGuard } from './core/guards/auth.guard';
import { ArticleDetail } from './pages/More/news-blog/article-detail/article-detail';
import { ForgotPassword as UserForgotPassword } from './user/forgot-password/forgot-password';
import { Login as AdminLogin } from './admin/login/login';
import { ForgotPassword as AdminForgotPassword } from './admin/forgot-password/forgot-password';
import { OfferingDetails } from './pages/offering-details/offering-details';
import { ErrorPage } from './error-page/error-page';

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
        path: 'about',
        component: About,
        pathMatch: 'full',
        data: {
            title: 'About MSME Bharat Manch | Empowering 7 Crore+ MSMEs',
            description: 'India\'s #1 platform empowering 7 Crore+ MSMEs with 360° business solutions'
        }
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
        path: 'loans',
        component: Loans,
        pathMatch: 'full'
    },
    {
        path: 'partners',
        component: Partners,
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
        path: 'offering/:slug',
        component: OfferingDetails
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
        path: '**',
        component: ErrorPage
    }
];
