using Desafio.Api.Api.Contratos;
using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Desafio.Api.Aplicacao;

public class BeneficiarioServico(AppDbContext db)
{
    private const string CodigoViolacaoDeUnicidade = "23505";

    public async Task<IReadOnlyList<Beneficiario>> ListarAsync(CancellationToken cancellationToken)
    {
        return await db.Beneficiarios
            .AsNoTracking()
            .Include(b => b.Plano)
            .OrderBy(b => b.NomeCompleto)
            .ToListAsync(cancellationToken);
    }

    public async Task<Beneficiario> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.Beneficiarios
            .Include(b => b.Plano)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NaoEncontradoException("Beneficiário não encontrado");
    }

    public async Task<Beneficiario> CriarAsync(BeneficiarioRequest dados, CancellationToken cancellationToken)
    {
        // 1. Verifica se o plano existe (422)
        var planoExiste = await db.Planos.AnyAsync(p => p.Id == dados.PlanoId, cancellationToken);
        if (!planoExiste)
            throw new ConflitoException(
                "Plano não encontrado",
                [new DetalheErro("plano_id", "inexistente")]
            );

        // 2. Cria a entidade (validações de domínio são disparadas no construtor)
        var beneficiario = new Beneficiario(
            dados.NomeCompleto,
            dados.Cpf,
            dados.DataNascimento,
            dados.PlanoId
        );

        // 3. Garante unicidade (consulta prévia, mas a garantia real é o índice único)
        await GarantirUnicidadeAsync(beneficiario, cancellationToken);

        db.Beneficiarios.Add(beneficiario);
        await SalvarAsync(cancellationToken);

        // 4. Carrega o plano para a resposta (opcional, mas útil)
        await db.Entry(beneficiario).Reference(b => b.Plano).LoadAsync(cancellationToken);

        return beneficiario;
    }

    public async Task<Beneficiario> AtualizarAsync(
        Guid id,
        BeneficiarioRequest dados,
        CancellationToken cancellationToken)
    {
        var beneficiario = await ObterPorIdAsync(id, cancellationToken);

        // Atualiza dados (nome, data, planoId) e opcionalmente status
        beneficiario.AtualizarDados(
            dados.NomeCompleto,
            dados.DataNascimento,
            dados.PlanoId,
            dados.Status // opcional, se enviado
        );

        await SalvarAsync(cancellationToken);

        // Recarrega o plano
        await db.Entry(beneficiario).Reference(b => b.Plano).LoadAsync(cancellationToken);

        return beneficiario;
    }

    public async Task ExcluirAsync(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await ObterPorIdAsync(id, cancellationToken);
        beneficiario.Excluir();
        await SalvarAsync(cancellationToken);
    }

    // Garantia de unicidade do CPF (consulta prévia)
    private async Task GarantirUnicidadeAsync(Beneficiario beneficiario, CancellationToken cancellationToken)
    {
        var conflito = await db.Beneficiarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.Id != beneficiario.Id)
            .Where(b => b.Cpf == beneficiario.Cpf)
            .FirstOrDefaultAsync(cancellationToken);

        if (conflito is not null)
            throw new ConflitoException(
                "CPF já cadastrado",
                [new DetalheErro("cpf", "duplicado")]
            );
    }

    // Captura violação de índice único (garantia real contra concorrência)
    private async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhViolacaoDeUnicidade(excecao))
        {
            throw new ConflitoException(
                "CPF já cadastrado",
                [new DetalheErro("cpf", "duplicado")]
            );
        }
    }

    private static bool EhViolacaoDeUnicidade(DbUpdateException excecao) =>
        excecao.InnerException is PostgresException postgres &&
        postgres.SqlState == CodigoViolacaoDeUnicidade;
}