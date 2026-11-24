using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MathPuzzle.Web.Middleware
{
    public class BloqueoSesionMiddleware
    {
        private readonly RequestDelegate _next;

        // Diccionario de bloqueos por sesión.
        private static readonly Dictionary<string, EstadoBloqueo>
            Bloqueos = new();

        // Protege el acceso al diccionario.
        private static readonly object Sincronizacion = new();

        // =========================================
        // CLASE PARA CONTROLAR CADA BLOQUEO
        // =========================================

        private class EstadoBloqueo
        {
            public SemaphoreSlim Semaforo { get; } =
                new SemaphoreSlim(1, 1);

            // Incluye solicitudes en ejecución y en espera.
            public int SolicitudesActivas { get; set; }
        }

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public BloqueoSesionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // =========================================
        // CONTROLAR SOLICITUDES DEL JUEGO
        // =========================================

        public async Task InvokeAsync(HttpContext context)
        {
            // Aplicar el bloqueo solamente a los módulos de juego.
            bool esJuego = context.Request.Path.StartsWithSegments(
                "/Juego",
                StringComparison.OrdinalIgnoreCase
            ) || context.Request.Path.StartsWithSegments(
                "/Secuencias",
                StringComparison.OrdinalIgnoreCase
            );

            if (!esJuego)
            {
                await _next(context);
                return;
            }

            // Obtener el identificador de sesión.
            string idSesion = context.Session.Id;

            EstadoBloqueo estado;

            // =====================================
            // OBTENER O CREAR EL BLOQUEO
            // =====================================

            lock (Sincronizacion)
            {
                if (!Bloqueos.TryGetValue(
                    idSesion, out var existente))
                {
                    existente = new EstadoBloqueo();

                    Bloqueos.Add(idSesion, existente);
                }

                estado = existente;

                // Registrar esta solicitud.
                estado.SolicitudesActivas++;
            }

            bool bloqueoAdquirido = false;

            try
            {
                // Esperar hasta que la sesión esté libre.
                await estado.Semaforo.WaitAsync(
                    context.RequestAborted);

                bloqueoAdquirido = true;

                // Cargar la sesión actualizada.
                await context.Session.LoadAsync(
                    context.RequestAborted);

                // Procesar la solicitud del jugador.
                await _next(context);

                // Guardar los cambios antes de
                // permitir la siguiente solicitud.
                await context.Session.CommitAsync(
                    context.RequestAborted);
            }
            finally
            {
                // =================================
                // LIBERAR EL BLOQUEO
                // =================================

                if (bloqueoAdquirido)
                {
                    estado.Semaforo.Release();
                }

                // =================================
                // LIMPIAR BLOQUEOS SIN USO
                // =================================

                lock (Sincronizacion)
                {
                    estado.SolicitudesActivas--;

                    // Eliminar cuando no existan
                    // solicitudes ejecutándose
                    // ni esperando.
                    if (estado.SolicitudesActivas == 0)
                    {
                        Bloqueos.Remove(idSesion);

                        estado.Semaforo.Dispose();
                    }
                }
            }
        }
    }
}