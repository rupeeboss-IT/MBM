import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { JoinTodayButton } from '../../core/components/join-today-button/join-today-button';
import {
  TeamMemberService,
  type TeamMemberItem,
} from '../../core/services/team-member.service';

@Component({
  selector: 'app-about',
  imports: [CommonModule, RouterLink, JoinTodayButton],
  templateUrl: './about.html',
  styleUrl: './about.css',
})
export class About implements OnInit {
  private readonly teamApi = inject(TeamMemberService);

  readonly teamLoading = signal(true);
  readonly teamMembers = signal<TeamMemberItem[]>([]);

  async ngOnInit() {
    await this.loadTeam();
  }

  private async loadTeam() {
    try {
      this.teamLoading.set(true);
      const res = await firstValueFrom(this.teamApi.listPublic());
      if (res.success) {
        this.teamMembers.set(res.members ?? []);
      }
    } catch {
      this.teamMembers.set([]);
    } finally {
      this.teamLoading.set(false);
    }
  }
}
