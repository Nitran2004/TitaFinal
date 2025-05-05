using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ProyectoIdentity.Controllers
{
    public class RecomendacionAvanzadoController : Controller
    {
        public class Plato
        {
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Calorias { get; set; }
            public string Categoria { get; set; }
            public string Ingredientes { get; set; }
            public double? Similitud { get; set; }
        }

        // Cargar los datos desde un archivo JSON o BD (este es el mismo conjunto de datos)
        private readonly List<Plato> _data = ObtenerDatosMenu();

        // Página principal
        public ActionResult Recomendacion()
        {
            // Obtener todas las categorías distintas
            var categorias = _data.Select(p => p.Categoria).Distinct().ToList();
            ViewBag.Categorias = categorias;

            // Pasar datos completos a la vista para procesamiento en cliente (opcional)
            ViewBag.MenuData = JsonConvert.SerializeObject(_data);

            return View();
        }

        // Método para manejar las recomendaciones
        [HttpPost]
        public ActionResult ObtenerRecomendaciones(string categoria, decimal presupuesto, string ingredientes)
        {
            try
            {
                // Filtrado inicial por precio y categoría
                var filtrado = _data.Where(item => item.Precio <= presupuesto).ToList();

                if (!string.IsNullOrEmpty(categoria) && categoria != "Cualquiera")
                {
                    filtrado = filtrado.Where(item => item.Categoria == categoria).ToList();
                }

                // Procesamiento avanzado de ingredientes
                if (!string.IsNullOrEmpty(ingredientes))
                {
                    filtrado = ProcesarIngredientes(filtrado, ingredientes);
                }

                // Tomar los 5 mejores resultados
                var recomendaciones = filtrado.Take(5).ToList();

                return Json(new { success = true, data = recomendaciones });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Método para procesar ingredientes con TF-IDF simplificado
        private List<Plato> ProcesarIngredientes(List<Plato> platos, string ingredientes)
        {
            var ingredientesLower = ingredientes.ToLower();

            // Casos especiales
            if (ingredientesLower.Contains("sin carne"))
            {
                return platos.Where(item =>
                    !item.Ingredientes.ToLower().Contains("carne") &&
                    !item.Ingredientes.ToLower().Contains("jamón") &&
                    !item.Ingredientes.ToLower().Contains("pepperoni")
                ).ToList();
            }
            else if (ingredientesLower.Contains("con carne"))
            {
                return platos.Where(item =>
                    item.Ingredientes.ToLower().Contains("carne") ||
                    item.Ingredientes.ToLower().Contains("jamón") ||
                    item.Ingredientes.ToLower().Contains("pepperoni")
                ).ToList();
            }

            // Tokenizar los ingredientes de la consulta
            var terminosConsulta = Tokenizar(ingredientesLower);

            // Si no hay términos significativos, devolver los platos sin cambios
            if (!terminosConsulta.Any())
            {
                return platos;
            }

            // Calcular vectores TF-IDF simplificados
            var (vectoresPlatos, todosTerminos) = CrearVectoresTFIDF(platos);

            // Crear vector de consulta
            var vectorConsulta = new double[todosTerminos.Count];
            for (int i = 0; i < todosTerminos.Count; i++)
            {
                vectorConsulta[i] = terminosConsulta.Contains(todosTerminos[i]) ? 1 : 0;
            }

            // Calcular similitud del coseno para cada plato
            for (int i = 0; i < platos.Count; i++)
            {
                if (i < vectoresPlatos.Count)
                {
                    platos[i].Similitud = CalcularSimilitudCoseno(vectorConsulta, vectoresPlatos[i]);
                }
                else
                {
                    platos[i].Similitud = 0;
                }
            }

            // Ordenar por similitud descendente
            return platos.OrderByDescending(p => p.Similitud).ToList();
        }

        // Tokenizar texto
        private List<string> Tokenizar(string texto)
        {
            // Eliminar caracteres especiales y dividir por espacios o comas
            return Regex.Replace(texto, "[^a-záéíóúñ\\s,]", "")
                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 2) // Ignorar palabras muy cortas
                .ToList();
        }

        // Método simplificado para crear vectores TF-IDF
        private (List<double[]>, List<string>) CrearVectoresTFIDF(List<Plato> platos)
        {
            // Procesar ingredientes y recopilar términos únicos
            var documentosProcesados = new List<List<string>>();
            var todosTerminos = new HashSet<string>();

            foreach (var plato in platos)
            {
                var terminos = Tokenizar(plato.Ingredientes.ToLower());
                documentosProcesados.Add(terminos);
                foreach (var termino in terminos)
                {
                    todosTerminos.Add(termino);
                }
            }

            var terminosLista = todosTerminos.ToList();
            var idf = new Dictionary<string, double>();

            // Calcular IDF (Inverse Document Frequency)
            foreach (var termino in terminosLista)
            {
                int documentosConTermino = 0;
                foreach (var doc in documentosProcesados)
                {
                    if (doc.Contains(termino))
                    {
                        documentosConTermino++;
                    }
                }

                idf[termino] = Math.Log((double)platos.Count / (1 + documentosConTermino));
            }

            // Calcular vectores TF-IDF para cada plato
            var vectoresTFIDF = new List<double[]>();

            foreach (var doc in documentosProcesados)
            {
                var vector = new double[terminosLista.Count];
                var frecuencias = new Dictionary<string, int>();

                // Calcular frecuencias de términos
                foreach (var termino in doc)
                {
                    if (!frecuencias.ContainsKey(termino))
                    {
                        frecuencias[termino] = 0;
                    }
                    frecuencias[termino]++;
                }

                // Calcular TF-IDF para cada término
                for (int i = 0; i < terminosLista.Count; i++)
                {
                    var termino = terminosLista[i];
                    double tf = frecuencias.ContainsKey(termino) ? frecuencias[termino] : 0;
                    vector[i] = tf * idf[termino];
                }

                vectoresTFIDF.Add(vector);
            }

            return (vectoresTFIDF, terminosLista);
        }

        // Calcular similitud de coseno entre dos vectores
        private double CalcularSimilitudCoseno(double[] vectorA, double[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
            {
                throw new ArgumentException("Los vectores deben tener la misma longitud");
            }

            double productoEscalar = 0;
            double normVectorA = 0;
            double normVectorB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                productoEscalar += vectorA[i] * vectorB[i];
                normVectorA += vectorA[i] * vectorA[i];
                normVectorB += vectorB[i] * vectorB[i];
            }

            normVectorA = Math.Sqrt(normVectorA);
            normVectorB = Math.Sqrt(normVectorB);

            // Evitar división por cero
            if (normVectorA == 0 || normVectorB == 0)
            {
                return 0;
            }

            return productoEscalar / (normVectorA * normVectorB);
        }

        // Método para obtener datos de muestra
        private static List<Plato> ObtenerDatosMenu()
        {
            // Aquí se podrían cargar los datos desde un JSON o base de datos
            // Por ahora, definimos algunos platos de ejemplo
            return new List<Plato>
            {
                new Plato
                {
                    Nombre = "Pizza Margarita",
                    Precio = 8.5M,
                    Calorias = 850,
                    Categoria = "Pizza",
                    Ingredientes = "masa, tomate, mozzarella, albahaca, aceite de oliva"
                },
                new Plato
                {
                    Nombre = "Pizza Pepperoni",
                    Precio = 10.0M,
                    Calorias = 950,
                    Categoria = "Pizza",
                    Ingredientes = "masa, tomate, mozzarella, pepperoni, orégano"
                },
                new Plato
                {
                    Nombre = "Ensalada César",
                    Precio = 7.5M,
                    Calorias = 450,
                    Categoria = "Ensalada",
                    Ingredientes = "lechuga, pollo, queso parmesano, crutones, salsa césar"
                },
                new Plato
                {
                    Nombre = "Pasta Carbonara",
                    Precio = 9.5M,
                    Calorias = 750,
                    Categoria = "Pasta",
                    Ingredientes = "espagueti, huevo, panceta, queso parmesano, pimienta"
                },
                new Plato
                {
                    Nombre = "Pasta al Pesto",
                    Precio = 9.0M,
                    Calorias = 700,
                    Categoria = "Pasta",
                    Ingredientes = "espagueti, albahaca, ajo, piñones, queso parmesano, aceite de oliva"
                },
                new Plato
                {
                    Nombre = "Hamburguesa Clásica",
                    Precio = 11.0M,
                    Calorias = 950,
                    Categoria = "Hamburguesa",
                    Ingredientes = "pan, carne de ternera, queso cheddar, lechuga, tomate, cebolla, ketchup, mostaza"
                },
                new Plato
                {
                    Nombre = "Hamburguesa Vegetariana",
                    Precio = 10.5M,
                    Calorias = 750,
                    Categoria = "Hamburguesa",
                    Ingredientes = "pan, hamburguesa de garbanzos, lechuga, tomate, cebolla caramelizada, aguacate"
                },
                new Plato
                {
                    Nombre = "Paella de Mariscos",
                    Precio = 14.5M,
                    Calorias = 850,
                    Categoria = "Arroces",
                    Ingredientes = "arroz, camarones, mejillones, calamares, pimientos, azafrán, caldo de pescado"
                },
                new Plato
                {
                    Nombre = "Risotto de Setas",
                    Precio = 12.0M,
                    Calorias = 700,
                    Categoria = "Arroces",
                    Ingredientes = "arroz arborio, champiñones, cebolla, ajo, vino blanco, queso parmesano"
                },
                new Plato
                {
                    Nombre = "Sushi Variado",
                    Precio = 15.0M,
                    Calorias = 500,
                    Categoria = "Sushi",
                    Ingredientes = "arroz, alga nori, salmón, atún, aguacate, pepino, wasabi, salsa de soja"
                },
                new Plato
                {
                    Nombre = "Tacos de Carne Asada",
                    Precio = 9.0M,
                    Calorias = 650,
                    Categoria = "Mexicana",
                    Ingredientes = "tortilla de maíz, carne de res, cebolla, cilantro, limón, salsa picante"
                },
                new Plato
                {
                    Nombre = "Tiramisú",
                    Precio = 6.5M,
                    Calorias = 450,
                    Categoria = "Postre",
                    Ingredientes = "bizcochos, café, mascarpone, huevos, azúcar, cacao en polvo"
                },
                new Plato
                {
                    Nombre = "Tarta de Manzana",
                    Precio = 5.5M,
                    Calorias = 400,
                    Categoria = "Postre",
                    Ingredientes = "masa, manzanas, azúcar, canela, mantequilla"
                },
                new Plato
                {
                    Nombre = "Gazpacho",
                    Precio = 6.0M,
                    Calorias = 250,
                    Categoria = "Sopa",
                    Ingredientes = "tomate, pepino, pimiento, ajo, aceite de oliva, vinagre, pan"
                },
                new Plato
                {
                    Nombre = "Sopa de Cebolla",
                    Precio = 7.0M,
                    Calorias = 350,
                    Categoria = "Sopa",
                    Ingredientes = "cebolla, caldo de carne, pan, queso gruyère, vino blanco"
                }
            };
        }
    }
}