import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { UserStoreService } from '../../../core/services/state/user-store.service';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './user-detail.component.html',
  styleUrls: ['./user-detail.component.css'],
})
export class UserDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly store = inject(UserStoreService);

  readonly user = computed(() => {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (Number.isNaN(id)) return null;
    return this.store.users().find((u) => u.id === id) ?? null;
  });
}
