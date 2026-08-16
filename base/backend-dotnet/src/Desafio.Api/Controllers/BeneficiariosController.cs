using Desafio.Api.Api.Contratos;
using Desafio.Api.Aplicacao;
using Desafio.Api.Dominio;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.Api.Controllers;

[ApiController]
[Route("beneficiarios")]
[Produces("application/json")]
public class BeneficiariosController : ControllerBase
{
    private readonly BeneficiarioServico _servico;

    public BeneficiariosController(BeneficiarioServico servico)
    {
        _servico = servico;
    }

    [HttpGet]
    [ProducesResponseType<BeneficiarioListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
    [FromQuery] int pagina = 1,
    [FromQuery] int tamanho = 10,
    [FromQuery] StatusBeneficiario? status = null,
    [FromQuery] Guid? planoId = null,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var resultado = await _servico.ListarAsync(pagina, tamanho, status, planoId, cancellationToken);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErroResponse(
                "ParametroInvalido",
                ex.Message,
                new[] { new DetalheErro(ex.ParamName ?? "parametro", "invalido") }
            ));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await _servico.ObterPorIdAsync(id, cancellationToken);
        return Ok(BeneficiarioResponse.De(beneficiario));
    }

    [HttpPost]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar(
        [FromBody] BeneficiarioRequest requisicao,
        CancellationToken cancellationToken)
    {
        try
        {
            var beneficiario = await _servico.CriarAsync(requisicao, cancellationToken);
            var response = BeneficiarioResponse.De(beneficiario);
            return CreatedAtAction(nameof(Obter), new { id = response.Id }, response);
        }
        catch (ConflitoException ex) when (ex.Detalhes.Any(d => d.Campo == "plano_id"))
        {
            // Plano inexistente → 422
            return UnprocessableEntity(new ErroResponse(
                "plano_inexistente",
                ex.Message,
                ex.Detalhes
            ));
        }
        // Outras ConflitoException (CPF duplicado) serão capturadas pelo middleware e retornarão 409.
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(
    Guid id,
    [FromBody] BeneficiarioUpdateRequest requisicao,
    CancellationToken cancellationToken)
    {
        try
        {
            var beneficiario = await _servico.AtualizarAsync(id, requisicao, cancellationToken);
            return Ok(BeneficiarioResponse.De(beneficiario));
        }
        catch (ConflitoException ex) when (ex.Detalhes.Any(d => d.Campo == "plano_id"))
        {
            return UnprocessableEntity(new ErroResponse("plano_inexistente", ex.Message, ex.Detalhes));
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        await _servico.ExcluirAsync(id, cancellationToken);
        return NoContent();
    }
}