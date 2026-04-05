import { Component } from '@angular/core';
import { ClientesGridComponent } from './clientes-grid.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ClientesGridComponent],
  template: `
    <h1>Search UI — Deno</h1>
    <app-clientes-grid />
  `
})
export class AppComponent {
  title = 'search-ui-deno';
}
