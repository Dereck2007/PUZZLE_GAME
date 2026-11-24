using MathPuzzle.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace MathPuzzle.Web.Controllers
{
    public class RankingController : Controller
    {
        private readonly PartidaRepository _partidaRepository;

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public RankingController(IConfiguration configuration)
        {
            // Crear el repositorio para consultar SQL Server.
            _partidaRepository = new PartidaRepository(configuration);
        }

        // =========================================
        // MOSTRAR EL RANKING DE JUGADORES
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Obtener las 10 mejores partidas.
            var ranking = await _partidaRepository
                .ObtenerRankingAsync();

            // Enviar los resultados a la vista.
            return View(ranking);
        }
    }
}