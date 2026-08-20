# Entrega — fernando-klee

---

## 1. Resumo da entrega

A entrega consistiu em corrigir e evoluir o módulo de Beneficiários, tanto no backend quanto no frontend, seguindo a especificação e o padrão já estabelecido pelo módulo de Planos. Todas as funcionalidades exigidas foram implementadas e os testes da suíte pública estão verdes.

### Backend (API)
- **Domínio `Beneficiario`**: adicionado construtor privado, propriedades com `private set`, validação separada para criação e atualização, exclusão lógica com `ExcluidoEm`.
- **Controller e Serviço**: refatorados para usar DTOs e seguir o padrão do `PlanosServico`. Implementados todos os endpoints (GET com paginação/filtros, POST, PUT, DELETE).
- **Infraestrutura**: índice único para CPF, filtro de exclusão lógica, conversor `DateOnly`.
- **Testes**: corrigidos testes existentes (tamanho padrão 10, retorno 409 para inativo) e adicionados novos para validação de parâmetros, filtros individuais e reativação.

### Frontend (Angular)
- **Roteamento e navegação**: configurado `app.routes.ts`, `RouterOutlet` e menu entre Planos e Beneficiários.
- **Listagem**: componente com filtros combináveis, paginação, loading e tratamento de erro.
- **Formulário**: cadastro e edição com validações no cliente, CPF desabilitado na edição, status visível apenas na edição e tratamento de erros da API (409, 422, 400).
- **Estilização**: seguiu o padrão visual do módulo de Planos, com estilos adicionais para filtros e paginação.

### O que ficou de fora e por quê
- **Testes automatizados do frontend**: não foram implementados porque o desafio não os exige e a avaliação é feita na apresentação ao vivo.
- **Bibliotecas de componentes**: não utilizadas para manter a simplicidade, conforme recomendação do enunciado.
- **Funcionalidades extras**: nenhuma além da especificação, para manter o foco no escopo solicitado.



---

## 2. Decisões

### 2.1 Defeitos que encontrei no código base

| Defeito | Onde estava | Correção |
|---------|-------------|----------|
| **CPF era mutável no PUT** | O controller aceitava CPF na atualização, mas a especificação diz que ele deve ser ignorado. | Criei DTOs separados: `BeneficiarioRequest` (com CPF) para POST e `BeneficiarioUpdateRequest` (sem CPF) para PUT. |
| **Validação de CPF duplicado sem garantia real** | O controller fazia uma consulta prévia, mas não tinha índice único no banco. | Adicionei `HasIndex(b => b.Cpf).IsUnique()` no `AppDbContext` e capturo `DbUpdateException` para retornar 409. |
| **Exclusão lógica não implementada** | O DELETE removia o registro fisicamente. | Adicionei propriedade `ExcluidoEm` e método `Excluir()` no domínio, e filtro global `HasQueryFilter(b => b.ExcluidoEm == null)` no DbContext. |
| **Inativo permitia alteração de dados** | O método `AtualizarDados` não verificava o status. | Adicionei regra: se `Status == INATIVO` e houver alteração de nome/data/plano, lança `ConflitoException` (409). |
| **Filtro `plano_id` não funcionava** | O controller usava `[FromQuery] Guid? planoId`, mas a spec usa `plano_id` (snake_case). | Adicionei `[FromQuery(Name = "plano_id")]` no controller. |
| **`DateOnly` com erro de timezone** | O conversor gerava `DateTime` com `Kind.Unspecified`, causando erro no PostgreSQL. | Alterei o conversor para usar `DateTime.SpecifyKind(v.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)`. |
| **Teste `Listar_sem_informar_tamanho` esperava 20** | O teste estava desalinhado com a spec (que define padrão 10). | Ajustei o teste para esperar 10. |
| **Teste `Atualizar_dados_de_beneficiario_inativo` esperava 200** | A spec define que inativo não pode ter dados alterados (deve retornar 409). | Ajustei o teste para esperar 409 e validar a mensagem de erro. |

---


### 2.2 Pontos em que a especificação não definiu o comportamento

| Ponto omisso | Decisão | Justificativa |
|--------------|---------|---------------|
| **Ordenação padrão da listagem** | `NomeCompleto` (ascendente) + `Id` (ascendente) como critério de desempate. | Garante estabilidade na paginação, evitando que registros se repitam ou sejam pulados. |
| **Filtros automáticos vs. manuais** | Optei por filtros manuais com botão "Filtrar". | Evita múltiplas requisições desnecessárias, dá controle ao usuário e segue o padrão da maioria das aplicações. |
| **Tamanho padrão da página** | Mantive 10, conforme especificação. | O frontend tem seletor para o usuário alterar (5, 10, 20, 50). |
| **Exclusão de beneficiário inativo** | Permiti exclusão lógica independente do status. | A especificação não proíbe, e a exclusão lógica é um mecanismo de remoção, não de status. |

