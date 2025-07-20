using ProyectoIdentity.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProyectoIdentity.Servicios
{
    public class RecomendadorProductos
    {
        private readonly OpenAIService _openAIService;
        private readonly ILogger<RecomendadorProductos> _logger;
        private List<Producto> _productos = new();
        private bool _inicializado = false;

        public RecomendadorProductos(
            OpenAIService openAIService,
            ILogger<RecomendadorProductos> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
        }

        public void Inicializar(List<Producto> productos)
        {
            try
            {
                _productos = productos ?? new List<Producto>();
                _inicializado = true;
                _logger.LogInformation("RecomendadorProductos inicializado con {Count} productos", _productos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inicializar RecomendadorProductos");
                _inicializado = false;
            }
        }

        public async Task<RecomendacionIA> ObtenerRecomendacion(string consulta, List<Producto> productos)
        {
            if (!_inicializado || !_productos.Any())
            {
                return new RecomendacionIA
                {
                    Respuesta = "Lo siento, no tengo productos disponibles en este momento.",
                    ProductoId = -1,
                    Puntuacion = 0
                };
            }

            try
            {
                _logger.LogInformation("Procesando consulta: {Consulta}", consulta);

                // Intentar con OpenAI primero
                var recomendacion = await IntentarConOpenAI(consulta, productos);

                // Si OpenAI no está disponible, usar método de fallback
                if (recomendacion.ProductoId == -1 && string.IsNullOrEmpty(recomendacion.Respuesta))
                {
                    _logger.LogInformation("OpenAI no disponible, usando método de respaldo...");
                    recomendacion = await FallbackRecomendacion(consulta);
                }

                return recomendacion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo recomendación");
                return await FallbackRecomendacion(consulta);
            }
        }

        private async Task<RecomendacionIA> IntentarConOpenAI(string consulta, List<Producto> productos)
        {
            try
            {
                var productosFiltrados = FiltrarProductosRelevantes(consulta, productos);

                var prompt = CrearPromptRecomendacion(consulta, productos);
                var respuestaIA = await _openAIService.GenerarRespuesta(prompt);

                if (!string.IsNullOrEmpty(respuestaIA))
                {
                    var recomendacion = ProcesarRespuestaIA(respuestaIA, consulta);
                    _logger.LogInformation("Recomendación obtenida via OpenAI - ProductoId: {ProductoId}", recomendacion.ProductoId);
                    return recomendacion;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error con OpenAI, usando método alternativo");
            }

            return await FallbackRecomendacion(consulta);
        }
        private List<Producto> FiltrarProductosRelevantes(string consulta, List<Producto> productos, int maxProductos = 10)
        {
            // Busca coincidencias en nombre, categoría o ingredientes (ignorando mayúsculas/minúsculas)
            var productosFiltrados = productos
                .Where(p =>
                    consulta.Contains(p.Nombre, StringComparison.OrdinalIgnoreCase) ||
                    consulta.Contains(p.Categoria, StringComparison.OrdinalIgnoreCase) ||
                    (p.Ingredientes != null && consulta.Contains(p.Ingredientes, StringComparison.OrdinalIgnoreCase))
                )
                .Take(maxProductos)
                .ToList();

            if (!productosFiltrados.Any())
            {
                // Si no hay coincidencias, toma los primeros N productos disponibles como fallback
                productosFiltrados = productos.Take(maxProductos).ToList();
            }

            return productosFiltrados;
        }

        private string CrearPromptRecomendacion(string consulta, List<Producto> productos)
        {
            var contexto = string.Join("\n\n", productos.Select(p =>
                $"ID: {p.Id}\n" + // Si tienes un campo Id
                $"Nombre: {p.Nombre}\n" +
                $"Categoría: {p.Categoria}\n" +
                $"Precio: {p.Precio}\n" +
                $"Descripción: {p.Descripcion}\n" +
                $"Ingredientes: {p.Ingredientes}\n" +
                //$"Información nutricional: {p.InfoNutricional}\n" +
                $"Alérgenos: {p.Alergenos}"
            ));


            return $@"
                Eres un asistente especializado en recomendaciones de productos para un restaurante/bar.

                PRODUCTOS DISPONIBLES: {contexto}    

                CONSULTA DEL CLIENTE: ""{consulta}""

                INSTRUCCIONES:
                1. Analiza cuidadosamente la consulta del cliente
                2. Revisa toda la información de los productos: nombre, descripción, categoría, ingredientes, alérgenos, información nutricional y disponibilidad.
                3. Responde en formato JSON con esta estructura exacta:
                {{
                    ""productoId"": [ID del producto recomendado o -1 si no hay coincidencia],
                    ""respuesta"": ""[Respuesta amigable y personalizada para el cliente]"",
                    ""puntuacion"": [número entre 0 y 100 indicando qué tan seguro estás de la recomendación]
                }}

                REGLAS:
                - Si el cliente pregunta por algo específico (como pizza, cerveza, cóctel), busca en esa categoría
                - Si menciona restricciones alimentarias (sin gluten, vegano, etc.), evita productos incompatibles
                - Si pregunta por recomendaciones generales, sugiere productos populares
                - La respuesta debe ser conversacional y explicar por qué recomiendas ese producto
                - Si no encuentras nada específico, usa productoId: -1 y da una respuesta general útil

                Responde SOLO con el JSON, sin texto adicional.";
        }

        private RecomendacionIA ProcesarRespuestaIA(string respuestaIA, string consultaOriginal)
        {
            try
            {
                // Limpiar la respuesta para extraer solo el JSON
                var jsonInicio = respuestaIA.IndexOf('{');
                var jsonFin = respuestaIA.LastIndexOf('}');

                if (jsonInicio >= 0 && jsonFin > jsonInicio)
                {
                    var jsonStr = respuestaIA.Substring(jsonInicio, jsonFin - jsonInicio + 1);
                    var respuestaJson = JsonSerializer.Deserialize<JsonElement>(jsonStr);

                    var productoId = respuestaJson.TryGetProperty("productoId", out var idProp) ? idProp.GetInt32() : -1;
                    var respuesta = respuestaJson.TryGetProperty("respuesta", out var respProp) ? respProp.GetString() : "";
                    var puntuacion = respuestaJson.TryGetProperty("puntuacion", out var puntProp) ? puntProp.GetDouble() : 0;

                    var producto = _productos.FirstOrDefault(p => p.Id == productoId);

                    return new RecomendacionIA
                    {
                        ProductoId = productoId,
                        Respuesta = respuesta ?? "Te recomiendo revisar nuestro menú completo.",
                        Puntuacion = puntuacion,
                        NombreProducto = producto?.Nombre ?? "",
                        Categoria = producto?.Categoria ?? "",
                        Precio = producto?.Precio ?? 0,
                        Descripcion = producto?.Descripcion,
                        Alergenos = producto?.Alergenos,
                        Ingredientes = producto?.Ingredientes,
                        Cantidad = producto?.Cantidad ?? 0
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error procesando respuesta IA, usando fallback");
            }

            return new RecomendacionIA
            {
                ProductoId = -1,
                Respuesta = "Basándome en tu consulta, te sugiero revisar nuestro menú. ¡Tenemos muchas opciones deliciosas!",
                Puntuacion = 50
            };
        }

        private async Task<RecomendacionIA> FallbackRecomendacion(string consulta)
        {
            // Método de respaldo usando lógica simple de palabras clave
            var consultaLower = consulta.ToLower();
            Producto? productoRecomendado = null;
            string respuesta = "";

            // Buscar por categorías principales
            if (consultaLower.Contains("pizza"))
            {
                productoRecomendado = _productos.FirstOrDefault(p =>
                    p.Categoria?.ToLower().Contains("pizza") == true && p.Cantidad > 0);
                respuesta = productoRecomendado != null
                    ? $"Te recomiendo nuestra {productoRecomendado.Nombre}. {productoRecomendado.Descripcion}"
                    : "Tenemos deliciosas pizzas disponibles. ¡Echa un vistazo a nuestro menú!";
            }
            else if (consultaLower.Contains("cerveza") || consultaLower.Contains("beer"))
            {
                productoRecomendado = _productos.FirstOrDefault(p =>
                    p.Categoria?.ToLower().Contains("cerveza") == true && p.Cantidad > 0);
                respuesta = productoRecomendado != null
                    ? $"Te sugiero nuestra {productoRecomendado.Nombre}. {productoRecomendado.Descripcion}"
                    : "Tenemos una gran variedad de cervezas. ¡Revisa nuestras opciones!";
            }
            else if (consultaLower.Contains("coctel") || consultaLower.Contains("cóctel") || consultaLower.Contains("cocktail"))
            {
                productoRecomendado = _productos.FirstOrDefault(p =>
                    p.Categoria?.ToLower().Contains("coctel") == true && p.Cantidad > 0);
                respuesta = productoRecomendado != null
                    ? $"Prueba nuestro {productoRecomendado.Nombre}. {productoRecomendado.Descripcion}"
                    : "Nuestros cócteles son especiales. ¡Te van a encantar!";
            }
            else if (consultaLower.Contains("sandwich") || consultaLower.Contains("sándwich"))
            {
                productoRecomendado = _productos.FirstOrDefault(p =>
                    p.Categoria?.ToLower().Contains("sándwich") == true && p.Cantidad > 0);
                respuesta = productoRecomendado != null
                    ? $"Te recomiendo nuestro {productoRecomendado.Nombre}. {productoRecomendado.Descripcion}"
                    : "Nuestros sándwiches son frescos y deliciosos. ¡Pruébalos!";
            }
            else if (consultaLower.Contains("sin gluten") || consultaLower.Contains("gluten free"))
            {
                productoRecomendado = _productos.FirstOrDefault(p =>
                    !ContieneGluten(p.Alergenos) && p.Cantidad > 0);
                respuesta = productoRecomendado != null
                    ? $"Perfecto para ti: {productoRecomendado.Nombre}. Es libre de gluten y delicioso."
                    : "Tenemos varias opciones sin gluten disponibles. ¡Consulta nuestro menú!";
            }
            else if (consultaLower.Contains("bebida") || consultaLower.Contains("tomar"))
            {
                productoRecomendado = _productos.FirstOrDefault(p =>
                    (p.Categoria?.ToLower().Contains("bebida") == true ||
                     p.Categoria?.ToLower().Contains("cerveza") == true ||
                     p.Categoria?.ToLower().Contains("coctel") == true) && p.Cantidad > 0);
                respuesta = productoRecomendado != null
                    ? $"Para beber te sugiero {productoRecomendado.Nombre}. {productoRecomendado.Descripcion}"
                    : "Tenemos una gran variedad de bebidas. ¡Revisa nuestro menú de bebidas!";
            }
            else if (consultaLower.Contains("comer") || consultaLower.Contains("almorzar") || consultaLower.Contains("comida"))
            {
                productoRecomendado = _productos.FirstOrDefault(p =>
                    (p.Categoria?.ToLower().Contains("pizza") == true ||
                     p.Categoria?.ToLower().Contains("sándwich") == true ||
                     p.Categoria?.ToLower().Contains("picada") == true) && p.Cantidad > 0);
                respuesta = productoRecomendado != null
                    ? $"Para comer te recomiendo {productoRecomendado.Nombre}. {productoRecomendado.Descripcion}"
                    : "Tenemos deliciosas opciones de comida. ¡Explora nuestro menú!";
            }
            else if (consultaLower.Contains("compartir") || consultaLower.Contains("grupo"))
            {
                productoRecomendado = _productos.FirstOrDefault(p =>
                    (p.Categoria?.ToLower().Contains("pizza") == true ||
                     p.Categoria?.ToLower().Contains("picada") == true) && p.Cantidad > 0);
                respuesta = productoRecomendado != null
                    ? $"Para compartir te sugiero {productoRecomendado.Nombre}. {productoRecomendado.Descripcion}"
                    : "Tenemos excelentes opciones para compartir. ¡Perfectas para grupos!";
            }
            else
            {
                // Recomendación general - tomar un producto aleatorio disponible
                var productosDisponibles = _productos.Where(p => p.Cantidad > 0).ToList();
                if (productosDisponibles.Any())
                {
                    var random = new Random();
                    productoRecomendado = productosDisponibles[random.Next(productosDisponibles.Count)];
                    respuesta = $"Te sugiero probar {productoRecomendado.Nombre}. {productoRecomendado.Descripcion}";
                }
                else
                {
                    respuesta = "¡Tenemos muchas opciones deliciosas! Te invito a explorar nuestro menú completo.";
                }
            }

            return new RecomendacionIA
            {
                ProductoId = productoRecomendado?.Id ?? -1,
                Respuesta = respuesta,
                Puntuacion = productoRecomendado != null ? 75 : 50,
                NombreProducto = productoRecomendado?.Nombre ?? "",
                Categoria = productoRecomendado?.Categoria ?? "",
                Precio = productoRecomendado?.Precio ?? 0,
                Descripcion = productoRecomendado?.Descripcion,
                Alergenos = productoRecomendado?.Alergenos,
                Ingredientes = productoRecomendado?.Ingredientes,
                Cantidad = productoRecomendado?.Cantidad ?? 0
            };

            int cantidadSolicitada = ExtraerCantidadSolicitada(consulta);
            var alergenosExcluidos = DetectarAlergenosExcluidos(consulta);

            var productosFiltrados = _productos
                .Where(p => p.Cantidad > 0)
                .Where(p => !ContieneAlgunoDeLosAlergenos(p.Alergenos, alergenosExcluidos))
                .ToList();

            var recomendaciones = productosFiltrados
                .Take(cantidadSolicitada)
                .Select(p => new RecomendacionIA
                {
                    ProductoId = p.Id,
                    Respuesta = $"Te recomiendo {p.Nombre}. {p.Descripcion}",
                    Puntuacion = 75,
                    NombreProducto = p.Nombre,
                    Categoria = p.Categoria,
                    Precio = p.Precio,
                    Descripcion = p.Descripcion,
                    Alergenos = p.Alergenos,
                    Ingredientes = p.Ingredientes,
                    Cantidad = p.Cantidad ?? 0
                })
                .ToList();

            if (!recomendaciones.Any())
            {
                recomendaciones.Add(new RecomendacionIA
                {
                    ProductoId = -1,
                    Respuesta = "No encontramos productos que se ajusten a tus criterios. ¡Explora nuestro menú para más opciones!",
                    Puntuacion = 50
                });
            }

            return recomendaciones.FirstOrDefault();
        }

        // Método para verificar si contiene Leche
        private static bool ContieneLeche(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Leche", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Lactosa
        private static bool ContieneLactosa(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Lactosa", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Pimienta
        private static bool ContienePimienta(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Pimienta", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Gluten
        private static bool ContieneGluten(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Gluten", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Sésamo
        private static bool ContieneSesamo(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Sésamo", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Conservantes
        private static bool ContieneConservantes(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Conservantes", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Nitratos
        private static bool ContieneNitratos(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Nitratos", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Trigo
        private static bool ContieneTrigo(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Trigo", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Mostaza
        private static bool ContieneMostaza(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Mostaza", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Derivados de cerdo
        private static bool ContieneDerivadosDeCerdo(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Derivados de cerdo", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Frutos secos
        private static bool ContieneFrutosSecos(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Frutos secos", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Quesos madurados
        private static bool ContieneQuesosMadurados(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Quesos madurados", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Leche de búfala
        private static bool ContieneLecheDeBufala(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Leche de búfala", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Cebada
        private static bool ContieneCebada(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Cebada", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Malta
        private static bool ContieneMalta(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Malta", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Sulfitos
        private static bool ContieneSulfitos(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Sulfitos", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Cítricos
        private static bool ContieneCitricos(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Cítricos", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Agave 100%
        private static bool ContieneAgave100(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Agave 100%", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Maracuyá
        private static bool ContieneMaracuya(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Maracuyá", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Frutos rojos
        private static bool ContieneFrutosRojos(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Frutos rojos", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Caramelo (colorante E-150d)
        private static bool ContieneCarameloE150d(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Caramelo (colorante E-150d)", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Menta
        private static bool ContieneMenta(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Menta", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Botánicos (Enebro)
        private static bool ContieneBotanicosEnebro(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Botánicos (Enebro)", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Hierbas
        private static bool ContieneHierbas(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Hierbas", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Huevo
        private static bool ContieneHuevo(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Huevo", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si contiene Ajo
        private static bool ContieneAjo(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Ajo", StringComparison.OrdinalIgnoreCase);
        }


        public bool EstaInicializado => _inicializado;
        public int CantidadProductos => _productos.Count;

        public static List<Producto> RecomendarProductos(List<Producto> productos, List<string> alergenosEvitar, int cantidadRequerida)
        {
            var recomendados = productos
                .Where(p => !ContieneAlgunoDeLosAlergenos(p.Alergenos, alergenosEvitar))
                .Take(cantidadRequerida)
                .ToList();

            return recomendados;
        }

        private static bool ContieneAlgunoDeLosAlergenos(string? alergenos, List<string> lista)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                return false;

            foreach (var alergenosBuscar in lista)
            {
                if (alergenos.Contains(alergenosBuscar, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private int ExtraerCantidadSolicitada(string consulta)
        {
            // Buscar números explícitos en la consulta (ej. “2 cervezas”)
            var matches = Regex.Matches(consulta, @"\d+");
            if (matches.Count > 0 && int.TryParse(matches[0].Value, out int resultado))
                return resultado;

            // Si no hay número explícito, devolver 1 como valor por defecto
            return 1;
        }

        private List<string> DetectarAlergenosExcluidos(string consulta)
        {
            var alergenosPosibles = new List<string>
    {
        "Leche", "Lactosa", "Pimienta", "Gluten", "Sésamo", "Conservantes", "Nitratos",
        "Trigo", "Mostaza", "Derivados de cerdo", "Frutos secos", "Quesos madurados", "Leche de búfala",
        "Cebada", "Malta", "Sulfitos", "Cítricos", "Agave 100%", "Maracuyá", "Frutos rojos",
        "Caramelo (colorante E-150d)", "Menta", "Botánicos (Enebro)", "Hierbas", "Huevo", "Ajo"
    };

            var excluidos = new List<string>();

            foreach (var alergeno in alergenosPosibles)
            {
                if (consulta.Contains($"sin {alergeno}", StringComparison.OrdinalIgnoreCase) ||
                    consulta.Contains($"sin {alergeno.ToLower()}", StringComparison.OrdinalIgnoreCase))
                {
                    excluidos.Add(alergeno);
                }
            }

            return excluidos;
        }

        private static string? AnalizarConsultaInvalida(string consulta)
        {
            if (string.IsNullOrWhiteSpace(consulta))
                return "Lo sentimos, no se recibió ninguna consulta válida.";

            string consultaLower = consulta.ToLower();

            // Lista de palabras clave que no aplican
            string[] palabrasProhibidas = { "gratis", "regalo", "promoción", "descuento total", "sin pagar" };

            foreach (var palabra in palabrasProhibidas)
            {
                if (consultaLower.Contains(palabra))
                {
                    return "Lo sentimos, Verace no maneja esa lógica de negocio.";
                }
            }

            // Si la consulta no contiene palabras relacionadas a productos/alergenos/precios/etc
            string[] palabrasRelacionadas = { "producto", "ingrediente", "alergia", "alérgeno", "precio", "cantidad", "recomendar", "contiene", "tiene" };

            bool contieneRelacionadas = palabrasRelacionadas.Any(p => consultaLower.Contains(p));
            if (!contieneRelacionadas)
            {
                return "Lo sentimos, esa consulta no parece estar relacionada con nuestros productos o servicios.";
            }

            return null; // Todo bien, puede continuar
        }


    }
}