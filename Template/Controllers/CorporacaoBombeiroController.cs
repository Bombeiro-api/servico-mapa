using Microsoft.AspNetCore.Mvc;
using ServicoMapa.Models;
using ServicoMapa.Servicos;

namespace ServicoMapa.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CorporacaoBombeiroController : ControllerBase
    {
        private readonly IServCorporacaoBombeiro _servCorporacao;

        public CorporacaoBombeiroController(IServCorporacaoBombeiro servCorporacao)
        {
            _servCorporacao = servCorporacao;
        }

        [HttpGet]
        public async Task<IActionResult> Listar() =>
            Ok(await _servCorporacao.ListarAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            var corporacao = await _servCorporacao.ObterPorIdAsync(id);
            return corporacao is null ? NotFound() : Ok(corporacao);
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CorporacaoBombeiro corporacao)
        {
            var criada = await _servCorporacao.CriarAsync(corporacao);
            return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Atualizar(int id, [FromBody] CorporacaoBombeiro corporacao)
        {
            var atualizada = await _servCorporacao.AtualizarAsync(id, corporacao);
            return atualizada is null ? NotFound() : Ok(atualizada);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(int id)
        {
            var removida = await _servCorporacao.RemoverAsync(id);
            return removida ? NoContent() : NotFound();
        }
    }
}
