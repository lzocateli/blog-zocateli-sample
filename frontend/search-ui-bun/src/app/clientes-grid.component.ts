import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { Subject } from 'rxjs';
import {
  debounceTime,
  distinctUntilChanged,
  filter,
  takeUntil
} from 'rxjs/operators';
import { ClienteService } from './cliente.service';
import { ClienteDto, PagedResult } from './models';

@Component({
  selector: 'app-clientes-grid',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './clientes-grid.component.html'
})
export class ClientesGridComponent implements OnInit, OnDestroy {
  buscaControl = new FormControl<string>('', { nonNullable: true });
  clientes: ClienteDto[] = [];
  paginaAtual = 1;
  totalPaginas = 1;
  totalRegistros = 0;
  carregando = false;
  termoBusca = '';

  private destroy$ = new Subject<void>();

  constructor(private clienteService: ClienteService) {}

  ngOnInit(): void {
    this.buscaControl.valueChanges
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        filter((termo: string) => termo.length === 0 || termo.length >= 3),
        takeUntil(this.destroy$)
      )
      .subscribe((termo: string) => {
        this.termoBusca = termo;
        this.paginaAtual = 1;
        this.buscar();
      });

    this.buscar();
  }

  buscar(): void {
    this.carregando = true;
    this.clienteService
      .listar(this.termoBusca, this.paginaAtual, 20)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (resultado: PagedResult<ClienteDto>) => {
          this.clientes = resultado.itens;
          this.totalPaginas = resultado.totalPaginas;
          this.totalRegistros = resultado.totalRegistros;
          this.carregando = false;
        },
        error: () => {
          this.carregando = false;
        }
      });
  }

  irParaPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalPaginas) return;
    this.paginaAtual = pagina;
    this.buscar();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
