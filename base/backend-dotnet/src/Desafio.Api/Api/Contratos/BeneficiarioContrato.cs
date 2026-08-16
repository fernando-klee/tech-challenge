using Desafio.Api.Dominio;

namespace Desafio.Api.Api.Contratos;

public sealed record BeneficiarioRequest(
    string NomeCompleto,
    string Cpf,
    DateOnly DataNascimento,
    Guid PlanoId
);

public sealed record BeneficiarioUpdateRequest(
    string NomeCompleto,
    DateOnly DataNascimento,
    Guid PlanoId,
    StatusBeneficiario? Status = null
);

public sealed record BeneficiarioListResponse(
    IReadOnlyList<BeneficiarioResponse> Dados,
    int Pagina,
    int Tamanho,
    int Total
);

public sealed record BeneficiarioResponse(
    Guid Id,
    string NomeCompleto,
    string Cpf,
    DateOnly DataNascimento,
    string Status,
    Guid PlanoId,
    PlanoResponse? Plano,
    DateTime DataCadastro
)
{
    public static BeneficiarioResponse De(Beneficiario beneficiario) =>
        new(
            beneficiario.Id,
            beneficiario.NomeCompleto,
            beneficiario.Cpf,
            beneficiario.DataNascimento,
            beneficiario.Status.ToString(),
            beneficiario.PlanoId,
            beneficiario.Plano is not null ? PlanoResponse.De(beneficiario.Plano) : null,
            beneficiario.DataCadastro
        );
}