---


### 2.3 Inconsistências que percebi

| Inconsistência | O que fiz |
|----------------|-----------|
| **Teste esperava 20, spec define 10** | Ajustei o teste para 10 e registrei a decisão. |
| **Teste esperava 200 para alteração de inativo, spec define 409** | Ajustei o teste para 409 e registrei a decisão. |
| **Teste de reativação usava data fixa, causando falso positivo** | Ajustei o teste para usar a data exata do beneficiário criado (`DataNascimento.ToString("yyyy-MM-dd")`). |

---


### 2.4 Decisões técnicas

| Escolha | Onde | Motivo |
|---------|------|--------|
| **Uso de `Subject` + `switchMap` no frontend** | `beneficiarios-lista.ts` e `planos-lista.ts` | Evita o erro `NG0203` (uso de `takeUntilDestroyed` fora do contexto de injeção) e garante cancelamento de requisições anteriores. |
| **Signals em vez de observáveis diretos** | Componentes de listagem e formulário | Reatividade simples, previsível e alinhada com o Angular moderno. |
| **DTOs separados para POST e PUT** | `BeneficiarioRequest` e `BeneficiarioUpdateRequest` | Mantém a API consistente com a especificação e evita expor campos desnecessários. |
| **Não criar `NgModule` para Beneficiários** | Frontend Angular | Segui o padrão **standalone** já usado em Planos, mantendo consistência. |
| **Validação de CPF no cliente** | Formulário Angular | Melhora a experiência do usuário, evitando envios desnecessários para a API. |
| **Mensagens de erro específicas para 409, 422, 400** | Frontend | Torna os erros compreensíveis para o usuário final, conforme exigido na seção 9.5 da especificação. |
| **Exclusão lógica no backend com `ExcluidoEm`** | Domínio e DbContext | Preserva o histórico e mantém o CPF ocupado, conforme especificação. |

---

### Detalhamento sobre a separação de validações no domínio

No domínio `Beneficiario`, optei por separar as validações em dois métodos:

- **`ValidarDados`** (privado): valida apenas nome, data e planoId – campos que podem ser alterados.
- **`DefinirDados`**: valida CPF + chama `ValidarDados` para os demais.
- **`AtualizarDados`**: não recebe CPF e usa `ValidarDados` para validar os campos alteráveis.

**Motivo:** No `Plano`, todos os campos são alteráveis, então um único método (`DefinirDados`) serve para criação e atualização. No `Beneficiario`, o CPF é imutável, então precisamos de lógicas separadas para criação (que recebe CPF) e atualização (que não recebe). Separar as validações evitou duplicação de código e manteve cada método com uma responsabilidade clara.


### 2.5 O que ficou de fora

  Consegui fazer tudo que a documentação pedia no prazo.
---

## 3. Uso de IA

**Nível de uso:** (nenhum / pontual / moderado / intenso)
Utilizei IA em nivpel intenso, porém **não integrada ao fluxo do projeto (MCP)** 
### 3.1 Ferramentas

**DeepSeek** (modelo conversacional, via interface web). O uso foi predominantemente para:

- Gerar esqueletos de código (ex: serviços, controllers, componentes Angular).
- Sugerir correções para erros de compilação e lógica (ex: erro `NG0203` no frontend, problema de timezone no conversor `DateOnly`).
- Refatorar trechos para seguir o padrão do módulo de Planos.
- Escrever mensagens de commit e documentação.

### 3.2 Os 3 prompts que mais influenciaram o resultado


Os prompts mais relevantes foram aqueles que pediam:

---

**Prompt 1**

```
- **“Como corrigir o erro NG0203 com takeUntilDestroyed no Angular?”** – Levou à solução com `Subject` + `switchMap`.
```

- **O que aceitei:** a solução com `Subject` + `switchMap` no construtor, movendo o `takeUntilDestroyed` para o contexto de injeção e usando `switchMap` para cancelar requisições anteriores.
- **O que descartei e por quê:** a sugestão de usar `runInInjectionContext` ou `inject()` dentro do método – optei pela abordagem com `Subject` por ser mais limpa e reutilizável, além de evitar chamadas manuais de contexto de injeção.

**Prompt 2**

```
- **“Como lidar com a regra de inativo congelado no domínio?”** – Definiu a separação entre `AtualizarDados` e `ValidarDados`.
```

