
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
    public class JuegoController : Controller
    {
        private const int CantidadPreguntas = 10;
        private const int TiempoPorPregunta = 15;
        private const int PuntosPorRespuesta = 100;

        private readonly PartidaRepository _partidaRepository;

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public JuegoController(IConfiguration configuration)
        {
            _partidaRepository =
                new PartidaRepository(configuration);
        }

        // =========================================
        // PANTALLA DE INICIO
        // =========================================

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // =========================================
        // INICIAR UNA NUEVA PARTIDA
        // =========================================

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
                ViewBag.Error =
                    "El nombre no puede superar 40 caracteres.";

                return View("Index");
            }

            // Obtener las preguntas del banco.
            var preguntas = BancoPreguntas.ObtenerPreguntas();

            // Seleccionar preguntas aleatorias.
            var seleccionadas = preguntas
                .OrderBy(p => Random.Shared.Next())
                .Take(CantidadPreguntas)
                .ToList();

            // Crear los órdenes de las respuestas.
            Dictionary<int, int[]> ordenes =
                new Dictionary<int, int[]>();

            foreach (var pregunta in seleccionadas)
            {
                int[] orden = Enumerable.Range(
                    0,
                    pregunta.Opciones.Count
                ).ToArray();

                // Mezclar las opciones.
                for (int i = orden.Length - 1; i > 0; i--)
                {
                    int j = Random.Shared.Next(i + 1);

                    (orden[i], orden[j]) =
                        (orden[j], orden[i]);
                }

                ordenes[pregunta.Id] = orden;
            }

            // =====================================
            // CREAR IDENTIFICADOR ÚNICO
            // =====================================

            Guid idUnicoPartida = Guid.NewGuid();

            HttpContext.Session.SetString(
                "IdUnicoPartida",
                idUnicoPartida.ToString("D")
            );

            // =====================================
            // GUARDAR DATOS DE LA PARTIDA
            // =====================================

            HttpContext.Session.SetString(
                "Jugador",
                nombre
            );

            HttpContext.Session.SetString(
                "PreguntasIds",
                string.Join(
                    ",",
                    seleccionadas.Select(p => p.Id)
                )
            );

            HttpContext.Session.SetString(
                "OrdenOpciones",
                JsonSerializer.Serialize(ordenes)
            );

            // Inicializar puntuación y progreso.
            HttpContext.Session.SetInt32("Posicion", 0);
            HttpContext.Session.SetInt32("Puntaje", 0);
            HttpContext.Session.SetInt32("Aciertos", 0);

            // Reiniciar los datos de la partida anterior.
            HttpContext.Session.Remove("InicioPregunta");
            HttpContext.Session.Remove("PartidaGuardada");

            return RedirectToAction("Pregunta");
        }

        // =========================================
        // MOSTRAR PREGUNTA ACTUAL
        // =========================================

        [HttpGet]
        public IActionResult Pregunta()
        {
            int[] ids = ObtenerPreguntasSesion();

            if (ids.Length == 0)
            {
                return RedirectToAction("Index");
            }

            int posicion =
                HttpContext.Session.GetInt32("Posicion") ?? 0;

            // Comprobar si terminó la partida.
            if (posicion >= ids.Length)
            {
                return RedirectToAction("Resultado");
            }

            // Obtener la pregunta original.
            var pregunta = BancoPreguntas
                .ObtenerPreguntas()
                .FirstOrDefault(p => p.Id == ids[posicion]);

            if (pregunta == null)
            {
                return RedirectToAction("Index");
            }

            // Recuperar el orden de las respuestas.
            int[]? orden = ObtenerOrdenOpciones(pregunta.Id);

            if (orden == null ||
                orden.Length != pregunta.Opciones.Count)
            {
                return RedirectToAction("Index");
            }

            // Crear pregunta con respuestas mezcladas.
            Pregunta preguntaVista = new Pregunta
            {
                Id = pregunta.Id,

                Enunciado = pregunta.Enunciado,

                Opciones = orden
                    .Select(i => pregunta.Opciones[i])
                    .ToList(),

                IndiceRespuestaCorrecta = Array.IndexOf(
                    orden,
                    pregunta.IndiceRespuestaCorrecta
                )
            };

            // =====================================
            // CONTROL DEL TEMPORIZADOR
            // =====================================

            if (HttpContext.Session.GetString(
                "InicioPregunta") == null)
            {
                long inicio = DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds();

                HttpContext.Session.SetString(
                    "InicioPregunta",
                    inicio.ToString()
                );
            }

            long.TryParse(
                HttpContext.Session.GetString("InicioPregunta"),
                out long inicioPregunta
            );

            long transcurrido =
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                - inicioPregunta;

            // Enviar datos a la vista.
            ViewBag.NumeroPregunta = posicion + 1;
            ViewBag.TotalPreguntas = ids.Length;

            ViewBag.Jugador =
                HttpContext.Session.GetString("Jugador");

            ViewBag.Puntaje =
                HttpContext.Session.GetInt32("Puntaje") ?? 0;

            ViewBag.TiempoRestante = Math.Max(
                0,
                (TiempoPorPregunta * 1000 - transcurrido)
                / 1000.0
            );

            return View(preguntaVista);
        }

        // =========================================
        // VALIDAR RESPUESTA
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Responder(
            int idPregunta,
            int indiceRespuesta)
        {
            int[] ids = ObtenerPreguntasSesion();

            if (ids.Length == 0)
            {
                return RedirectToAction("Index");
            }

            int posicion =
                HttpContext.Session.GetInt32("Posicion") ?? 0;

            if (posicion >= ids.Length)
            {
                return RedirectToAction("Resultado");
            }

            // Verificar la pregunta actual.
            if (ids[posicion] != idPregunta)
            {
                return RedirectToAction("Pregunta");
            }

            var pregunta = BancoPreguntas
                .ObtenerPreguntas()
                .FirstOrDefault(p => p.Id == idPregunta);

            if (pregunta == null)
            {
                return RedirectToAction("Index");
            }

            // Recuperar opciones mezcladas.
            int[]? orden = ObtenerOrdenOpciones(idPregunta);

            if (orden == null ||
                orden.Length != pregunta.Opciones.Count)
            {
                return RedirectToAction("Index");
            }

            // Obtener el índice correcto.
            int indiceCorrecto = Array.IndexOf(
                orden,
                pregunta.IndiceRespuestaCorrecta
            );

            // =====================================
            // VERIFICAR EL TIEMPO
            // =====================================

            bool tiempoValido = false;

            string? inicioGuardado =
                HttpContext.Session.GetString("InicioPregunta");

            if (long.TryParse(inicioGuardado, out long inicio))
            {
                long transcurrido =
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    - inicio;

                tiempoValido =
                    transcurrido >= 0 &&
                    transcurrido <= TiempoPorPregunta * 1000;
            }

            // =====================================
            // SUMAR PUNTOS
            // =====================================

            if (tiempoValido &&
                indiceRespuesta >= 0 &&
                indiceRespuesta < orden.Length &&
                indiceRespuesta == indiceCorrecto)
            {
                int puntaje =
                    HttpContext.Session.GetInt32("Puntaje") ?? 0;

                int aciertos =
                    HttpContext.Session.GetInt32("Aciertos") ?? 0;

                HttpContext.Session.SetInt32(
                    "Puntaje",
                    puntaje + PuntosPorRespuesta
                );

                HttpContext.Session.SetInt32(
                    "Aciertos",
                    aciertos + 1
                );
            }

            // Avanzar a la siguiente pregunta.
            HttpContext.Session.SetInt32(
                "Posicion",
                posicion + 1
            );

            // Reiniciar el tiempo.
            HttpContext.Session.Remove("InicioPregunta");

            return RedirectToAction("Pregunta");
        }

        // =========================================
        // MOSTRAR Y GUARDAR RESULTADOS
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Resultado()
        {
            int[] ids = ObtenerPreguntasSesion();

            if (ids.Length == 0)
            {
                return RedirectToAction("Index");
            }

            int posicion =
                HttpContext.Session.GetInt32("Posicion") ?? 0;

            // No mostrar resultados antes de terminar.
            if (posicion < ids.Length)
            {
                return RedirectToAction("Pregunta");
            }

            // Obtener información del jugador.
            string jugador =
                HttpContext.Session.GetString("Jugador")
                ?? "Jugador";

            int puntaje =
                HttpContext.Session.GetInt32("Puntaje") ?? 0;

            int aciertos =
                HttpContext.Session.GetInt32("Aciertos") ?? 0;

            int totalPreguntas = ids.Length;

            // =====================================
            // OBTENER IDENTIFICADOR DE LA PARTIDA
            // =====================================

            string? identificadorGuardado =
                HttpContext.Session.GetString("IdUnicoPartida");

            if (!Guid.TryParse(
                identificadorGuardado,
                out Guid idUnicoPartida
            ) || idUnicoPartida == Guid.Empty)
            {
                // Si falta el identificador, iniciar
                // una nueva partida correctamente.
                return RedirectToAction("Index");
            }

            // =====================================
            // GUARDAR RESULTADO EN SQL SERVER
            // =====================================

            int partidaGuardada =
                HttpContext.Session.GetInt32("PartidaGuardada")
                ?? 0;

            if (partidaGuardada == 0)
            {
                await _partidaRepository.GuardarPartidaAsync(
                    jugador,
                    puntaje,
                    aciertos,
                    totalPreguntas,
                    "Math Puzzle",
                    idUnicoPartida
                );

                // Marcar la partida como guardada.
                HttpContext.Session.SetInt32(
                    "PartidaGuardada",
                    1
                );
            }

            // =====================================
            // ENVIAR RESULTADOS A LA VISTA
            // =====================================

            ViewBag.Jugador = jugador;
            ViewBag.Puntaje = puntaje;
            ViewBag.Aciertos = aciertos;
            ViewBag.TotalPreguntas = totalPreguntas;

            return View();
        }

        // =========================================
        // OBTENER PREGUNTAS DE LA SESIÓN
        // =========================================

        private int[] ObtenerPreguntasSesion()
        {
            string? datos =
                HttpContext.Session.GetString("PreguntasIds");

            if (string.IsNullOrWhiteSpace(datos))
            {
                return Array.Empty<int>();
            }

            return datos.Split(',')
                .Select(int.Parse)
                .ToArray();
        }

        // =========================================
        // OBTENER ORDEN DE LAS RESPUESTAS
        // =========================================

        private int[]? ObtenerOrdenOpciones(int idPregunta)
        {
            string? datos =
                HttpContext.Session.GetString("OrdenOpciones");

            if (string.IsNullOrWhiteSpace(datos))
            {
                return null;
            }

            var ordenes = JsonSerializer.Deserialize<
                Dictionary<int, int[]>
            >(datos);

            if (ordenes != null &&
                ordenes.TryGetValue(
                    idPregunta,
                    out int[]? orden
                ))
            {
                return orden;
            }

            return null;
        }
    }
}