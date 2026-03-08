// models.ts
export interface ClienteDto {
    id: number;
    nome: string;
    cpfCnpj: string;
    email: string;
    cidade: string;
}

export interface PagedResult<T> {
    itens: T[];
    totalRegistros: number;
    totalPaginas: number;
    paginaAtual: number;
    tamanhoPagina: number;
}
