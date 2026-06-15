import { Component, OnInit, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterOutlet } from '@angular/router';

import { UserStoreService } from '../../core/services/state/user-store.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { CardHoverDirective } from '../../shared/directives/card-hover.directive';
import { UserRoleLabelPipe } from '../../shared/pipes/user-role-label.pipe';
import { UiNotificationService } from '../../shared/services/ui-notification.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    RouterOutlet,
    LoadingSpinnerComponent,
    CardHoverDirective,
    UserRoleLabelPipe,
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
})
export class DashboardComponent implements OnInit {
  readonly store = inject(UserStoreService);
  readonly notifications = inject(UiNotificationService);

  readonly roleOptions = ['all', 'admin', 'analyst', 'viewer'] as const;
  readonly hasUsers = computed(() => this.store.users().length > 0);

  ngOnInit(): void {
    this.store.loadUsers(false);
    this.notifications.showInfo('Dashboard carregado com arquitetura Recommended 2026.');
  }

  onSearch(term: string): void {
    this.store.updateFilters({ term });
  }

  onRoleChange(role: 'all' | 'admin' | 'analyst' | 'viewer'): void {
    this.store.updateFilters({ role });
  }

  onOnlyActive(onlyActive: boolean): void {
    this.store.updateFilters({ onlyActive });
  }

  retry(): void {
    this.store.loadUsers(false);
    this.notifications.showSuccess('Dados recarregados com sucesso.');
  }

  clearMessage(): void {
    this.notifications.clear();
  }
}
