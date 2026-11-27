using MathPuzzle.Web.Models;
using MathPuzzle.Web.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MathPuzzle.Web.Controllers
{
    public class ContrarrelojController : Controller
    {
        private const int PuntosPorRespuesta = 100;
        private const int TotalPreguntas = 10;
        public const int SegundosPorPregunta = 15;

        // Margen extra por si la página tarda un poco en enviar la respuesta.
        private const int SegundosDeGracia = 2;

        private const string ClaveJugador = "Contrarreloj_Jugador";
        private const string ClavePreguntas = "Contrarreloj_Preguntas";
        private const string ClavePosicion = "Contrarreloj_Posicion";
        private const string ClavePuntaje = "Contrarreloj_Puntaje";
        private const string ClaveAciertos = "Contrarreloj_Aciertos";
        private const string ClaveInicioPregunta = "Contrarreloj_InicioPregunta";
        private const string ClaveIdUnicoPartida = "Contrarreloj_IdUnicoPartida";
        private const string ClavePartidaGuardada = "Contrarreloj_PartidaGuardada";

        private readonly PartidaRepository _partidaRepository;

        private static readonly string[] ClavesSesion =
        {
            ClaveJugador,
            ClavePreguntas,
            ClavePosicion,
            ClavePuntaje,
            ClaveAciertos,
            ClaveInicioPregunta,
            ClaveIdUnicoPartida,
            ClavePartidaGuardada
        };

        public ContrarrelojController(IConfiguration configuration)
        {
            _partidaRepository = new PartidaRepository(configuration);
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Iniciar(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                ViewBag.Error = "Debes ingresar tu nombre.";
                return View("Index");
            }

            nombre = nombre.Trim();

            if (nombre.Length > 40)
            {
                ViewBag.Error = "El nombre no puede superar 40 caracteres.";
                return View("Index");
            }

            foreach (string clave in ClavesSesion)
            {
                HttpContext.Session.Remove(clave);
            }

            List<Pregunta> preguntas = GenerarPreguntas();

            HttpContext.Session.SetString(ClaveJugador, nombre);
            HttpContext.Session.SetString(ClavePreguntas, JsonSerializer.Serialize(preguntas));
            HttpContext.Session.SetInt32(ClavePosicion, 0);
            HttpContext.Session.SetInt32(ClavePuntaje, 0);
            HttpContext.Session.SetInt32(ClaveAciertos, 0);
            HttpContext.Session.SetString(ClaveIdUnicoPartida, Guid.NewGuid().ToString("D"));
            HttpContext.Session.SetInt32(ClavePartidaGuardada, 0);

            return RedirectToAction(nameof(Pregunta));
        }

        [HttpGet]
        public IActionResult Pregunta()
        {
            List<Pregunta> preguntas = ObtenerPreguntasSesion();

            if (preguntas.Count == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            int posicion = HttpContext.Session.GetInt32(ClavePosicion) ?? 0;

            if (posicion >= preguntas.Count)
            {
                return RedirectToAction(nameof(Resultado));
            }

            // Si el jugador recarga la página, el tiempo NO se reinicia.
            DateTime inicio = ObtenerOCrearInicioPregunta(posicion);
            double transcurridoMs = (DateTime.UtcNow - inicio).TotalMilliseconds;
            int msRestantes = (int)Math.Max(0, SegundosPorPregunta * 1000 - transcurridoMs);

            ViewBag.NumeroPregunta = posicion + 1;
            ViewBag.TotalPreguntas = preguntas.Count;
            ViewBag.Jugador = HttpContext.Session.GetString(ClaveJugador);
            ViewBag.Puntaje = HttpContext.Session.GetInt32(ClavePuntaje) ?? 0;
            ViewBag.TipoOperacion = ObtenerTipoOperacion(posicion);
            ViewBag.MsRestantes = msRestantes;
            ViewBag.SegundosPorPregunta = SegundosPorPregunta;

            return View(preguntas[posicion]);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Responder(int idPregunta, int indiceRespuesta)
        {
            List<Pregunta> preguntas = ObtenerPreguntasSesion();

            if (preguntas.Count == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            int posicion = HttpContext.Session.GetInt32(ClavePosicion) ?? 0;

            if (posicion >= preguntas.Count)
            {
                return RedirectToAction(nameof(Resultado));
            }

            Pregunta pregunta = preguntas[posicion];

            if (pregunta.Id != idPregunta)
            {
                return RedirectToAction(nameof(Pregunta));
            }

            // El servidor también revisa el tiempo, para que no se pueda hacer trampa
            // desactivando el JavaScript. indiceRespuesta = -1 significa "se acabó el tiempo".
            bool aTiempo = false;

            if (TryObtenerInicioPregunta(posicion, out DateTime inicio))
            {
                double segundos = (DateTime.UtcNow - inicio).TotalSeconds;
                aTiempo = segundos <= SegundosPorPregunta + SegundosDeGracia;
            }

            bool correcta = aTiempo &&
                            indiceRespuesta >= 0 &&
                            indiceRespuesta < pregunta.Opciones.Count &&
                            indiceRespuesta == pregunta.IndiceRespuestaCorrecta;

            if (correcta)
            {
                int puntaje = HttpContext.Session.GetInt32(ClavePuntaje) ?? 0;
                int aciertos = HttpContext.Session.GetInt32(ClaveAciertos) ?? 0;

                HttpContext.Session.SetInt32(ClavePuntaje, puntaje + PuntosPorRespuesta);
                HttpContext.Session.SetInt32(ClaveAciertos, aciertos + 1);
            }

            HttpContext.Session.SetInt32(ClavePosicion, posicion + 1);
            HttpContext.Session.Remove(ClaveInicioPregunta);

            return RedirectToAction(nameof(Pregunta));
        }

        [HttpGet]
        public async Task<IActionResult> Resultado()
        {
            List<Pregunta> preguntas = ObtenerPreguntasSesion();

            if (preguntas.Count == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            int posicion = HttpContext.Session.GetInt32(ClavePosicion) ?? 0;

            if (posicion < preguntas.Count)
            {
                return RedirectToAction(nameof(Pregunta));
            }

            string? identificadorGuardado = HttpContext.Session.GetString(ClaveIdUnicoPartida);

            if (!Guid.TryParse(identificadorGuardado, out Guid idUnicoPartida) ||
                idUnicoPartida == Guid.Empty)
            {
                return RedirectToAction(nameof(Index));
            }

            string jugador = HttpContext.Session.GetString(ClaveJugador) ?? "Jugador";
            int puntaje = HttpContext.Session.GetInt32(ClavePuntaje) ?? 0;
            int aciertos = HttpContext.Session.GetInt32(ClaveAciertos) ?? 0;
            int totalPreguntas = preguntas.Count;

            if ((HttpContext.Session.GetInt32(ClavePartidaGuardada) ?? 0) == 0)
            {
                await _partidaRepository.GuardarPartidaAsync(
                    jugador,
                    puntaje,
                    aciertos,
                    totalPreguntas,
                    "Contrarreloj",
                    idUnicoPartida
                );

                HttpContext.Session.SetInt32(ClavePartidaGuardada, 1);
            }

            ViewBag.Jugador = jugador;
            ViewBag.Puntaje = puntaje;
            ViewBag.Aciertos = aciertos;
            ViewBag.TotalPreguntas = totalPreguntas;
            ViewBag.Porcentaje = (int)Math.Round((double)aciertos / totalPreguntas * 100);

            return View();
        }

        // =========================================
        // GENERACIÓN DE OPERACIONES (NIVEL FÁCIL)
        // Preguntas 1-4: sumas del 1 al 20
        // Preguntas 5-7: restas que nunca dan negativo
        // Preguntas 8-10: tablas del 2 al 5
        // =========================================

        private static List<Pregunta> GenerarPreguntas()
        {
            var preguntas = new List<Pregunta>();

            for (int i = 0; i < TotalPreguntas; i++)
            {
                int a;
                int b;
                int resultado;
                string simbolo;

                if (i < 4)
                {
                    a = Random.Shared.Next(1, 21);
                    b = Random.Shared.Next(1, 21);
                    resultado = a + b;
                    simbolo = "+";
                }
                else if (i < 7)
                {
                    a = Random.Shared.Next(5, 21);
                    b = Random.Shared.Next(1, a + 1);
                    resultado = a - b;
                    simbolo = "−";
                }
                else
                {
                    a = Random.Shared.Next(2, 6);
                    b = Random.Shared.Next(1, 11);
                    resultado = a * b;
                    simbolo = "×";
                }

                List<string> opciones = GenerarOpciones(resultado);

                preguntas.Add(new Pregunta
                {
                    Id = i + 1,
                    Enunciado = $"{a} {simbolo} {b}",
                    Opciones = opciones,
                    IndiceRespuestaCorrecta = opciones.IndexOf(resultado.ToString())
                });
            }

            return preguntas;
        }

        // Crea 4 opciones: la correcta y 3 cercanas (sin negativos ni repetidas), en orden aleatorio.
        private static List<string> GenerarOpciones(int correcta)
        {
            var valores = new HashSet<int> { correcta };

            while (valores.Count < 4)
            {
                int diferencia = Random.Shared.Next(-5, 6);
                int candidato = correcta + diferencia;

                if (diferencia != 0 && candidato >= 0)
                {
                    valores.Add(candidato);
                }
            }

            return valores
                .OrderBy(_ => Random.Shared.Next())
                .Select(v => v.ToString())
                .ToList();
        }

        private static string ObtenerTipoOperacion(int posicion)
        {
            if (posicion < 4)
            {
                return "Suma";
            }

            return posicion < 7 ? "Resta" : "Multiplicación";
        }

        private List<Pregunta> ObtenerPreguntasSesion()
        {
            string? datos = HttpContext.Session.GetString(ClavePreguntas);

            if (string.IsNullOrWhiteSpace(datos))
            {
                return new List<Pregunta>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<Pregunta>>(datos) ?? new List<Pregunta>();
            }
            catch (JsonException)
            {
                return new List<Pregunta>();
            }
        }

        // Se guarda como "posicion|ticks" para saber a qué pregunta pertenece el tiempo.
        private DateTime ObtenerOCrearInicioPregunta(int posicion)
        {
            if (TryObtenerInicioPregunta(posicion, out DateTime inicio))
            {
                return inicio;
            }

            DateTime ahora = DateTime.UtcNow;
            HttpContext.Session.SetString(ClaveInicioPregunta, $"{posicion}|{ahora.Ticks}");
            return ahora;
        }

        private bool TryObtenerInicioPregunta(int posicion, out DateTime inicio)
        {
            inicio = DateTime.MinValue;
            string? datos = HttpContext.Session.GetString(ClaveInicioPregunta);

            if (string.IsNullOrWhiteSpace(datos))
            {
                return false;
            }

            string[] partes = datos.Split('|');

            if (partes.Length == 2 &&
                int.TryParse(partes[0], out int posicionGuardada) &&
                posicionGuardada == posicion &&
                long.TryParse(partes[1], out long ticks))
            {
                inicio = new DateTime(ticks, DateTimeKind.Utc);
                return true;
            }

            return false;
        }
    }
}
