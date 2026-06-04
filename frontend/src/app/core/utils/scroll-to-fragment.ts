import { Router } from '@angular/router';

/** Scroll to the current route fragment (accounts for fixed header). */
export function scrollToRouteFragment(router: Router, headerOffsetPx = 88): void {
  const fragment = router.parseUrl(router.url).fragment;
  if (!fragment) return;

  const scroll = () => {
    const el = document.getElementById(fragment);
    if (!el) return;
    const top = el.getBoundingClientRect().top + window.scrollY - headerOffsetPx;
    window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
  };

  requestAnimationFrame(() => requestAnimationFrame(scroll));
}
