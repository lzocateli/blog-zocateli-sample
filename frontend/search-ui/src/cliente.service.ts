// cliente.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, ClienteDto } from './models';

@Injectable({ providedIn: 'root' })
export class ClienteService {
    private readonly apiUrl = '/api/clientes';

    constructor(private http: HttpClient) { }

    listar(
        q: string,
        pagina: number,
        tamanho: number
    ): Observable<PagedResult<ClienteDto>> {
        let params = new HttpParams()
            .set('pagina', pagina)
            .set('tamanho', tamanho);

        if (q?.trim()) {
            params = params.set('q', q.trim());
        }

        return this.http.get<PagedResult<ClienteDto>>(this.apiUrl, { params });
    }
}
