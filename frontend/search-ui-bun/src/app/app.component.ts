import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { TempoRealEstadoResponse, TempoRealTransporte } from './tempo-real.models';
import { TempoRealService } from './tempo-real.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  providers: [TempoRealService],
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
        color: #f8fafc;
        background:
          radial-gradient(circle at top left, rgba(56, 189, 248, 0.32), transparent 30%),
          radial-gradient(circle at top right, rgba(244, 114, 182, 0.18), transparent 26%),
          linear-gradient(180deg, #07111f 0%, #0f172a 45%, #111827 100%);
        font-family: 'Inter', 'Segoe UI', sans-serif;
      }

      .container {
        max-width: 1180px;
        margin: 0 auto;
        padding: 48px 20px 64px;
      }

      .hero {
        display: grid;
        gap: 12px;
        margin-bottom: 28px;
      }

      .badge {
        display: inline-flex;
        width: fit-content;
        padding: 6px 12px;
        border-radius: 999px;
        background: rgba(15, 23, 42, 0.65);
        border: 1px solid rgba(148, 163, 184, 0.24);
        color: #cbd5e1;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        font-size: 0.72rem;
      }

      h1 {
        margin: 0;
        font-size: clamp(2rem, 5vw, 4rem);
        line-height: 0.95;
        max-width: 12ch;
      }

      .lead {
        margin: 0;
        max-width: 72ch;
        color: #cbd5e1;
        font-size: 1.05rem;
        line-height: 1.7;
      }

      .grid {
        display: grid;
        grid-template-columns: minmax(0, 1.15fr) minmax(320px, 0.85fr);
        gap: 24px;
      }

      .panel {
        background: rgba(15, 23, 42, 0.72);
        border: 1px solid rgba(148, 163, 184, 0.18);
        border-radius: 24px;
        box-shadow: 0 24px 80px rgba(15, 23, 42, 0.28);
        backdrop-filter: blur(16px);
      }

      .form-panel,
      .stream-panel {
        padding: 24px;
      }

      .form-grid {
        display: grid;
        gap: 16px;
      }

      label {
        display: grid;
        gap: 8px;
        color: #e2e8f0;
        font-weight: 600;
      }

      input {
        width: 100%;
        box-sizing: border-box;
        border-radius: 14px;
        border: 1px solid rgba(148, 163, 184, 0.22);
        background: rgba(2, 6, 23, 0.78);
        color: #f8fafc;
        padding: 14px 16px;
        font: inherit;
      }

      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 12px;
      }

      button {
        border: 0;
        border-radius: 14px;
        padding: 14px 18px;
        font: inherit;
        font-weight: 700;
        cursor: pointer;
      }

      .primary {
        background: linear-gradient(135deg, #38bdf8, #818cf8);
        color: #020617;
      }

      .secondary {
        background: rgba(30, 41, 59, 0.9);
        color: #e2e8f0;
        border: 1px solid rgba(148, 163, 184, 0.18);
      }

      .chips {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
      }

      .chip {
        padding: 8px 12px;
        border-radius: 999px;
        background: rgba(30, 41, 59, 0.95);
        border: 1px solid rgba(148, 163, 184, 0.22);
        cursor: pointer;
        color: inherit;
      }

      .chip.active {
        background: rgba(56, 189, 248, 0.18);
        border-color: rgba(56, 189, 248, 0.58);
      }

      .stream-panel {
        display: grid;
        gap: 16px;
      }

      .status {
        display: grid;
        gap: 4px;
        padding: 16px;
        border-radius: 18px;
        background: rgba(2, 6, 23, 0.65);
        border: 1px solid rgba(148, 163, 184, 0.18);
      }

      .status strong {
        font-size: 1.05rem;
      }

      .history {
        display: grid;
        gap: 10px;
      }

      .event {
        padding: 14px 16px;
        border-radius: 16px;
        background: rgba(30, 41, 59, 0.84);
        border: 1px solid rgba(148, 163, 184, 0.16);
      }

      .meta {
        color: #94a3b8;
        font-size: 0.85rem;
      }

      .empty {
        color: #94a3b8;
        border: 1px dashed rgba(148, 163, 184, 0.24);
        border-radius: 16px;
        padding: 20px;
      }

      @media (max-width: 920px) {
        .grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ],
  template: `
    <main class="container">
      <section class="hero">
        <span class="badge">Angular + .NET • tempo real</span>
        <h1>Tempo real sem mistério</h1>
        <p class="lead">
          Esta tela compara WebSockets, SSE, SignalR e Long Polling usando o mesmo fluxo de tarefas do backend em ASP.NET Core.
          O objetivo é mostrar o comportamento prático de cada transporte, sem esconder o custo real de adoção.
        </p>
      </section>

      <section class="grid">
        <article class="panel form-panel">
          <div class="form-grid">
            <label>
              Descrição da tarefa
              <input [formControl]="descricaoControl" placeholder="Ex.: processar relatório, gerar feed, simular falha" />
            </label>

            <label>
              Transporte
              <div class="chips">
                <button
                  type="button"
                  class="chip"
                  *ngFor="let opcao of transportes"
                  [class.active]="transporteSelecionado === opcao.valor"
                  (click)="selecionarTransporte(opcao.valor)">
                  {{ opcao.rotulo }}
                </button>
              </div>
            </label>

            <div class="actions">
              <button class="primary" type="button" (click)="iniciar()" [disabled]="carregando || descricaoControl.invalid">
                {{ carregando ? 'Iniciando...' : 'Iniciar demo' }}
              </button>
              <button class="secondary" type="button" (click)="parar()">Parar fluxo</button>
            </div>
          </div>
        </article>

        <article class="panel stream-panel">
          <div class="status">
            <strong>{{ statusAtual?.etapa ?? 'Nenhuma tarefa em execução' }}</strong>
            <span class="meta">
              {{ statusAtual?.status ?? 'Aguardando entrada' }}
              <ng-container *ngIf="statusAtual">• {{ statusAtual?.progresso }}% • v{{ statusAtual?.versao }}</ng-container>
            </span>
            <span class="meta" *ngIf="tarefaAtualId">Tarefa {{ tarefaAtualId }}</span>
          </div>

          <div class="history" *ngIf="historico.length; else vazio">
            <div class="event" *ngFor="let evento of historico">
              <strong>{{ evento.etapa }}</strong>
              <div class="meta">{{ evento.status }} • {{ evento.progresso }}% • {{ evento.atualizadoEmUtc | date: 'HH:mm:ss' }}</div>
              <p>{{ evento.mensagem }}</p>
            </div>
          </div>

          <ng-template #vazio>
            <div class="empty">
              Execute a demo para ver o fluxo de estado, inclusive a diferença entre polling, SSE, WebSocket e SignalR.
            </div>
          </ng-template>
        </article>
      </section>
    </main>
  `
})
export class AppComponent {
  private readonly tempoRealService = inject(TempoRealService);
  private readonly destroyRef = inject(DestroyRef);

