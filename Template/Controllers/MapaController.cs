using Microsoft.AspNetCore.Mvc;
using ServicoMapa.DTO;
using ServicoMapa.Servicos;

namespace ServicoMapa.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapaController : ControllerBase
    {
        private readonly IServMapa _servMapa;

        public MapaController(IServMapa servMapa)
        {
            _servMapa = servMapa;
        }

        /// <summary>
        /// Encontra a corporação de bombeiros mais próxima e retorna a rota até o local do incêndio.
        /// </summary>
        [HttpPost("rota-mais-proxima")]
        public async Task<IActionResult> RotaMaisProxima([FromBody] RoteamentoRequestDTO request)
        {
            try
            {
                var resultado = await _servMapa.BuscarRotaMaisProxima(request);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
