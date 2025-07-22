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

                // Solo usar OpenAI, sin fallbacks
                var recomendacion = await ProcesarConOpenAI(consulta, productos);
                return recomendacion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo recomendación");
                return new RecomendacionIA
                {
                    Respuesta = "Lo siento, hubo un error procesando tu consulta. Por favor intenta de nuevo.",
                    ProductoId = -1,
                    Puntuacion = 0
                };
            }
        }

        private async Task<RecomendacionIA> ProcesarConOpenAI(string consulta, List<Producto> productos)
        {
            try
            {
                // Verificar si OpenAI está disponible
                var conexionValida = await _openAIService.VerificarConexion();
                if (!conexionValida)
                {
                    _logger.LogWarning("OpenAI no está disponible");
                    return new RecomendacionIA
                    {
                        Respuesta = "El servicio de recomendaciones no está disponible en este momento. Por favor, intenta más tarde.",
                        ProductoId = -1,
                        Puntuacion = 0
                    };
                }

                var productosFiltrados = FiltrarProductosRelevantes(consulta, productos);
                var prompt = CrearPromptRecomendacion(consulta, productos);

                _logger.LogInformation("Enviando prompt a OpenAI para consulta: {Consulta}", consulta);

                var respuestaIA = await _openAIService.GenerarRespuesta(prompt);
                _logger.LogInformation($"\nRESPUESTA JIMMY:\n{respuestaIA}\n");

                if (!string.IsNullOrEmpty(respuestaIA))
                {
                    _logger.LogInformation("Respuesta recibida de OpenAI: {Respuesta}", respuestaIA);

                    var recomendacion = ProcesarRespuestaIA(respuestaIA, consulta);
                    _logger.LogInformation("Recomendación procesada - ProductoId: {ProductoId}", recomendacion.ProductoId);
                    return recomendacion;
                }
                else
                {
                    _logger.LogWarning("OpenAI retornó respuesta vacía");
                    return new RecomendacionIA
                    {
                        Respuesta = "No pude generar una recomendación en este momento. Por favor, reformula tu pregunta.",
                        ProductoId = -1,
                        Puntuacion = 0
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando con OpenAI: {Mensaje}", ex.Message);
                return new RecomendacionIA
                {
                    Respuesta = "Hubo un error técnico. Por favor, intenta de nuevo en unos momentos.",
                    ProductoId = -1,
                    Puntuacion = 0
                };
            }
        }

        private List<Producto> FiltrarProductosRelevantes(string consulta, List<Producto> productos, int maxProductos = 15)
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
                // Si no hay coincidencias, toma los primeros N productos disponibles
                productosFiltrados = productos
                    .Where(p => p.Cantidad > 0)
                    .Take(maxProductos)
                    .ToList();
            }

            return productosFiltrados;
        }

        private string CrearPromptRecomendacion(string consulta, List<Producto> productos)
        {
            var contexto = string.Join("\n\n", productos.Select(p =>
                $"ID: {p.Id}\n" +
                $"Nombre: {p.Nombre}\n" +
                $"Categoría: {p.Categoria}\n" +
                $"Precio: {p.Precio}\n" +
                $"Descripción: {p.Descripcion}\n" +
                $"Ingredientes: {p.Ingredientes}\n" +
                $"Alérgenos: {p.Alergenos}"
            ));

            return $@"
                Eres un asistente especializado en recomendaciones de productos para un restaurante/bar en Ecuador.

                PRODUCTOS DISPONIBLES:
                {contexto}    

                CONSULTA DEL CLIENTE: ""{consulta}""

                INSTRUCCIONES:
                1. La consulta del cliente puede incluir jerga ecuatoriana, expresiones locales o dialecto del Ecuador. Interprétala de manera natural y correcta.
                2. Analiza cuidadosamente la consulta del cliente
                3. Revisa toda la información de los productos: nombre, descripción, categoría, ingredientes, alérgenos.
                4. Si el cliente usa expresiones como ""sin [ingrediente]"", ""no quiero [alérgeno]"", ""no me gusta [ingrediente]"", ""me hace daño [algo]"", ""me cae mal [algo]"" o similares, interpreta eso como una **restricción alimentaria** y excluye productos que contengan ese ingrediente o alérgeno.
                5. Si el cliente menciona de forma general ""quiero comida"", ""algo para comer"", ""tengo hambre"", ""qué hay para comer"", interpreta eso como una búsqueda general dentro de las categorías: **Pizza**, **Sánduches**, **Picadas** y **Promo**.
                6. Si el cliente dice frases como ""algo para comer y beber"", interpreta eso como una solicitud de productos de la categoría **Promo**, que incluye combinaciones de comida y bebida.

                

                IMPORTANTE: Si no puedes generar un JSON válido por cualquier razón, responde SOLO con texto conversacional amigable explicando tu recomendación.

                REGLAS:
                - Si el cliente pregunta por algo específico (como pizza, cerveza, cóctel), busca en esa categoría.
                - Si menciona restricciones alimentarias (sin gluten, vegano, etc.), evita productos incompatibles.
                - Si el cliente usa expresiones como ""sin [ingrediente]"", ""no quiero [alérgeno]"", interpreta eso como una restricción alimentaria.
                - Si pregunta por recomendaciones generales, sugiere productos populares o que destaquen.
                - La respuesta debe ser conversacional y explicar por qué recomiendas ese producto.
                - Si no encuentras ningún producto que coincida exactamente, sugiere alternativas similares.
                - Si el precio del producto es de hasta $5, considérese barato.
                - Si el precio es más de $8, considérese caro.
                - Siempre sé útil y amigable, incluso si no hay una coincidencia perfecta.

                Responde de la manera que mejor puedas ayudar al cliente:";
        }

        private RecomendacionIA ProcesarRespuestaIA(string respuestaIA, string consultaOriginal)
        {
            try
            {
                // Intentar extraer y parsear el JSON
                var jsonInicio = respuestaIA.IndexOf('{');
                var jsonFin = respuestaIA.LastIndexOf('}');

                if (jsonInicio >= 0 && jsonFin > jsonInicio)
                {
                    var jsonStr = respuestaIA.Substring(jsonInicio, jsonFin - jsonInicio + 1);

                    try
                    {
                        var respuestaJson = JsonSerializer.Deserialize<JsonElement>(jsonStr);

                        var productoId = respuestaJson.TryGetProperty("productoId", out var idProp) ? idProp.GetInt32() : -1;
                        var respuesta = respuestaJson.TryGetProperty("respuesta", out var respProp) ? respProp.GetString() : "";
                        var puntuacion = respuestaJson.TryGetProperty("puntuacion", out var puntProp) ? puntProp.GetDouble() : 0;

                        var producto = _productos.FirstOrDefault(p => p.Id == productoId);

                        _logger.LogInformation("JSON parseado exitosamente - ProductoId: {ProductoId}", productoId);

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
                    catch (JsonException jsonEx)
                    {
                        _logger.LogWarning(jsonEx, "Error parseando JSON específico, usando respuesta completa de OpenAI");
                    }
                }
                else
                {
                    _logger.LogInformation("No se encontró JSON válido en la respuesta, usando respuesta completa de OpenAI");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error general procesando respuesta IA, preservando respuesta original de OpenAI");
            }

            // PRESERVAR SIEMPRE LA RESPUESTA DE OPENAI
            // Si llegamos aquí, significa que no pudimos parsear JSON, pero tenemos una respuesta válida de OpenAI
            _logger.LogInformation("Preservando respuesta completa de OpenAI como texto");

            return new RecomendacionIA
            {
                ProductoId = -1,
                Respuesta = respuestaIA, // ¡PRESERVAMOS LA RESPUESTA COMPLETA DE OPENAI!
                Puntuacion = 80, // Alta puntuación porque viene de OpenAI
                NombreProducto = "",
                Categoria = "",
                Precio = 0,
                Descripcion = null,
                Alergenos = null,
                Ingredientes = null,
                Cantidad = 0
            };
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