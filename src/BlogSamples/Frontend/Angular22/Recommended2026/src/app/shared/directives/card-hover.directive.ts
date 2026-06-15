import { Directive, HostBinding, HostListener } from '@angular/core';

@Directive({
  selector: '[appCardHover]',
  standalone: true,
})
export class CardHoverDirective {
  @HostBinding('style.transform') transform = 'translateY(0)';
  @HostBinding('style.transition') transition = 'transform 180ms ease, box-shadow 180ms ease';
  @HostBinding('style.boxShadow') boxShadow = '0 8px 24px rgba(2, 132, 199, 0.08)';

  @HostListener('mouseenter')
  onMouseEnter(): void {
    this.transform = 'translateY(-2px)';
    this.boxShadow = '0 12px 28px rgba(2, 132, 199, 0.16)';
  }

  @HostListener('mouseleave')
  onMouseLeave(): void {
    this.transform = 'translateY(0)';
    this.boxShadow = '0 8px 24px rgba(2, 132, 199, 0.08)';
  }
}
