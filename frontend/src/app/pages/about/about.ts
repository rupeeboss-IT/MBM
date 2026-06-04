import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JoinTodayButton } from '../../core/components/join-today-button/join-today-button';

@Component({
  selector: 'app-about',
  imports: [RouterLink, JoinTodayButton],
  templateUrl: './about.html',
  styleUrl: './about.css',
})
export class About {}
