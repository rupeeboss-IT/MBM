import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MembershipPlanCheckoutService } from '../../services/membership-plan-checkout.service';

@Component({
  selector: 'app-membership-checkout-modals',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './membership-checkout-modals.html',
  styleUrl: './membership-checkout-modals.css',
})
export class MembershipCheckoutModals {
  readonly checkout = inject(MembershipPlanCheckoutService);
}
