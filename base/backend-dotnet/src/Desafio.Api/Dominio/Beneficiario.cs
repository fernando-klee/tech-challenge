using System.Text.RegularExpressions;

namespace Desafio.Api.Dominio;

public enum StatusBeneficiario
{
    ATIVO,
    INATIVO
}
// classe partial para conseguir compilar pois usa generated
public partial class Beneficiario
{
    private Beneficiario()
    {
    }

    public Beneficiario(string? nomeCompleto, string? cpf, DateOnly dataNascimento, Guid planoId)
        : this(Guid.NewGuid(), nomeCompleto, cpf, dataNascimento, planoId)
    {
    }

    public Beneficiario(Guid id, string? nomeCompleto, string? cpf, DateOnly dataNascimento, Guid planoId) 
    {
        Id = id;
        Status = StatusBeneficiario.ATIVO;
        DataCadastro = DateTime.UtcNow;
        DefinirDados(nomeCompleto, cpf, dataNascimento, planoId);

    }

    public Guid Id { get; private set; }

    public string NomeCompleto { get; private set; } = null!;

    public string Cpf { get; private set; } = null!;

    public DateOnly DataNascimento { get; private set; }

    public StatusBeneficiario Status { get; private set; }

    public Guid PlanoId { get; private set; }

    public Plano? Plano { get; private set; }

    public DateTime DataCadastro { get; private set; }

    public DateTime? ExcluidoEm { get; private set; }


    public void DefinirDados(string? nomeCompleto, string? cpf, DateOnly dataNascimento, Guid planoId)
    {
        nomeCompleto = nomeCompleto?.Trim() ?? string.Empty;
        cpf = cpf?.Trim() ?? string.Empty;

        var detalhes = new List<DetalheErro>();

        if (cpf.Length == 0)
            detalhes.Add(new DetalheErro("cpf", "obrigatorio"));
        else if (!FormatoDoCpf().IsMatch(cpf))
            detalhes.Add(new DetalheErro("cpf", "formato_invalido"));
        else if (!CpfValido(cpf))
            detalhes.Add(new DetalheErro("cpf", "cpf_invalido"));

        try
        {
            ValidarDados(nomeCompleto, dataNascimento, planoId);
        }
        catch (ValidacaoException ex)
        {
            detalhes.AddRange(ex.Detalhes);
        }

        if (detalhes.Count > 0)
            throw new ValidacaoException("Dados do beneficiário inválidos", detalhes);

        NomeCompleto = nomeCompleto;
        Cpf = cpf;
        DataNascimento = dataNascimento;
        PlanoId = planoId;
    }

    public void Ativar()
    {
        if (Status == StatusBeneficiario.INATIVO)
            Status = StatusBeneficiario.ATIVO;
             
    }

    public void Inativar()
    {
        if (Status == StatusBeneficiario.ATIVO)
            Status = StatusBeneficiario.INATIVO;
    }

    public void Excluir() => ExcluidoEm = DateTime.UtcNow;

    public void AtualizarDados(string? nomeCompleto, DateOnly dataNascimento, Guid planoId, StatusBeneficiario? status = null)
    {
        bool alterouNome = nomeCompleto != null && nomeCompleto.Trim() != NomeCompleto;
        bool alterouData = dataNascimento != default && dataNascimento != DataNascimento;
        bool alterouPlano = planoId != Guid.Empty && planoId != PlanoId;

        if (Status == StatusBeneficiario.INATIVO && (alterouNome || alterouData || alterouPlano))
        {
            throw new ValidacaoException("Beneficiario inativo nao pode ter dados alterados", new List<DetalheErro>
        {
            new DetalheErro("status", "inativo_nao_permite_alteracao")
        });
        }

        var nomeParaValidar = nomeCompleto ?? NomeCompleto;
        var dataParaValidar = dataNascimento != default ? dataNascimento : DataNascimento;
        var planoIdParaValidar = planoId != Guid.Empty ? planoId : PlanoId;

        ValidarDados(nomeParaValidar, dataParaValidar, planoIdParaValidar);

        if (alterouNome) NomeCompleto = nomeCompleto!.Trim();
        if (alterouData) DataNascimento = dataNascimento;
        if (alterouPlano) PlanoId = planoId;

        if (status.HasValue && status.Value != Status)
            Status = status.Value;
    }

    private void ValidarDados(string? nomeCompleto, DateOnly dataNascimento, Guid planoId)
    {
        nomeCompleto = nomeCompleto?.Trim() ?? string.Empty;
        var detalhes = new List<DetalheErro>();

        if (nomeCompleto.Length == 0)
            detalhes.Add(new DetalheErro("nome_completo", "obrigatorio"));
        else if (nomeCompleto.Length is < 3 or > 120)
            detalhes.Add(new DetalheErro("nome_completo", "tamanho_invalido"));

        if (dataNascimento >= DateOnly.FromDateTime(DateTime.UtcNow))
            detalhes.Add(new DetalheErro("data_nascimento", "data_futura"));

        if (planoId == Guid.Empty)
            detalhes.Add(new DetalheErro("plano_id", "obrigatorio"));

        if (detalhes.Count > 0)
            throw new ValidacaoException("Dados do beneficiário inválidos", detalhes);
    }

    //fiz antes para usar no if de validacao cpf
    private static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11)
        {
            return false;
        }

        if(cpf.Distinct().Count() == 1)
        {
            return false;
        }

        int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCpf = cpf.Substring(0, 9);
        int soma = 0;

        for(int i =0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

        int resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        string digito = resto.ToString();
        tempCpf += digito;

        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];


        resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        digito += resto.ToString();

        return cpf.EndsWith(digito);

    }
    
    [GeneratedRegex("^[0-9]{11}$")]
    private static partial Regex FormatoDoCpf();
}
