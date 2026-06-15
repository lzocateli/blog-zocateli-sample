import { Injectable, computed, signal } from '@angular/core';
import { take } from 'rxjs/operators';

import { User, UserFilters } from '../../../models/user.model';
import { UserApiService } from '../api/user-api.service';

@Injectable({ providedIn: 'root' })
export class UserStoreService {
  private readonly usersSignal = signal<User[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);
  private readonly selectedUserIdSignal = signal<number | null>(null);
  private readonly filtersSignal = signal<UserFilters>({
    term: '',
    onlyActive: false,
    role: 'all',
  });

  readonly users = this.usersSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly filters = this.filtersSignal.asReadonly();

  readonly selectedUser = computed(() => {
    const id = this.selectedUserIdSignal();
    if (id == null) return null;
    return this.usersSignal().find((u) => u.id === id) ?? null;
  });

  readonly filteredUsers = computed(() => {
    const { term, onlyActive, role } = this.filtersSignal();
    const normalized = term.trim().toLowerCase();

    return this.usersSignal().filter((u) => {
      const matchTerm =
        normalized.length === 0 ||
        u.name.toLowerCase().includes(normalized) ||
        u.email.toLowerCase().includes(normalized) ||
        u.team.toLowerCase().includes(normalized);
      const matchActive = !onlyActive || u.active;
      const matchRole = role === 'all' || u.role === role;
      return matchTerm && matchActive && matchRole;
    });
  });

  readonly stats = computed(() => {
    const users = this.usersSignal();
    const total = users.length;
    const active = users.filter((u) => u.active).length;
    const admins = users.filter((u) => u.role === 'admin').length;
    return { total, active, admins };
  });

  constructor(private readonly api: UserApiService) {}

  loadUsers(simulateError = false): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    this.api
      .fetchUsers(simulateError)
      .pipe(take(1))
      .subscribe({
        next: (data) => {
          this.usersSignal.set(data);
          this.loadingSignal.set(false);
          if (this.selectedUserIdSignal() == null && data.length > 0) {
            this.selectedUserIdSignal.set(data[0].id);
          }
        },
        error: (err: Error) => {
          this.errorSignal.set(err.message);
          this.loadingSignal.set(false);
        },
      });
  }

  selectUser(id: number): void {
    this.selectedUserIdSignal.set(id);
  }

  updateFilters(partial: Partial<UserFilters>): void {
    this.filtersSignal.update((current) => ({ ...current, ...partial }));
  }

  toggleUserActive(id: number): void {
    this.usersSignal.update((list) =>
      list.map((u) => (u.id === id ? { ...u, active: !u.active } : u))
    );
  }

  promoteToAdmin(id: number): void {
    this.usersSignal.update((list) =>
      list.map((u) => (u.id === id ? { ...u, role: 'admin' } : u))
    );
  }
}
