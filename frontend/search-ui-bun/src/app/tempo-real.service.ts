import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { TempoRealCriacaoResponse, TempoRealEstadoResponse } from './tempo-real.models';

@Injectable()
export class TempoRealService {
  private readonly apiUrl = '/api/tempo-real';

  constructor(private readonly http: HttpClient) {}

  criarTarefa(descricao: string): Promise<TempoRealCriacaoResponse> {
    return firstValueFrom(
      this.http.post<TempoRealCriacaoResponse>(`${this.apiUrl}/tarefas`, { descricao })
    );
  }

  conectarSse(
    tarefaId: string,
    onEstado: (estado: TempoRealEstadoResponse) => void
  ): () => void {
    const source = new EventSource(`${this.apiUrl}/tarefas/${tarefaId}/stream`);

    source.addEventListener('estado', (evento: Event) => {
      const messageEvent = evento as MessageEvent;
      onEstado(JSON.parse(messageEvent.data) as TempoRealEstadoResponse);
    });

    source.onerror = () => source.close();

    return () => source.close();
  }

  conectarWebSocket(
    tarefaId: string,
    onEstado: (estado: TempoRealEstadoResponse) => void
  ): () => void {
    const baseUrl = window.location.origin.replace(/^http/, 'ws');
    const socket = new WebSocket(`${baseUrl}${this.apiUrl}/ws?tarefaId=${encodeURIComponent(tarefaId)}`);

    socket.onmessage = (evento) => {
      onEstado(JSON.parse(evento.data) as TempoRealEstadoResponse);
    };

    return () => socket.close();
  }

  async conectarSignalR(
    tarefaId: string,
    onEstado: (estado: TempoRealEstadoResponse) => void
  ): Promise<() => Promise<void>> {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/tempo-real', { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    connection.on('estado', (estado: TempoRealEstadoResponse) => onEstado(estado));
    await connection.start();
    await connection.invoke('AcompanharTarefa', tarefaId);

    return async () => {
      await connection.stop();
    };
  }

  conectarLongPolling(
    tarefaId: string,
    versaoInicial: number,
    onEstado: (estado: TempoRealEstadoResponse) => void
  ): () => void {
    let interromper = false;

    void this.executarLongPolling(tarefaId, versaoInicial, onEstado, () => interromper);

    return () => {
      interromper = true;
    };
  }

  private async executarLongPolling(
    tarefaId: string,
    versaoAtual: number,
    onEstado: (estado: TempoRealEstadoResponse) => void,
    deveParar: () => boolean
  ): Promise<void> {
    while (!deveParar()) {
      const params = new HttpParams()
        .set('versao', versaoAtual)
        .set('timeoutMs', '15000');

      const resposta = await firstValueFrom(
        this.http.get<TempoRealEstadoResponse>(`${this.apiUrl}/tarefas/${tarefaId}/long-poll`, {
          params,
          observe: 'response'
        })
      );

      if (resposta.status === 200 && resposta.body) {
        versaoAtual = resposta.body.versao;
        onEstado(resposta.body);

        if (resposta.body.status === 'Concluida' || resposta.body.status === 'Falhou') {
          return;
        }
      }
    }
  }
}