import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE } from '../nucleo/api';
import { Beneficiario, BeneficiarioListResponse } from './beneficiario';

/**
 * Todo acesso à API passa por um serviço. Componente não chama HttpClient direto.
 */
@Injectable({ providedIn: 'root' })
export class BeneficiarioServico {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);

  /**
   * Lista beneficiários com paginação e filtros.
   * @param pagina Número da página (inicia em 1)
   * @param tamanho Itens por página (1 a 100)
   * @param status Filtro por situação (ATIVO ou INATIVO)
   * @param planoId Filtro por plano (UUID)
   */
  listar(
    pagina: number = 1,
    tamanho: number = 10,
    status?: string,
    planoId?: string
  ): Observable<BeneficiarioListResponse> {
    let params = new HttpParams()
      .set('pagina', pagina.toString())
      .set('tamanho', tamanho.toString());

    if (status) {
      params = params.set('status', status);
    }
    if (planoId) {
      params = params.set('plano_id', planoId);
    }

    return this.http.get<BeneficiarioListResponse>(`${this.base}/beneficiarios`, { params });
  }

  obterPorId(id: string): Observable<Beneficiario> {
    return this.http.get<Beneficiario>(`${this.base}/beneficiarios/${id}`);
  }

  criar(dados: any): Observable<Beneficiario> {
    return this.http.post<Beneficiario>(`${this.base}/beneficiarios`, dados);
  }

  atualizar(id: string, dados: any): Observable<Beneficiario> {
    return this.http.put<Beneficiario>(`${this.base}/beneficiarios/${id}`, dados);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/beneficiarios/${id}`);
  }
}