- **O que aceitei:** a separação entre `AtualizarDados` e `ValidarDados`, onde o CPF é validado apenas na criação e os campos comuns são validados em um método privado reutilizável.
- **O que descartei e por quê:** a sugestão de criar uma exceção específica (`InativoException`) – mantive `ConflitoException` para consistência com o `PlanoServico` e com o padrão de erros da aplicação.

**Prompt 3**

```
- **“Como garantir a unicidade do CPF mesmo com concorrência?”** – Resultou no índice único no banco e na captura de `DbUpdateException`.
```
 **O que aceitei:** a combinação de índice único no banco (`HasIndex(b => b.Cpf).IsUnique()`) com captura de `DbUpdateException` no método `SalvarAsync`, convertendo para `ConflitoException`.
- **O que descartei e por quê:** a ideia de depender apenas da consulta prévia (`AnyAsync`) – não é segura em concorrência, pois duas requisições podem passar pela verificação antes de qualquer uma ser persistida. O índice único é a garantia real.

---

**Outros prompts de suporte** (não listados como principais, mas igualmente úteis):
- Ajuste de testes para alinhar com a especificação (tamanho 10, retorno 409).
- Correção do mapeamento de `plano_id` (snake_case) no controller.
- Resolução do problema de timezone no conversor `DateOnly` com `DateTime.SpecifyKind`.


### 3.3 O que fiz sem IA

Apesar do uso intenso da IA, algumas atividades foram realizadas manualmente porque exigiam compreensão contextual e não pura geração de código:

- **Análise da arquitetura existente**: estudei a estrutura do módulo de Planos para identificar padrões de camadas, injeção de dependência, tratamento de erros e nomenclatura – tudo isso para garantir que o módulo de Beneficiários seguisse exatamente o mesmo modelo.
- **Levantamento de requisitos**: li a especificação (`SPEC.md`) e comparei com o código existente para identificar inconsistências (ex: teste esperando 20, spec definindo 10).
- **Decisões sobre pontos omissos**: escolhi manualmente a ordenação padrão (`NomeCompleto + Id`), o uso de filtros manuais (botão "Filtrar") e a separação de DTOs para POST e PUT.
- **Testes manuais**: validei o comportamento da API e do frontend para garantir que as regras de negócio estavam sendo aplicadas corretamente (ex: inativo congelado, exclusão lógica, paginação).


### 3.4 O que ainda não domino

Apesar de conseguir entender e modificar todo o código produzido, há uma parte do frontend Angular que exigiria mais estudo para explicar em detalhe:

- **A lógica de `Subject` + `switchMap` no `beneficiarios-lista.ts`**: como venho trabalhando mais com React nos últimos anos, o ecossistema do Angular (especialmente signals e rxjs) ainda é algo que estou reaprendendo. Consigo entender o funcionamento geral e sei modificar o código se necessário, mas explicar a fundo a interação entre `Subject`, `takeUntilDestroyed` e `switchMap` em uma conversa técnica mais aprofundada exigiria uma breve revisão.

---

## 4. Perguntas de compreensão

### 4.1 Concorrência

**O que acontece se duas requisições simultâneas tentarem criar beneficiários com o mesmo
CPF? Onde exatamente, na sua implementação, a unicidade é garantida?**

A unicidade do CPF é garantida em duas camadas:

1. **Garantia real (banco de dados)** – no `AppDbContext.cs`, a configuração `entidade.HasIndex(b => b.Cpf).IsUnique();` cria um índice único na coluna `Cpf`. Qualquer tentativa de inserir um CPF duplicado é rejeitada pelo PostgreSQL, que retorna o erro `23505`.

2. **Proteção adicional (serviço)** – no `BeneficiarioServico`, o método `CriarAsync` executa uma consulta prévia (`GarantirUnicidadeAsync`) para verificar se o CPF já existe. Isso permite retornar `409 Conflict` mais rapidamente, sem esperar a exceção do banco.

**Fluxo em caso de concorrência:**
- Duas requisições com o mesmo CPF chegam simultaneamente.
- Ambas passam pela consulta prévia e, como o registro ainda não foi persistido, ambas concluem que o CPF é único.
- Ambas tentam salvar ao mesmo tempo.
- O banco permite apenas uma inserção; a outra falha com `DbUpdateException` (código `23505`).
- O método `SalvarAsync` captura a exceção e a converte em `ConflitoException`, resultando em `409 Conflict` para o cliente.

**Conclusão:** a unicidade é garantida **no banco de dados** pelo índice único, que é a única camada capaz de resolver atomicamente a condição de corrida.

---


### 4.2 Um defeito que você corrigiu

