using MathPuzzle.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MathPuzzle.Web.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MathPuzzle.Web.Controllers
{
    public class SecuenciasController : Controller
    {
        private const int PuntosPorRespuesta = 100;

        private const string ClaveJugador = "Secuencias_Jugador";
        private const string ClavePreguntasIds = "Secuencias_PreguntasIds";
        private const string ClaveOrdenOpciones = "Secuencias_OrdenOpciones";
        private const string ClavePosicion = "Secuencias_Posicion";
        private const string ClavePuntaje = "Secuencias_Puntaje";
        private const string ClaveAciertos = "Secuencias_Aciertos";
        private const string ClaveIdUnicoPartida = "Secuencias_IdUnicoPartida";
        private const string ClavePartidaGuardada = "Secuencias_PartidaGuardada";

        private readonly PartidaRepository _partidaRepository;

        private static readonly string[] ClavesSesion =
        {
            ClaveJugador,
            ClavePreguntasIds,
            ClaveOrdenOpciones,
            ClavePosicion,
            ClavePuntaje,
            ClaveAciertos,
            ClaveIdUnicoPartida,
            ClavePartidaGuardada
        };

        public SecuenciasController(IConfiguration configuration)
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

            var seleccionadas = ObtenerPreguntas()
                .OrderBy(p => ObtenerNivelPorId(p.Id))
                .ThenBy(_ => Random.Shared.Next())
                .ToList();

            Dictionary<int, int[]> ordenes = new Dictionary<int, int[]>();

            foreach (var pregunta in seleccionadas)
            {
                int[] orden = Enumerable.Range(0, pregunta.Opciones.Count).ToArray();

                for (int i = orden.Length - 1; i > 0; i--)
                {
                    int j = Random.Shared.Next(i + 1);
                    (orden[i], orden[j]) = (orden[j], orden[i]);
                }

                ordenes[pregunta.Id] = orden;
            }

            foreach (string clave in ClavesSesion)
            {
                HttpContext.Session.Remove(clave);
            }

            HttpContext.Session.SetString(ClaveJugador, nombre);
            HttpContext.Session.SetString(
                ClavePreguntasIds,
                string.Join(",", seleccionadas.Select(p => p.Id))
            );
            HttpContext.Session.SetString(
                ClaveOrdenOpciones,
                JsonSerializer.Serialize(ordenes)
            );
            HttpContext.Session.SetInt32(ClavePosicion, 0);
            HttpContext.Session.SetInt32(ClavePuntaje, 0);
            HttpContext.Session.SetInt32(ClaveAciertos, 0);
            HttpContext.Session.SetString(
                ClaveIdUnicoPartida,
                Guid.NewGuid().ToString("D")
            );
            HttpContext.Session.SetInt32(ClavePartidaGuardada, 0);

            return RedirectToAction(nameof(Pregunta));
        }

        [HttpGet]
        public IActionResult Pregunta()
        {
            int[] ids = ObtenerPreguntasSesion();

            if (ids.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            int posicion = HttpContext.Session.GetInt32(ClavePosicion) ?? 0;

            if (posicion >= ids.Length)
            {
                return RedirectToAction(nameof(Resultado));
            }

            var pregunta = ObtenerPreguntas()
                .FirstOrDefault(p => p.Id == ids[posicion]);

            if (pregunta == null)
            {
                return RedirectToAction(nameof(Index));
            }

            int[]? orden = ObtenerOrdenOpciones(pregunta.Id);

            if (!OrdenValido(orden, pregunta.Opciones.Count))
            {
                return RedirectToAction(nameof(Index));
            }

            Pregunta preguntaVista = new Pregunta
            {
                Id = pregunta.Id,
                Enunciado = pregunta.Enunciado,
                Opciones = orden!
                    .Select(i => pregunta.Opciones[i])
                    .ToList(),
                IndiceRespuestaCorrecta = Array.IndexOf(
                    orden!,
                    pregunta.IndiceRespuestaCorrecta
                )
            };

            ViewBag.NumeroPregunta = posicion + 1;
            ViewBag.TotalPreguntas = ids.Length;
            ViewBag.Jugador = HttpContext.Session.GetString(ClaveJugador);
            ViewBag.Puntaje = HttpContext.Session.GetInt32(ClavePuntaje) ?? 0;
            ViewBag.Dificultad = ObtenerNombreDificultad(posicion);
            ViewBag.Nivel = ObtenerNivel(posicion);

            return View(preguntaVista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Responder(int idPregunta, int indiceRespuesta)
        {
            int[] ids = ObtenerPreguntasSesion();

            if (ids.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            int posicion = HttpContext.Session.GetInt32(ClavePosicion) ?? 0;

            if (posicion >= ids.Length)
            {
                return RedirectToAction(nameof(Resultado));
            }

            if (ids[posicion] != idPregunta)
            {
                return RedirectToAction(nameof(Pregunta));
            }

            var pregunta = ObtenerPreguntas()
                .FirstOrDefault(p => p.Id == idPregunta);
            int[]? orden = ObtenerOrdenOpciones(idPregunta);

            if (pregunta == null || !OrdenValido(orden, pregunta.Opciones.Count))
            {
                return RedirectToAction(nameof(Index));
            }

            int indiceCorrecto = Array.IndexOf(
                orden!,
                pregunta.IndiceRespuestaCorrecta
            );

            if (indiceRespuesta >= 0 &&
                indiceRespuesta < orden!.Length &&
                indiceRespuesta == indiceCorrecto)
            {
                int puntaje = HttpContext.Session.GetInt32(ClavePuntaje) ?? 0;
                int aciertos = HttpContext.Session.GetInt32(ClaveAciertos) ?? 0;

                HttpContext.Session.SetInt32(
                    ClavePuntaje,
                    puntaje + PuntosPorRespuesta
                );
                HttpContext.Session.SetInt32(ClaveAciertos, aciertos + 1);
            }

            HttpContext.Session.SetInt32(ClavePosicion, posicion + 1);

            return RedirectToAction(nameof(Pregunta));
        }

        [HttpGet]
        public async Task<IActionResult> Resultado()
        {
            int[] ids = ObtenerPreguntasSesion();

            if (ids.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            int posicion = HttpContext.Session.GetInt32(ClavePosicion) ?? 0;

            if (posicion < ids.Length)
            {
                return RedirectToAction(nameof(Pregunta));
            }

            string? identificadorGuardado =
                HttpContext.Session.GetString(ClaveIdUnicoPartida);

            if (!Guid.TryParse(identificadorGuardado, out Guid idUnicoPartida) ||
                idUnicoPartida == Guid.Empty)
            {
                return RedirectToAction(nameof(Index));
            }

            string jugador = HttpContext.Session.GetString(ClaveJugador) ?? "Jugador";
            int puntaje = HttpContext.Session.GetInt32(ClavePuntaje) ?? 0;
            int aciertos = HttpContext.Session.GetInt32(ClaveAciertos) ?? 0;
            int totalPreguntas = ids.Length;

            if ((HttpContext.Session.GetInt32(ClavePartidaGuardada) ?? 0) == 0)
            {
                await _partidaRepository.GuardarPartidaAsync(
                    jugador,
                    puntaje,
                    aciertos,
                    totalPreguntas,
                    "Desafío de Secuencias",
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

        private static List<Pregunta> ObtenerPreguntas()
        {
            return new List<Pregunta>
            {
                new Pregunta { Id = 101, Enunciado = "2, 4, 6, 8, __", Opciones = new List<string> { "9", "10", "12", "14" }, IndiceRespuestaCorrecta = 1 },
                new Pregunta { Id = 102, Enunciado = "5, 10, 15, 20, __", Opciones = new List<string> { "25", "30", "22", "24" }, IndiceRespuestaCorrecta = 0 },
                new Pregunta { Id = 103, Enunciado = "3, 6, 9, 12, __", Opciones = new List<string> { "16", "18", "15", "21" }, IndiceRespuestaCorrecta = 2 },
                new Pregunta { Id = 104, Enunciado = "2, 4, 8, 16, __", Opciones = new List<string> { "24", "30", "32", "36" }, IndiceRespuestaCorrecta = 2 },
                new Pregunta { Id = 105, Enunciado = "1, 4, 9, 16, __", Opciones = new List<string> { "20", "24", "25", "36" }, IndiceRespuestaCorrecta = 2 },
                new Pregunta { Id = 106, Enunciado = "1, 1, 2, 3, 5, __", Opciones = new List<string> { "7", "8", "9", "10" }, IndiceRespuestaCorrecta = 1 },
                new Pregunta { Id = 107, Enunciado = "2, 5, 10, 17, 26, __", Opciones = new List<string> { "35", "36", "37", "38" }, IndiceRespuestaCorrecta = 2 },
                new Pregunta { Id = 108, Enunciado = "3, 6, 12, 24, __", Opciones = new List<string> { "36", "42", "48", "54" }, IndiceRespuestaCorrecta = 2 },
                new Pregunta { Id = 109, Enunciado = "1, 2, 6, 24, __", Opciones = new List<string> { "48", "96", "120", "144" }, IndiceRespuestaCorrecta = 2 },
                new Pregunta { Id = 110, Enunciado = "2, 6, 12, 20, 30, __", Opciones = new List<string> { "36", "40", "42", "44" }, IndiceRespuestaCorrecta = 2 }
            };
        }

        private int[] ObtenerPreguntasSesion()
        {
            string? datos = HttpContext.Session.GetString(ClavePreguntasIds);

            if (string.IsNullOrWhiteSpace(datos))
            {
                return Array.Empty<int>();
            }

            string[] elementos = datos.Split(',');
            int[] ids = new int[elementos.Length];

            for (int i = 0; i < elementos.Length; i++)
            {
                if (!int.TryParse(elementos[i], out ids[i]))
                {
                    return Array.Empty<int>();
                }
            }

            return ids;
        }

        private int[]? ObtenerOrdenOpciones(int idPregunta)
        {
            string? datos = HttpContext.Session.GetString(ClaveOrdenOpciones);

            if (string.IsNullOrWhiteSpace(datos))
            {
                return null;
            }

            try
            {
                var ordenes = JsonSerializer.Deserialize<Dictionary<int, int[]>>(datos);

                return ordenes != null && ordenes.TryGetValue(idPregunta, out int[]? orden)
                    ? orden
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool OrdenValido(int[]? orden, int cantidadOpciones)
        {
            return orden != null &&
                   orden.Length == cantidadOpciones &&
                   orden.OrderBy(i => i).SequenceEqual(Enumerable.Range(0, cantidadOpciones));
        }

        private static int ObtenerNivelPorId(int idPregunta)
        {
            if (idPregunta <= 103)
            {
                return 1;
            }

            return idPregunta <= 107 ? 2 : 3;
        }

        private static int ObtenerNivel(int posicion)
        {
            return posicion < 3 ? 1 : posicion < 7 ? 2 : 3;
        }

        private static string ObtenerNombreDificultad(int posicion)
        {
            return ObtenerNivel(posicion) switch
            {
                1 => "Inicial",
                2 => "Intermedia",
                _ => "Avanzada"
            };
        }
    }
}
