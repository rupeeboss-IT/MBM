import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-terms',
  imports: [RouterLink],
  templateUrl: './terms.html',
  styleUrls: ['../legal/legal-shared.css', './terms.css'],
})
export class Terms {}
