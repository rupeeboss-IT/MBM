import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  private readonly router = inject(Router);

  goToPlan(planCode: 'premium' | 'pro') {
    try {
      window.localStorage.setItem('mbm_pending_plan', planCode);
    } catch {
      // ignore storage errors
    }
    this.router.navigateByUrl('/membership');
  }
}
