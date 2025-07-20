using ProyectoIdentity.Models;
using System.Text.Json;

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

        public async Task<RecomendacionIA> ObtenerRecomendacion(string consulta)
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
                var recomendacion = await IntentarConOpenAI(consulta);

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

        private async Task<RecomendacionIA> IntentarConOpenAI(string consulta)
        {
            try
            {
                var prompt = CrearPromptRecomendacion(consulta);
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

        private string CrearPromptRecomendacion(string consulta)
        {
            var productosJson = JsonSerializer.Serialize(_productos.Select(p => new
            {
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.Categoria,
                p.Precio,
                p.Alergenos,
                p.Ingredientes,
                p.Cantidad
            }), new JsonSerializerOptions { WriteIndented = true });

            return $@"
Eres un asistente especializado en recomendaciones de productos para un restaurante/bar.

PRODUCTOS DISPONIBLES:
{productosJson}

CONSULTA DEL CLIENTE: ""{consulta}""

INSTRUCCIONES:
1. Analiza la consulta del cliente
2. Encuentra el producto más relevante de la lista
3. Responde en formato JSON con esta estructura exacta:
{{
    ""productoId"": [ID del producto recomendado o -1 si no hay coincidencia],
    ""respuesta"": ""[Respuesta amigable y personalizada para el cliente]"",
    ""puntuacion"": [número entre 0 y 100 indicando qué tan seguro estás de la recomendación]
}}

REGLAS:
- Si el cliente pregunta por algo específico (como pizza, cerveza, cóctel), busca en esa categoría
- Si menciona restricciones alimentarias (sin gluten, vegano, etc.), considera los alérgenos
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
        }

        private static bool ContieneGluten(string? alergenos)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Contains("Gluten", StringComparison.OrdinalIgnoreCase);
        }

        public bool EstaInicializado => _inicializado;
        public int CantidadProductos => _productos.Count;
    }
}