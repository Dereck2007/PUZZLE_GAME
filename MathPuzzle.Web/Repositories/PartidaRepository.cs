using MathPuzzle.Web.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MathPuzzle.Web.Repositories
{
    public class PartidaRepository
    {
        private readonly string _connectionString;

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public PartidaRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString(
                "MathPuzzleConnection"
            ) ?? throw new InvalidOperationException(
                "No se encontró la conexión MathPuzzleConnection."
            );
        }

        // =========================================
        // GUARDAR PARTIDA
        // =========================================

        public async Task GuardarPartidaAsync(
            string nombreJugador,
            int puntaje,
            int aciertos,
            int totalPreguntas,
            string tipoJuego,
            Guid? idUnicoPartida = null)
        {
            // Validar el nombre.
            if (string.IsNullOrWhiteSpace(nombreJugador))
            {
                throw new ArgumentException(
                    "El nombre del jugador es obligatorio."
                );
            }

            nombreJugador = nombreJugador.Trim();

            if (nombreJugador.Length > 40)
            {
                throw new ArgumentException(
                    "El nombre no puede superar 40 caracteres."
                );
            }

            if (string.IsNullOrWhiteSpace(tipoJuego))
            {
                throw new ArgumentException(
                    "El tipo de juego es obligatorio."
                );
            }

            tipoJuego = tipoJuego.Trim();

            // Validar los resultados.
            if (totalPreguntas < 1 || totalPreguntas > 10 ||
                aciertos < 0 || aciertos > totalPreguntas ||
                puntaje != aciertos * 100)
            {
                throw new ArgumentException(
                    "Los resultados de la partida no son válidos."
                );
            }

            // Crear un identificador si no se proporciona.
            Guid identificador =
                idUnicoPartida ?? Guid.NewGuid();

            if (identificador == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador de la partida no es válido."
                );
            }

            // Consulta SQL.
            string query = @"
                INSERT INTO dbo.Partidas
                (
                    IdUnicoPartida,
                    NombreJugador,
                    Puntaje,
                    Aciertos,
                    TotalPreguntas,
                    TipoJuego
                )
                VALUES
                (
                    @IdUnicoPartida,
                    @NombreJugador,
                    @Puntaje,
                    @Aciertos,
                    @TotalPreguntas,
                    @TipoJuego
                );";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))
            {
                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    // Identificador único.
                    command.Parameters.Add(
                        "@IdUnicoPartida",
                        SqlDbType.UniqueIdentifier
                    ).Value = identificador;

                    // Nombre del jugador.
                    command.Parameters.Add(
                        "@NombreJugador",
                        SqlDbType.NVarChar,
                        40
                    ).Value = nombreJugador;

                    // Puntuación.
                    command.Parameters.Add(
                        "@Puntaje",
                        SqlDbType.Int
                    ).Value = puntaje;

                    // Respuestas correctas.
                    command.Parameters.Add(
                        "@Aciertos",
                        SqlDbType.Int
                    ).Value = aciertos;

                    // Total de preguntas.
                    command.Parameters.Add(
                        "@TotalPreguntas",
                        SqlDbType.Int
                    ).Value = totalPreguntas;

                    command.Parameters.Add(
                        "@TipoJuego",
                        SqlDbType.NVarChar,
                        tipoJuego.Length
                    ).Value = tipoJuego;

                    await connection.OpenAsync();

                    try
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex) when (
                        (ex.Number == 2601 || ex.Number == 2627) &&
                        ex.Message.Contains(
                            "UX_Partidas_IdUnicoPartida",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    {
                        // Esta partida ya fue registrada.
                        // No se inserta una segunda vez.
                    }
                }
            }
        }

        // =========================================
        // OBTENER LOS 10 MEJORES RESULTADOS
        // =========================================

        public async Task<List<Partida>> ObtenerRankingAsync()
        {
            List<Partida> ranking = new List<Partida>();

            string query = @"
                SELECT TOP (10)
                    IdPartida,
                    NombreJugador,
                    Puntaje,
                    Aciertos,
                    TotalPreguntas,
                    FechaRegistro,
                    TipoJuego
                FROM dbo.Partidas
                ORDER BY
                    Puntaje DESC,
                    Aciertos DESC,
                    FechaRegistro ASC,
                    IdPartida ASC;";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))
            {
                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader =
                        await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Partida partida = new Partida
                            {
                                IdPartida = reader.GetInt32(0),

                                NombreJugador = reader.GetString(1),

                                Puntaje = reader.GetInt32(2),

                                Aciertos = reader.GetInt32(3),

                                TotalPreguntas = reader.GetInt32(4),

                                FechaRegistro = reader.GetDateTime(5),

                                TipoJuego = reader.IsDBNull(6)
                                    ? string.Empty
                                    : reader.GetString(6)
                            };

                            ranking.Add(partida);
                        }
                    }
                }
            }

            return ranking;
        }
    }
}