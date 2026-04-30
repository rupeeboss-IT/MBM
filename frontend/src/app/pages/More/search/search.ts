import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ServicesService } from '../../../core/services/services.service';

@Component({
  selector: 'app-search',
  imports: [CommonModule, FormsModule],
  templateUrl: './search.html',
  styleUrl: './search.css',
})
export class Search {
  query = '';
  serviceResults: Array<{ slug: string; title: string; subtitle: string; emoji: string }> = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private services: ServicesService
  ) {
    this.route.queryParamMap.subscribe((params) => {
      const q = (params.get('q') ?? '').trim();
      this.query = q;
      this.performSearch(q);
    });
  }

  submitSearch() {
    const q = this.query.trim();
    this.router.navigate(['/search'], { queryParams: q ? { q } : {} });
    this.performSearch(q);
  }

  goToService(event: MouseEvent, slug: string) {
    event.preventDefault();
    this.router.navigate(['/service', slug]);
  }

  private performSearch(q: string) {
    if (!q) {
      this.serviceResults = [];
      return;
    }
    const needle = q.toLowerCase();
    this.serviceResults = this.services
      .getAllServices()
      .filter(({ data }) => {
        const hay = [
          data.title,
          data.subtitle,
          data.cat,
          ...(data.benefits ?? []),
          ...(data.targets ?? [])
        ]
          .join(' ')
          .toLowerCase();
        return hay.includes(needle);
      })
      .map(({ slug, data }) => ({
        slug,
        title: data.title,
        subtitle: data.subtitle,
        emoji: data.emoji
      }));
  }
}
