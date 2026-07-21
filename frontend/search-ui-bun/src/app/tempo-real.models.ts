export type TempoRealTransporte = 'signalr' | 'websocket' | 'sse' | 'long-polling';

export interface TempoRealCriacaoResponse {
  tarefaId: string;
  descricao: string;
  status: string;
  versao: number;
}

export interface TempoRealEstadoResponse {
  tarefaId: string;
  descricao: string;
  status: string;
  etapa: string;
  progresso: number;
  versao: number;
  atualizadoEmUtc: string;
  mensagem: string;
}