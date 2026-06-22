import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MBM_LOGO_ALT, MBM_LOGO_SRC, MBM_WHATSAPP_DISPLAY, mbmWhatsAppUrl } from '../core/brand';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {
  readonly logoSrc = MBM_LOGO_SRC;
  readonly logoAlt = MBM_LOGO_ALT;
  readonly whatsAppDisplay = MBM_WHATSAPP_DISPLAY;
  readonly whatsAppUrl = mbmWhatsAppUrl();
}
