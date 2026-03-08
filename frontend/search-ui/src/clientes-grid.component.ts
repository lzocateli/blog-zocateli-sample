// clientes-grid.component.ts
import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subject } from 'rxjs';
import {
    debounceTime,
    distinctUntilChanged,
    filter,
    switchMap,
    takeUntil
} from 'rxjs/operators';
import { ClienteService } from './cliente.service';
import { PagedResult, ClienteDto } from './models';

@Component({
    selector: 'app-clientes-grid',
    templateUrl: './clientes-grid.component.html'
})
export class ClientesGridComponent implements OnInit, OnDestroy {
    buscaControl = new FormControl('');
    clientes: ClienteDto[] = [];
    paginaAtual = 1;
    totalPaginas = 1;
    totalRegistros = 0;
    carregando = false;
    termoBusca = '';

    private destroy$ = new Subject<void>();

    constructor(private clienteService: ClienteService) { }

    ngOnInit(): void {
        this.buscaControl.valueChanges.pipe(
            debounceTime(400),                    // aguarda 400ms após a última tecla
            distinctUntilChanged(),               // ignora se o valor não mudou
            filter(termo => !termo || termo.length === 0 || termo.length >= 3),
            takeUntil(this.destroy$)
        ).subscribe(termo => {
            this.termoBusca = termo ?? '';
            this.paginaAtual = 1;                 // volta para a primeira página a cada nova busca
            this.buscar();
        });

        // Carrega a primeira página sem filtro ao abrir a tela
        this.buscar();
    }

    buscar(): void {
        this.carregando = true;
        this.clienteService
            .listar(this.termoBusca, this.paginaAtual, 20)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (resultado) => {
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
