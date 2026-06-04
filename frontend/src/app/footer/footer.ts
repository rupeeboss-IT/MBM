import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MBM_LOGO_ALT, MBM_LOGO_SRC } from '../core/brand';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {
  readonly logoSrc = MBM_LOGO_SRC;
  readonly logoAlt = MBM_LOGO_ALT;
}
