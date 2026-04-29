import { Injectable } from '@angular/core';
import { SCHEMES_DATA, type SchemeModel, type SchemeSlug } from '../../data/schemes.data';

@Injectable({ providedIn: 'root' })
export class SchemesService {
  getSchemeBySlug(slug: string): SchemeModel | null {
    return (SCHEMES_DATA as Record<string, SchemeModel>)[slug] ?? null;
  }

  getAllSchemes(): Array<{ slug: SchemeSlug; data: SchemeModel }> {
    return (Object.keys(SCHEMES_DATA) as SchemeSlug[]).map((slug) => ({
      slug,
      data: SCHEMES_DATA[slug]
    }));
  }
}

