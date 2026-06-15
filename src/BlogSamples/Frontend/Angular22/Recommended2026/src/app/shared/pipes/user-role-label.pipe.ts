import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'userRoleLabel',
  standalone: true,
})
export class UserRoleLabelPipe implements PipeTransform {
  transform(value: 'admin' | 'analyst' | 'viewer'): string {
    switch (value) {
      case 'admin':
        return 'Administrator';
      case 'analyst':
        return 'Analyst';
      case 'viewer':
        return 'Viewer';
      default:
        return value;
    }
  }
}
