import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

import { UserStoreService } from '../../../core/services/state/user-store.service';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.css'],
})
export class UserListComponent {
  readonly store = inject(UserStoreService);

  toggleActive(id: number): void {
    this.store.toggleUserActive(id);
  }

  promote(id: number): void {
    this.store.promoteToAdmin(id);
  }
}
