using Desafio.Api.Dominio;

namespace Desafio.Api.Api.Contratos;

public sealed record BeneficiarioRequest(
    string NomeCompleto,
    string Cpf,
    DateOnly DataNascimento,
    Guid PlanoId,
    StatusBeneficiario? Status = null // opcional – usado no PUT
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