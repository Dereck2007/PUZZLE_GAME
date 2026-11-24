using System;

namespace MathPuzzle.Web.Models
{
    public class Partida
    {
        public int IdPartida { get; set; }

        public string NombreJugador { get; set; } = string.Empty;

        public int Puntaje { get; set; }

        public int Aciertos { get; set; }

        public int TotalPreguntas { get; set; }

        public string TipoJuego { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }
    }
}