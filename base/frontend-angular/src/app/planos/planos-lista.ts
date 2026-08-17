import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, switchMap } from 'rxjs';

import { mensagemDeErro } from '../nucleo/api';
import { Plano } from './plano';
import { PlanoServico } from './plano-servico';

@Component({
  selector: 'app-planos-lista',
  templateUrl: './planos-lista.html',
  styleUrl: './planos-lista.css'
})
export class PlanosLista {
  private readonly servico = inject(PlanoServico);

  protected readonly planos = signal<Plano[]>([]);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);

  // Subject para controlar recarregamentos
  private readonly recarregar$ = new Subject<void>();

  constructor() {
    // A inscrição é feita no construtor (contexto de injeção)
    this.recarregar$
      .pipe(
        takeUntilDestroyed(),
        switchMap(() => {
          this.carregando.set(true);
          this.erro.set(null);
          return this.servico.listar();
        })
      )
      .subscribe({
        next: (planos) => {
          this.planos.set(planos);
          this.carregando.set(false);
        },
        error: (resposta: HttpErrorResponse) => {
          this.erro.set(mensagemDeErro(resposta));
          this.carregando.set(false);
        }
      });

    // Primeiro carregamento
    this.recarregar$.next();
  }

  // Método público chamado pelo botão "Recarregar"
  protected carregar(): void {
    this.recarregar$.next();
  }
}
