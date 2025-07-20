using System.Text;
using System.Text.Json;

namespace ProyectoIdentity.Servicios
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _apiKey;

        public OpenAIService(HttpClient httpClient, ILogger<OpenAIService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiKey = _configuration["OpenAI:ApiKey"]; // Configura esto en appsettings.json

            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> GenerarRespuesta(string prompt)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("OpenAI API key no configurada");
                    return string.Empty;
                }

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 500,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Enviando solicitud a OpenAI...");

                var response = await _httpClient.PostAsync("chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (responseJson.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("message", out var message) &&
                            message.TryGetProperty("content", out var messageContent))
                        {
                            var result = messageContent.GetString() ?? string.Empty;
                            _logger.LogInformation("Respuesta obtenida de OpenAI exitosamente");
                            return result;
                        }
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error en OpenAI API: {StatusCode} - {Error}", response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error llamando a OpenAI API");
            }

            return string.Empty;
        }

        public async Task<bool> VerificarConexion()
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return false;
                }

                var response = await _httpClient.GetAsync("models");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando conexión con OpenAI");
                return false;
            }
        }
    }
}