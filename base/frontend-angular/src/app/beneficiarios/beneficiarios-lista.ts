import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, switchMap, debounceTime } from 'rxjs';
import { mensagemDeErro } from '../nucleo/api';
import { BeneficiarioServico } from './beneficiario-servico';
import { PlanoServico } from '../planos/plano-servico';
import { Beneficiario, BeneficiarioListResponse } from './beneficiario';
import { Plano } from '../planos/plano';

@Component({
  selector: 'app-beneficiarios-lista',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './beneficiarios-lista.html',
  styleUrl: './beneficiarios-lista.css'
})
export class BeneficiariosListaComponent {
  private readonly beneficiarioServico = inject(BeneficiarioServico);
  private readonly planoServico = inject(PlanoServico);

  protected readonly beneficiarios = signal<Beneficiario[]>([]);
  protected readonly planos = signal<Plano[]>([]);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);

  protected readonly pagina = signal(1);
  protected readonly tamanho = signal(10);
  protected readonly total = signal(0);
  protected readonly statusFiltro = signal('');
  protected readonly planoFiltro = signal('');
  protected readonly Math = Math;

  private readonly recarregar$ = new Subject<void>();

  constructor() {

    this.carregarPlanos();

    this.recarregar$
      .pipe(
        takeUntilDestroyed(),
        switchMap(() => {
          this.carregando.set(true);
          this.erro.set(null);
          return this.beneficiarioServico.listar(
            this.pagina(),
            this.tamanho(),
            this.statusFiltro() || undefined,
            this.planoFiltro() || undefined
          );
        })
      )
      .subscribe({
        next: (resposta: BeneficiarioListResponse) => {
          this.beneficiarios.set(resposta.dados);
          this.total.set(resposta.total);
          this.carregando.set(false);
        },
        error: (resposta: HttpErrorResponse) => {
          this.erro.set(mensagemDeErro(resposta));
          this.carregando.set(false);
        }
      });

    this.recarregar$.next();
  }

  private carregarPlanos(): void {
    this.planoServico
      .listar()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (planos) => this.planos.set(planos),
        error: (resposta: HttpErrorResponse) => this.erro.set(mensagemDeErro(resposta))
      });
  }

  protected carregarBeneficiarios(): void {
    this.recarregar$.next();
  }

  protected aplicarFiltros(): void {
    this.pagina.set(1);
    this.carregarBeneficiarios();
  }

  protected mudarPagina(novaPagina: number): void {
    if (novaPagina < 1) return;
    this.pagina.set(novaPagina);
    this.carregarBeneficiarios();
  }

  protected excluir(id: string): void {
    if (!confirm('Tem certeza que deseja excluir este beneficiário?')) return;

    this.beneficiarioServico
      .excluir(id)
      .subscribe({
        next: () => this.carregarBeneficiarios(),
        error: (resposta: HttpErrorResponse) => {
          const msg = mensagemDeErro(resposta);
          this.erro.set(`Erro ao excluir: ${msg}`);
        }
      });
  }
}
