import { CommonModule } from '@angular/common';

import { Component, computed, inject, signal } from '@angular/core';

import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { firstValueFrom, timeout } from 'rxjs';

import { ToastService } from '../../../core/services/toast.service';

import {

  LeadAttributionService,

  type LeadCustomerDetail as LeadCustomerDetailModel,

  type LeadPaymentHistoryItem,

} from '../../../core/services/lead-attribution.service';

import { getHttpErrorMessage } from '../../../core/utils/http-error-message';

import { LocalDatePipe } from '../../../core/pipes/local-date.pipe';



@Component({

  selector: 'app-lead-customer-detail',

  standalone: true,

  imports: [CommonModule, RouterLink, LocalDatePipe],

  templateUrl: './lead-customer-detail.html',

  styleUrls: ['../../admin-shared.css', './lead-customer-detail.css'],

})

export class LeadCustomerDetail {

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  private readonly api = inject(LeadAttributionService);

  private readonly toast = inject(ToastService);



  readonly loading = signal(true);

  readonly customer = signal<LeadCustomerDetailModel | null>(null);

  readonly showMembershipHistory = signal(false);

  readonly listLink = ['/admin/lead-attribution/customers'];



  readonly membershipHistory = computed(() =>

    (this.customer()?.paymentHistory ?? []).filter((p) => p.orderType === 'Membership'),

  );



  constructor() {

    void this.load();

  }



  async load() {

    const userId = this.route.snapshot.paramMap.get('userId');

    if (!userId) {

      void this.router.navigate(this.listLink);

      return;

    }



    try {

      this.loading.set(true);

      const res = await firstValueFrom(this.api.getCustomer(userId).pipe(timeout(20000)));

      if (res?.success === false || !res.customer) {

        this.toast.error(res?.message || 'Customer not found.');

        void this.router.navigate(this.listLink);

        return;

      }

      this.customer.set(res.customer);

    } catch (e: unknown) {

      this.toast.error(getHttpErrorMessage(e, 'Unable to load customer.'));

      void this.router.navigate(this.listLink);

    } finally {

      this.loading.set(false);

    }

  }



  openMembershipHistory() {

    if ((this.customer()?.membershipSalesCount ?? 0) > 0) {

      this.showMembershipHistory.set(true);

    }

  }



  closeMembershipHistory() {

    this.showMembershipHistory.set(false);

  }



  formatAmount(paise: number): string {

    return new Intl.NumberFormat('en-IN', {

      style: 'currency',

      currency: 'INR',

      maximumFractionDigits: 2,

    }).format(paise / 100);

  }



  trackPayment(_index: number, item: LeadPaymentHistoryItem): string {

    return item.paymentOrderId;

  }

}


