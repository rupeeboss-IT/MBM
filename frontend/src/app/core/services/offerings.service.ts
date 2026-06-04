import { Injectable } from '@angular/core';
import { OFFERING_DISPLAY_ORDER } from '../../data/offering-display-order';
import { OFFERINGS_DATA, type OfferingModel, type OfferingSlug } from '../../data/offerings.data';

@Injectable({ providedIn: 'root' })
export class OfferingsService {
  getOfferingBySlug(slug: string): OfferingModel | null {
    return (OFFERINGS_DATA as Record<string, OfferingModel>)[slug] ?? null;
  }

  getAllOfferings(): Array<{ slug: OfferingSlug; data: OfferingModel }> {
    const slugs = Object.keys(OFFERINGS_DATA) as OfferingSlug[];
    slugs.sort((a, b) => {
      const ai = OFFERING_DISPLAY_ORDER.indexOf(a);
      const bi = OFFERING_DISPLAY_ORDER.indexOf(b);
      const aRank = ai === -1 ? Number.MAX_SAFE_INTEGER : ai;
      const bRank = bi === -1 ? Number.MAX_SAFE_INTEGER : bi;
      return aRank - bRank;
    });
    return slugs.map((slug) => ({ slug, data: OFFERINGS_DATA[slug] }));
  }
}

