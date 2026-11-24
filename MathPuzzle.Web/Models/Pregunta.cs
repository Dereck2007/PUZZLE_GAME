namespace MathPuzzle.Web.Models
{
    public class Pregunta
    {
        public int Id { get; set; }

        public string Enunciado { get; set; } = string.Empty;

        public List<string> Opciones { get; set; } = new List<string>();

        // Índice de la opción correcta: 0, 1, 2 o 3.
        public int IndiceRespuestaCorrecta { get; set; }
    }
}