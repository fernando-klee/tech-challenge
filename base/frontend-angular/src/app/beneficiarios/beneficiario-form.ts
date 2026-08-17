import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, switchMap, takeUntil } from 'rxjs';
import { mensagemDeErro } from '../nucleo/api';
import { BeneficiarioServico } from './beneficiario-servico';
import { PlanoServico } from '../planos/plano-servico';
import { Plano } from '../planos/plano';

@Component({
  selector: 'app-beneficiario-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './beneficiario-form.html',
  styleUrl: './beneficiario-form.css'
})
export class BeneficiarioFormComponent implements OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly beneficiarioServico = inject(BeneficiarioServico);
  private readonly planoServico = inject(PlanoServico);

  protected readonly planos = signal<Plano[]>([]);
  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly id = signal('');

  protected form!: FormGroup;

  // Subject para controlar o salvamento
  private readonly salvar$ = new Subject<void>();
  private readonly destroy$ = new Subject<void>();

  constructor() {
    this.form = this.fb.group({
      nome_completo: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
      cpf: [{ value: '', disabled: this.isEdit() }, [Validators.required, Validators.pattern(/^[0-9]{11}$/)]],
      data_nascimento: ['', [Validators.required, this.dataPassadaValidator]],
      plano_id: ['', Validators.required],
      status: ['ATIVO']
    });

    this.carregarPlanos();

    this.salvar$
      .pipe(
        takeUntil(this.destroy$),
        switchMap(() => {
          if (this.form.invalid) {
            Object.keys(this.form.controls).forEach(key => {
              this.form.get(key)?.markAsTouched();
            });
            this.carregando.set(false);
            return [];
          }
          this.carregando.set(true);
          this.erro.set(null);
          const dados = this.form.value;
          if (this.isEdit()) {
            delete dados.cpf;
            return this.beneficiarioServico.atualizar(this.id(), dados);
          } else {
            return this.beneficiarioServico.criar(dados);
          }
        })
      )
      .subscribe({
        next: () => {
          this.carregando.set(false);
          this.router.navigate(['/beneficiarios']);
        },
        error: (resposta: HttpErrorResponse) => {
          this.carregando.set(false);
          let msg = mensagemDeErro(resposta);
          if (resposta.status === 409) {
            msg = 'CPF já cadastrado.';
          } else if (resposta.status === 422) {
            msg = 'Plano inválido ou inexistente.';
          } else if (resposta.status === 400 && resposta.error?.detalhes) {
            const detalhes = resposta.error.detalhes
              .map((d: any) => `${d.campo}: ${d.regra}`)
              .join(', ');
            msg = `Dados inválidos: ${detalhes}`;
          }
          this.erro.set(msg);
        }
      });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id.set(idParam);
      this.isEdit.set(true);
      this.carregarBeneficiario(idParam);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private carregarPlanos(): void {
    this.planoServico
      .listar()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (planos) => this.planos.set(planos),
        error: (resposta: HttpErrorResponse) => this.erro.set(mensagemDeErro(resposta))
      });
  }

  private carregarBeneficiario(id: string): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.beneficiarioServico
      .obterPorId(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (beneficiario) => {
          this.form.patchValue({
            nome_completo: beneficiario.nome_completo,
            cpf: beneficiario.cpf,
            data_nascimento: beneficiario.data_nascimento,
            plano_id: beneficiario.plano_id,
            status: beneficiario.status
          });
          this.form.get('cpf')?.disable();
          this.carregando.set(false);
        },
        error: (resposta: HttpErrorResponse) => {
          this.erro.set(`Erro ao carregar dados: ${mensagemDeErro(resposta)}`);
          this.carregando.set(false);
        }
      });
  }

  private dataPassadaValidator(control: any) {
    const value = control.value;
    if (!value) return null;
    const data = new Date(value);
    const hoje = new Date();
    hoje.setHours(0, 0, 0, 0);
    if (data >= hoje) {
      return { dataFutura: true };
    }
    return null;
  }

  protected salvar(): void {
    this.salvar$.next();
  }

  protected cancelar(): void {
    this.router.navigate(['/beneficiarios']);
  }
}
