import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="spinner-wrap" [attr.aria-label]="label">
      <div class="spinner"></div>
      <span>{{ label }}</span>
    </div>
  `,
  styles: [
    `
      .spinner-wrap {
        display: inline-flex;
        align-items: center;
        gap: 10px;
        color: #334155;
        font-size: 14px;
      }
      .spinner {
        width: 18px;
        height: 18px;
        border: 2px solid #cbd5e1;
        border-top-color: #0ea5e9;
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
      }
      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }
    `,
  ],
})
export class LoadingSpinnerComponent {
  @Input() label = 'Loading data...';
}
