import { Injectable } from '@angular/core';
import { OFFERINGS_DATA, type OfferingModel, type OfferingSlug } from '../../data/offerings.data';

@Injectable({ providedIn: 'root' })
export class OfferingsService {
  getOfferingBySlug(slug: string): OfferingModel | null {
    return (OFFERINGS_DATA as Record<string, OfferingModel>)[slug] ?? null;
  }

  getAllOfferings(): Array<{ slug: OfferingSlug; data: OfferingModel }> {
    return (Object.keys(OFFERINGS_DATA) as OfferingSlug[]).map((slug) => ({
      slug,
      data: OFFERINGS_DATA[slug]
    }));
  }
}

