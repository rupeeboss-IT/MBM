import { CommonModule } from '@angular/common';
import { Component, computed, HostListener, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UdyamRegistrationInputComponent } from '../udyam-registration-input/udyam-registration-input';
import { LocalDatePipe } from '../../pipes/local-date.pipe';
import { SchemeDiscoveryFlowService } from '../../services/scheme-discovery-flow.service';

@Component({
  selector: 'app-scheme-discovery-modals',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalDatePipe, UdyamRegistrationInputComponent],
  templateUrl: './scheme-discovery-modals.html',
  styleUrl: './scheme-discovery-modals.css',
})
export class SchemeDiscoveryModals {
  readonly flow = inject(SchemeDiscoveryFlowService);

  readonly title = computed(() => {
    switch (this.flow.modal()) {
      case 'auth':
        return 'Get Your Report';
      case 'no_membership':
      case 'plan_choice':
        return 'Choose How to Proceed';
      case 'report_exists':
        return 'Report Already Available';
      case 'udyam':
        return this.flow.udyamCheckoutMode() ? 'Enter Udyam Number' : 'Request Your Report';
      case 'pending':
        return 'Generating Report';
      case 'success':
        return 'Report Generated Successfully';
      case 'report_generated':
        return 'Report Generated Successfully';
      case 'report_duplicate':
        return 'Report Already Available';
      case 'report_failed':
        return 'Report Generation Failed';
      case 'generating_report':
        return 'Generating Report';
      case 'referral':
        return 'Referral Code';
      default:
        return 'Government Scheme Discovery';
    }
  });

  readonly subtitle = computed(() => {
    switch (this.flow.modal()) {
      case 'auth':
        return 'Are you an existing user or a new user?';
      case 'no_membership':
        return 'Pay ₹1,180 for a one-time report — no annual membership required — or upgrade to Premium/Pro for included reports.';
      case 'referral':
        return 'Government Scheme Discovery Report — ₹1,180 incl. GST';
      case 'plan_choice':
        return 'Upgrade for included reports, or purchase a one-time report for your current membership period.';
      case 'report_exists':
        return 'Your scheme discovery report for this membership period is ready to view or download.';
      case 'udyam':
        return this.flow.udyamCheckoutMode()
          ? 'Enter your Udyam number first. It will be saved before referral and payment.'
          : 'Confirm your details and enter your Udyam Registration Number.';
      case 'pending':
        return 'Please wait while we analyze your business information and prepare recommendations.';
      case 'success':
        return 'Your report is ready to view or download.';
      case 'report_generated':
        return 'Your personalized Government Scheme Discovery Report is ready to view or download.';
      case 'report_duplicate':
        return 'Your report remains valid for one year from the date of generation.';
      case 'report_failed':
        return 'We were unable to generate the report right now. Please try again after some time.';
      case 'generating_report':
        return 'Please wait while we analyze your business information and prepare recommendations.';
      default:
        return null;
    }
  });

  readonly icon = computed(() => {
    switch (this.flow.modal()) {
      case 'auth':
        return '🏛️';
      case 'no_membership':
        return '🪪';
      case 'plan_choice':
        return '✨';
      case 'report_exists':
        return '📄';
      case 'udyam':
        return '📋';
      case 'pending':
        return '⏳';
      case 'success':
      case 'report_generated':
        return '✅';
      case 'report_duplicate':
        return '📄';
      case 'report_failed':
        return '⚠️';
      case 'referral':
        return '🎟️';
      case 'loading':
      case 'generating_report':
        return '🔍';
      default:
        return '🏛️';
    }
  });

  readonly headerTone = computed(() => {
    switch (this.flow.modal()) {
      case 'success':
      case 'report_generated':
        return 'success';
      case 'report_exists':
      case 'report_duplicate':
        return 'info';
      case 'report_failed':
        return 'warn';
      case 'pending':
        return 'pending';
      case 'no_membership':
        return 'warn';
      default:
        return 'default';
    }
  });

  readonly showFooter = computed(() => this.flow.modal() === 'udyam');

  readonly udyamSubmitLabel = computed(() => {
    if (this.flow.submitting()) {
      return this.flow.udyamCheckoutMode() ? 'Saving…' : 'Generating Report…';
    }
    return this.flow.udyamCheckoutMode() ? 'Continue to Payment' : 'Generate Report';
  });

  @HostListener('document:keydown.escape')
  onEscape(): void {
    const modal = this.flow.modal();
    if (modal === 'none' || modal === 'loading' || modal === 'generating_report') return;
    if (modal === 'referral') {
      this.flow.cancelReferralStep();
      return;
    }
    this.flow.closeAll();
  }
}