**Escolha um dos defeitos que encontrou no código base e explique: por que o código original
estava errado, e em que situação real ele quebraria em produção?**

**Defeito escolhido:** O código original permitia que um beneficiário **INATIVO** tivesse seus dados cadastrais alterados, contrariando a especificação.

**Onde estava:** No método `AtualizarDados` da classe `Beneficiario`. O código aplicava as alterações sem verificar o status.

**Por que estava errado:** A especificação (seção 2.3) afirma que um beneficiário INATIVO é um registro congelado – seus dados não podem ser alterados, e a tentativa deve retornar `409`.

**Situação real:** Um operador desativa um beneficiário. Com o código original, ele poderia editar o nome ou plano desse beneficiário inativo, gerando inconsistências em relatórios e auditorias. A mudança de status (reativação) deveria ser a única alteração permitida.
Quebra de regra de negócio =>	A especificação define o status INATIVO como "congelado". Permitir alterações viola essa regra, tornando o sistema imprevisível.

**Correção:** Adicionei a seguinte lógica:
```csharp
bool alterouNome = nomeCompleto != null && nomeCompleto.Trim() != NomeCompleto;
bool alterouData = dataNascimento != default && dataNascimento != DataNascimento;
bool alterouPlano = planoId != Guid.Empty && planoId != PlanoId;

if (Status == StatusBeneficiario.INATIVO && (alterouNome || alterouData || alterouPlano))
{
    throw new ConflitoException("Beneficiario inativo nao pode ter dados alterados", ...);
}


### 4.3 O trecho mais complexo

**Escolha o trecho mais complexo que a IA gerou para você, ou o trecho mais complexo do
projeto se você não usou IA, e explique linha a linha o que ele faz.**

**Trecho escolhido:** A lógica de recarregamento no `beneficiarios-lista.ts`, que usa `Subject` + `switchMap` para evitar o erro `NG0203` e gerenciar requisições concorrentes.

```typescript
private readonly recarregar$ = new Subject<void>();

constructor() {
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
      next: (resposta) => {
        this.beneficiarios.set(resposta.dados);
        this.total.set(resposta.total);
        this.carregando.set(false);
      },
      error: (resposta) => {
        this.erro.set(mensagemDeErro(resposta));
        this.carregando.set(false);
      }
    });

  this.recarregar$.next();
}

protected carregarBeneficiarios(): void {
  this.recarregar$.next();
}

**Explicação linha a linha:**

| Linha / Bloco | Explicação |
|---------------|------------|
| `private readonly recarregar$ = new Subject<void>();` | Cria um **Subject** – um tipo especial de Observable que funciona como um "gatilho" que pode ser acionado manualmente a qualquer momento para iniciar o carregamento. |
| `this.recarregar$.pipe(...)` | Aplica operadores à stream do Subject. A stream só executa quando alguém chama `recarregar$.next()`. |
| `takeUntilDestroyed()` | Cancela a inscrição automaticamente quando o componente for destruído. Evita vazamento de memória. **Está no construtor**, portanto não gera o erro `NG0203` (contexto de injeção válido). |
| `switchMap(() => { ... })` | Cada emissão do Subject dispara a requisição. Se uma nova emissão ocorrer enquanto a anterior ainda estiver rodando, `switchMap` **cancela a anterior** e inicia a nova – prevenindo requisições concorrentes. |
| `this.carregando.set(true)` e `this.erro.set(null)` | Define o estado de loading como verdadeiro e limpa qualquer erro anterior – feedback visual imediato para o usuário. |
| `return this.beneficiarioServico.listar(...)` | Chama a API com os parâmetros atuais (página, tamanho, filtros). O retorno é um Observable que será consumido pelo `subscribe`. |
| `.subscribe({ next: ..., error: ... })` | Processa a resposta da API. `next` atualiza a lista e o total; `error` exibe a mensagem de erro. Ambos desativam o loading. |
| `this.recarregar$.next();` (no construtor) | Dispara o **primeiro carregamento** automaticamente assim que o componente é criado. |
| `protected carregarBeneficiarios(): void { this.recarregar$.next(); }` | Método público chamado pelo botão "Recarregar" ou pelos filtros. Apenas emite um sinal no Subject – **não faz a requisição diretamente**, mantendo a lógica centralizada. |

**Por que isso é complexo:** Essa abordagem exige compreensão de **streams reativas** (RxJS), o papel do `Subject` como gatilho, o comportamento de `switchMap` (cancelamento de requisições anteriores) e o contexto de injeção do `takeUntilDestroyed`. É um padrão poderoso, mas demanda conhecimento de programação reativa e do ecossistema Angular.
