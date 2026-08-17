import { Routes } from '@angular/router';
import { PlanosLista } from './planos/planos-lista';
import { BeneficiariosListaComponent } from './beneficiarios/beneficiarios-lista';
import { BeneficiarioFormComponent } from './beneficiarios/beneficiario-form';

export const routes: Routes = [
  { path: 'planos', component: PlanosLista },
  { path: 'beneficiarios', component: BeneficiariosListaComponent },
  { path: 'beneficiarios/novo', component: BeneficiarioFormComponent },
  { path: 'beneficiarios/editar/:id', component: BeneficiarioFormComponent },
  { path: '', redirectTo: '/planos', pathMatch: 'full' }
];
