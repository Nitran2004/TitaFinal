using ProyectoIdentity.Models;
using System.Text.Json;

namespace ProyectoIdentity.Servicios
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaService> _logger;
        private const string OLLAMA_URL = "http://localhost:11434/api/generate";

        public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<RecomendacionProducto> ObtenerRecomendacionInteligente(string consulta, List<Producto> productos)
        {
            try
            {
                // FILTRAR Y LIMITAR productos para evitar sobrecarga
                var productosOptimizados = FiltrarProductosRelevantes(consulta, productos);
                
                var productosTexto = GenerarContextoProductos(productosOptimizados);
                var prompt = ConstruirPrompt(consulta, productosTexto);

                var request = new
                {
                    model = "gemma3",
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.7,
                        max_tokens = 300, // Reducido para respuestas más rápidas
                        top_p = 0.9
                    }
                };

                _logger.LogInformation("Enviando consulta a Ollama con {ProductCount} productos optimizados: {Consulta}", 
                    productosOptimizados.Count, consulta);

                var response = await _httpClient.PostAsJsonAsync(OLLAMA_URL, request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error en Ollama: {StatusCode} - {Content}", 
                        response.StatusCode, await response.Content.ReadAsStringAsync());
                    return CrearRespuestaError();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

                return ProcesarRespuesta(ollamaResponse.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar Ollama");
                return CrearRespuestaError();
            }
        }

        private List<Producto> FiltrarProductosRelevantes(string consulta, List<Producto> productos)
        {
            var consultaLower = consulta.ToLower();
            var productosRelevantes = new List<Producto>();

            // 1. Buscar productos que coincidan directamente con la consulta
            var coincidenciasDirectas = productos.Where(p =>
                p.Nombre?.ToLower().Contains(consultaLower) == true ||
                p.Descripcion?.ToLower().Contains(consultaLower) == true ||
                p.Categoria?.ToLower().Contains(consultaLower) == true ||
                p.Ingredientes?.ToLower().Contains(consultaLower) == true
            ).Take(5).ToList();

            productosRelevantes.AddRange(coincidenciasDirectas);

            // 2. Detectar categoría específica de la consulta
            var categoriaDetectada = DetectarCategoria(consultaLower);
            if (!string.IsNullOrEmpty(categoriaDetectada))
            {
                var productosCategoria = productos
                    .Where(p => p.Categoria?.Equals(categoriaDetectada, StringComparison.OrdinalIgnoreCase) == true)
                    .Where(p => !productosRelevantes.Any(pr => pr.Id == p.Id)) // Evitar duplicados
                    .Take(5)
                    .ToList();
                
                productosRelevantes.AddRange(productosCategoria);
            }

            // 3. Detectar restricciones dietéticas
            if (DetectarSinGluten(consultaLower))
            {
                var sinGluten = productos
                    .Where(p => !ContieneAlergeno(p.Alergenos, "Gluten"))
                    .Where(p => !productosRelevantes.Any(pr => pr.Id == p.Id))
                    .Take(3)
                    .ToList();
                
                productosRelevantes.AddRange(sinGluten);
            }

            // 4. Si no encontramos suficientes, agregar productos populares por categoría
            if (productosRelevantes.Count < 8)
            {
                var categorias = new[] { "Pizza", "Bebidas", "Cerveza", "Cocteles" };
                foreach (var cat in categorias)
                {
                    if (productosRelevantes.Count >= 8) break;
                    
                    var productosPopulares = productos
                        .Where(p => p.Categoria == cat)
                        .Where(p => !productosRelevantes.Any(pr => pr.Id == p.Id))
                        .Take(2)
                        .ToList();
                    
                    productosRelevantes.AddRange(productosPopulares);
                }
            }

            // Limitar a máximo 10 productos para evitar timeout
            return productosRelevantes.Take(10).ToList();
        }

        private string GenerarContextoProductos(List<Producto> productos)
        {
            return string.Join("\n", productos.Select(p =>
            {
                var info = $"ID:{p.Id} | {p.Nombre} | {p.Descripcion} | {p.Categoria} | ${p.Precio}";
                
                if (!string.IsNullOrEmpty(p.Alergenos) && p.Alergenos != "NULL")
                    info += $" | Alérgenos:{p.Alergenos}";
                else
                    info += " | Sin alérgenos conocidos";

                info += $" | Disponibles:{p.Cantidad}";
                return info;
            }));
        }

        private string ConstruirPrompt(string consulta, string productosTexto)
        {
            return $@"Eres un asistente de nuestro restaurante. Recomienda SOLO productos de esta lista:

{productosTexto}

Cliente pregunta: {consulta}

INSTRUCCIONES:
- Recomienda UN producto específico de la lista
- Si pide sin gluten, verifica alérgenos
- Sé breve y amable
- Incluye precio e ID

FORMATO: [Tu respuesta]|[ID_DEL_PRODUCTO]
Si no encuentras nada específico: |--1

RESPUESTA:";
        }

        private RecomendacionProducto ProcesarRespuesta(string respuestaOllama)
        {
            try
            {
                _logger.LogInformation("Procesando respuesta de Ollama: {Respuesta}", respuestaOllama);

                var partes = respuestaOllama.Split('|');
                var respuestaTexto = partes[0].Trim();
                var productoId = -1;

                if (partes.Length > 1)
                {
                    var idTexto = partes[partes.Length - 1].Trim();
                    
                    if (idTexto == "-1" || idTexto == "--1")
                    {
                        productoId = -1;
                    }
                    else
                    {
                        int.TryParse(idTexto, out productoId);
                    }
                }

                return new RecomendacionProducto
                {
                    ProductoId = productoId,
                    Respuesta = respuestaTexto,
                    Puntuacion = productoId > 0 ? 1.0 : 0.5,
                    NombreProducto = "",
                    Categoria = "",
                    Precio = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando respuesta de Ollama: {Respuesta}", respuestaOllama);
                return CrearRespuestaError();
            }
        }

        private RecomendacionProducto CrearRespuestaError()
        {
            return new RecomendacionProducto
            {
                ProductoId = -1,
                Respuesta = "Disculpa, estoy teniendo dificultades técnicas. Por favor, intenta nuevamente o consulta nuestro menú directamente.",
                Puntuacion = 0
            };
        }

        public async Task<bool> VerificarConexionOllama()
        {
            try
            {
                var testRequest = new
                {
                    model = "gemma3",
                    prompt = "Test",
                    stream = false,
                    options = new { max_tokens = 5 }
                };

                var response = await _httpClient.PostAsJsonAsync(OLLAMA_URL, testRequest);
                var isSuccess = response.IsSuccessStatusCode;
                
                _logger.LogInformation("Verificación Ollama: {Status}", isSuccess ? "Conectado" : "Desconectado");
                
                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error verificando conexión con Ollama");
                return false;
            }
        }

        // Métodos auxiliares
        private bool DetectarSinGluten(string consulta)
        {
            var palabrasClavesSinGluten = new[] { "sin gluten", "libre de gluten", "gluten free", "celíaco", "celiaco" };
            return palabrasClavesSinGluten.Any(palabraClave => consulta.Contains(palabraClave));
        }

        private string DetectarCategoria(string consulta)
        {
            var mapaCategorias = new Dictionary<string, string[]>
            {
                ["Pizza"] = new[] { "pizza", "pizzas" },
                ["Cerveza"] = new[] { "cerveza", "cervezas", "beer" },
                ["Cocteles"] = new[] { "coctel", "cóctel", "cocteles", "cócteles", "cocktail", "trago", "tragos" },
                ["Bebidas"] = new[] { "bebida", "bebidas", "jugo", "gaseosa", "agua", "refresco" },
                ["Sánduches"] = new[] { "sandwich", "sándwich", "sanduche", "sánduche" },
                ["Shot"] = new[] { "shot", "shots", "chupito" },
                ["Picadas"] = new[] { "picada", "picadas", "compartir", "aperitivo" },
                ["Promo"] = new[] { "promo", "promoción", "oferta", "combo" }
            };

            foreach (var categoria in mapaCategorias)
            {
                if (categoria.Value.Any(palabraClave => consulta.Contains(palabraClave)))
                {
                    return categoria.Key;
                }
            }

            return string.Empty;
        }

        private bool ContieneAlergeno(string? alergenos, string alergeno)
        {
            if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
                return false;

            return alergenos.Split('|')
                .Any(a => a.Trim().Equals(alergeno, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool Done { get; set; }
        public string Model { get; set; } = string.Empty;
    }
}