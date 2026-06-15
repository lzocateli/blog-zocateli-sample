export interface User {
  id: number;
  name: string;
  email: string;
  role: 'admin' | 'analyst' | 'viewer';
  team: string;
  active: boolean;
}

export interface UserFilters {
  term: string;
  onlyActive: boolean;
  role: 'all' | User['role'];
}