  descricaoControl = new FormControl('simular processamento em tempo real', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(3)]
  });

  transportes: Array<{ valor: TempoRealTransporte; rotulo: string }> = [
    { valor: 'signalr', rotulo: 'SignalR' },
    { valor: 'websocket', rotulo: 'WebSocket' },
    { valor: 'sse', rotulo: 'SSE' },
    { valor: 'long-polling', rotulo: 'Long Polling' }
  ];

  transporteSelecionado: TempoRealTransporte = 'signalr';
  carregando = false;
  tarefaAtualId = '';
  statusAtual: TempoRealEstadoResponse | null = null;
  historico: TempoRealEstadoResponse[] = [];

  private cancelarFluxo: (() => void | Promise<void>) | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.parar());
  }

  selecionarTransporte(transporte: TempoRealTransporte): void {
    this.transporteSelecionado = transporte;
  }

  async iniciar(): Promise<void> {
    if (this.descricaoControl.invalid) {
      return;
    }

    this.parar();
    this.carregando = true;
    this.historico = [];
    this.statusAtual = null;

    try {
      const tarefa = await this.tempoRealService.criarTarefa(this.descricaoControl.value.trim());
      this.tarefaAtualId = tarefa.tarefaId;
      this.statusAtual = {
        tarefaId: tarefa.tarefaId,
        descricao: tarefa.descricao,
        status: tarefa.status,
        etapa: 'Tarefa criada',
        progresso: 0,
        versao: tarefa.versao,
        atualizadoEmUtc: new Date().toISOString(),
        mensagem: 'Fluxo iniciado no backend'
      };

      const onEstado = (estado: TempoRealEstadoResponse) => {
        this.statusAtual = estado;
        this.historico = [...this.historico.filter(item => item.versao !== estado.versao), estado].sort((a, b) => a.versao - b.versao);
      };

      switch (this.transporteSelecionado) {
        case 'signalr':
          this.cancelarFluxo = await this.tempoRealService.conectarSignalR(tarefa.tarefaId, onEstado);
          break;
        case 'websocket':
          this.cancelarFluxo = this.tempoRealService.conectarWebSocket(tarefa.tarefaId, onEstado);
          break;
        case 'sse':
          this.cancelarFluxo = this.tempoRealService.conectarSse(tarefa.tarefaId, onEstado);
          break;
        case 'long-polling':
          this.cancelarFluxo = this.tempoRealService.conectarLongPolling(tarefa.tarefaId, tarefa.versao, onEstado);
          break;
      }
    } finally {
      this.carregando = false;
    }
  }

  parar(): void {
    if (this.cancelarFluxo) {
      const resultado = this.cancelarFluxo();
      if (resultado instanceof Promise) {
        void resultado;
      }
      this.cancelarFluxo = null;
    }
  }
}
