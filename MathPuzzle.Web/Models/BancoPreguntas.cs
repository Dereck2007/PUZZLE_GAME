using System.Collections.Generic;

namespace MathPuzzle.Web.Models
{
    public class BancoPreguntas
    {
        public static List<Pregunta> ObtenerPreguntas()
        {
            return new List<Pregunta>
            {
                new Pregunta
                {
                    Id = 1,
                    Enunciado = "¿Cuánto es 7 × 8?",
                    Opciones = new List<string>
                    {
                        "48", "54", "56", "64"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 2,
                    Enunciado = "¿Cuál es el resultado de (18 + 12) ÷ 5?",
                    Opciones = new List<string>
                    {
                        "5", "6", "7", "8"
                    },
                    IndiceRespuestaCorrecta = 1
                },

                new Pregunta
                {
                    Id = 3,
                    Enunciado = "Si 3x + 5 = 20, ¿cuánto vale x?",
                    Opciones = new List<string>
                    {
                        "3", "4", "5", "6"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 4,
                    Enunciado = "¿Cuánto es 12 elevado al cuadrado?",
                    Opciones = new List<string>
                    {
                        "124", "132", "144", "164"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 5,
                    Enunciado = "¿Cuál es el 15% de 200?",
                    Opciones = new List<string>
                    {
                        "20", "25", "30", "35"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 6,
                    Enunciado = "¿Cuál es la raíz cuadrada de 169?",
                    Opciones = new List<string>
                    {
                        "11", "12", "13", "14"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 7,
                    Enunciado = "Un triángulo tiene ángulos de 50° y 60°. ¿Cuánto mide el tercero?",
                    Opciones = new List<string>
                    {
                        "60°", "65°", "70°", "75°"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 8,
                    Enunciado = "¿Cuánto es 2 elevado a la quinta potencia?",
                    Opciones = new List<string>
                    {
                        "16", "25", "32", "64"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 9,
                    Enunciado = "¿Cuánto es 3/4 + 1/4?",
                    Opciones = new List<string>
                    {
                        "1/2", "3/4", "1", "5/4"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 10,
                    Enunciado = "¿Cuánto es 9 × 9 - 10?",
                    Opciones = new List<string>
                    {
                        "69", "70", "71", "72"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 11,
                    Enunciado = "¿Qué número sigue en la secuencia 2, 4, 8, 16...?",
                    Opciones = new List<string>
                    {
                        "18", "24", "32", "36"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 12,
                    Enunciado = "Si 2(x - 3) = 14, ¿cuánto vale x?",
                    Opciones = new List<string>
                    {
                        "7", "8", "9", "10"
                    },
                    IndiceRespuestaCorrecta = 3
                },

                new Pregunta
                {
                    Id = 13,
                    Enunciado = "¿Cuál es el perímetro de un rectángulo de 8 cm de largo y 5 cm de ancho?",
                    Opciones = new List<string>
                    {
                        "13 cm", "20 cm", "26 cm", "40 cm"
                    },
                    IndiceRespuestaCorrecta = 2
                },

                new Pregunta
                {
                    Id = 14,
                    Enunciado = "¿Cuál es el promedio de 4, 6, 8 y 10?",
                    Opciones = new List<string>
                    {
                        "6", "7", "8", "9"
                    },
                    IndiceRespuestaCorrecta = 1
                },

                new Pregunta
                {
                    Id = 15,
                    Enunciado = "Al lanzar un dado de seis caras, ¿cuál es la probabilidad de obtener un número par?",
                    Opciones = new List<string>
                    {
                        "1/6", "1/3", "1/2", "2/3"
                    },
                    IndiceRespuestaCorrecta = 2
                }
            };
        }
    }
}