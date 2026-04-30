import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-membership',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './membership.html',
  styleUrl: './membership.css',
})
export class Membership {
  openFaqIndex: number | null = null;

  toggleFaq(index: number) {
    this.openFaqIndex = this.openFaqIndex === index ? null : index;
  }
}
