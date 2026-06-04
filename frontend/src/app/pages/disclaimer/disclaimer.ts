import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-disclaimer',
  imports: [RouterLink],
  templateUrl: './disclaimer.html',
  styleUrls: ['../legal/legal-shared.css', './disclaimer.css'],
})
export class Disclaimer {}
