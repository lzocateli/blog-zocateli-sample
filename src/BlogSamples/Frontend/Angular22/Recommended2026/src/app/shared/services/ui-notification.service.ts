import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UiNotificationService {
  private readonly messageSignal = signal<string | null>(null);
  private readonly levelSignal = signal<'info' | 'success' | 'warning'>('info');

  readonly message = this.messageSignal.asReadonly();
  readonly level = this.levelSignal.asReadonly();
  readonly hasMessage = computed(() => this.messageSignal() !== null);

  showInfo(message: string): void {
    this.levelSignal.set('info');
    this.messageSignal.set(message);
  }

  showSuccess(message: string): void {
    this.levelSignal.set('success');
    this.messageSignal.set(message);
  }

  showWarning(message: string): void {
    this.levelSignal.set('warning');
    this.messageSignal.set(message);
  }

  clear(): void {
    this.messageSignal.set(null);
  }
}
