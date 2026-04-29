import { Injectable } from '@angular/core';
import { SERVICES_DATA, type ServiceModel, type ServiceSlug } from '../../data/services.data';

@Injectable({ providedIn: 'root' })
export class ServicesService {
  getServiceBySlug(slug: string): ServiceModel | null {
    return (SERVICES_DATA as Record<string, ServiceModel>)[slug] ?? null;
  }

  getAllServices(): Array<{ slug: ServiceSlug; data: ServiceModel }> {
    return (Object.keys(SERVICES_DATA) as ServiceSlug[]).map((slug) => ({
      slug,
      data: SERVICES_DATA[slug]
    }));
  }
}

