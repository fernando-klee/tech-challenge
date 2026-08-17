export interface Beneficiario {
  id: string;
  nome_completo: string;
  cpf: string;
  data_nascimento: string;
  status: 'ATIVO' | 'INATIVO';
  plano_id: string;
  plano?: {
    id: string;
    nome: string;
    codigo_registro_ans: string;
  };
  data_cadastro: string; // ISO 8601
}

export interface BeneficiarioListResponse {
  dados: Beneficiario[];
  pagina: number;
  tamanho: number;
  total: number;
}
