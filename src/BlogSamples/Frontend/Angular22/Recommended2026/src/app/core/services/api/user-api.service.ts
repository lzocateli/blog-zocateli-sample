import { Injectable } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';

import { User } from '../../../models/user.model';

const MOCK_USERS: User[] = [
  { id: 1, name: 'Alice Johnson', email: 'alice@corp.com', role: 'admin', team: 'platform', active: true },
  { id: 2, name: 'Bob Smith', email: 'bob@corp.com', role: 'analyst', team: 'finance', active: true },
  { id: 3, name: 'Carla Green', email: 'carla@corp.com', role: 'viewer', team: 'sales', active: false },
  { id: 4, name: 'Diego Costa', email: 'diego@corp.com', role: 'analyst', team: 'growth', active: true },
  { id: 5, name: 'Erika Stone', email: 'erika@corp.com', role: 'viewer', team: 'support', active: true },
];

@Injectable({ providedIn: 'root' })
export class UserApiService {
  fetchUsers(simulateError = false): Observable<User[]> {
    if (simulateError) {
      return throwError(() => new Error('Failed to load users from API')).pipe(delay(500));
    }
    return of(MOCK_USERS).pipe(delay(450));
  }
}
