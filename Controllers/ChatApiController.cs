using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using ProyectoIdentity.Servicios;
using System.Security.Claims;

namespace ProyectoIdentity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly RecomendadorProductos _recomendador;
        private readonly ILogger<ChatApiController> _logger;

        public ChatApiController(
            ApplicationDbContext context,
            RecomendadorProductos recomendador,
            ILogger<ChatApiController> logger)
        {
            _context = context;
            _recomendador = recomendador;
            _logger = logger;
        }

        [HttpPost("solicitar")]
        public async Task<ActionResult<RespuestaChat>> SolicitarChat([FromBody] ChatRequest request)
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                _logger.LogInformation("Procesando solicitud de chat: {Mensaje} para usuario: {UsuarioId}",
                    request.Mensaje, usuarioId);

                // Crear registro de solicitud
                var solicitud = new SolicitudChat
                {
                    Mensaje = request.Mensaje,
                    UsuarioId = usuarioId,
                    EstadoSolicitud = "Procesando",
                    FechaCreacion = DateTime.Now
                };

                _context.SolicitudesChat.Add(solicitud);
                await _context.SaveChangesAsync();

                // Obtener productos de la base de datos
                var productos = await ObtenerProductosDisponibles();

                if (!productos.Any())
                {
                    return BadRequest(new RespuestaChat
                    {
                        Exito = false,
                        Mensaje = "No hay productos disponibles en este momento.",
                        TipoRespuesta = "error"
                    });
                }

                // Inicializar el recomendador
                _recomendador.Inicializar(productos);

                // Obtener recomendación
                var recomendacion = await _recomendador.ObtenerRecomendacion(request.Mensaje);

                // Actualizar solicitud con resultado
                solicitud.EstadoSolicitud = "Completada";
                solicitud.RespuestaIA = recomendacion.Respuesta;
                solicitud.ProductoRecomendadoId = recomendacion.ProductoId > 0 ? recomendacion.ProductoId : null;

                // Guardar en historial
                var historial = new HistorialChat
                {
                    UsuarioId = usuarioId,
                    Mensaje = request.Mensaje,
                    Respuesta = recomendacion.Respuesta,
                    TipoMensaje = "recomendacion",
                    ProductoId = recomendacion.ProductoId > 0 ? recomendacion.ProductoId : null,
                    Fecha = DateTime.Now
                };

                _context.HistorialChat.Add(historial);
                await _context.SaveChangesAsync();

                // Obtener productos alternativos si no encontró uno específico
                List<Producto>? alternativos = null;
                if (recomendacion.ProductoId <= 0)
                {
                    alternativos = await ObtenerProductosAlternativos(request.Mensaje, 3);
                }

                var respuesta = new RespuestaChat
                {
                    Exito = true,
                    Mensaje = recomendacion.Respuesta,
                    Recomendacion = recomendacion,
                    ProductosAlternativos = alternativos,
                    TipoRespuesta = recomendacion.ProductoId > 0 ? "recomendacion" : "informacion"
                };

                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando solicitud de chat");

                return StatusCode(500, new RespuestaChat
                {
                    Exito = false,
                    Mensaje = "Lo siento, estoy teniendo dificultades técnicas. Por favor, intenta nuevamente.",
                    TipoRespuesta = "error"
                });
            }
        }

        [HttpGet("sugerencias")]
        public async Task<ActionResult<RespuestaSugerencias>> ObtenerSugerencias([FromQuery] SugerenciasRequest request)
        {
            try
            {
                var sugerencias = new List<string>();

                if (!string.IsNullOrEmpty(request.Categoria))
                {
                    // Sugerencias específicas por categoría
                    sugerencias = request.Categoria.ToLower() switch
                    {
                        "pizza" => new List<string>
                        {
                            "¿Qué pizza recomiendan para compartir?",
                            "Quiero una pizza sin gluten",
                            "¿Cuál es su pizza más popular?",
                            "Pizza vegetariana por favor"
                        },
                        "bebidas" => new List<string>
                        {
                            "¿Qué bebidas sin alcohol tienen?",
                            "Recomiéndenme una cerveza",
                            "¿Tienen jugos naturales?",
                            "Algo refrescante para acompañar"
                        },
                        "cocteles" => new List<string>
                        {
                            "¿Cuál es su cóctel especial?",
                            "Algo dulce y suave",
                            "Un trago fuerte, por favor",
                            "¿Tienen cócteles sin alcohol?"
                        },
                        _ => new List<string>
                        {
                            "¿Qué me recomiendan para almorzar?",
                            "Algo ligero y saludable",
                            "¿Cuál es su especialidad?",
                            "Para compartir en grupo"
                        }
                    };
                }
                else
                {
                    // Sugerencias generales
                    sugerencias = new List<string>
                    {
                        "¿Qué me recomiendan hoy?",
                        "Algo sin gluten, por favor",
                        "¿Cuál es su especialidad de la casa?",
                        "Para compartir en pareja",
                        "Algo vegetariano",
                        "Lo más popular del menú",
                        "¿Tienen opciones veganas?",
                        "Algo rápido y delicioso"
                    };
                }

                return Ok(new RespuestaSugerencias
                {
                    Sugerencias = sugerencias,
                    Categoria = request.Categoria ?? "general"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo sugerencias");
                return StatusCode(500, new RespuestaSugerencias());
            }
        }

        [HttpGet("historial")]
        public async Task<ActionResult<List<HistorialChat>>> ObtenerHistorial()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var historial = await _context.HistorialChat
                    .Where(h => h.UsuarioId == usuarioId)
                    .Include(h => h.Producto)
                    .OrderByDescending(h => h.Fecha)
                    .Take(20)
                    .ToListAsync();

                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial");
                return StatusCode(500, new List<HistorialChat>());
            }
        }

        [HttpGet("metricas")]
        public async Task<ActionResult<MetricasChat>> ObtenerMetricas()
        {
            try
            {
                var totalConsultas = await _context.SolicitudesChat.CountAsync();
                var consultasExitosas = await _context.SolicitudesChat
                    .CountAsync(s => s.ProductoRecomendadoId.HasValue);

                var categoriasPopulares = await _context.HistorialChat
                    .Where(h => h.Producto != null)
                    .GroupBy(h => h.Producto!.Categoria)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key ?? "Sin categoría")
                    .ToListAsync();

                var consultasRecientes = await _context.SolicitudesChat
                    .OrderByDescending(s => s.FechaCreacion)
                    .Take(10)
                    .Select(s => s.Mensaje)
                    .ToListAsync();

                var metricas = new MetricasChat
                {
                    TotalConsultas = totalConsultas,
                    RecomendacionesExitosas = consultasExitosas,
                    ConsultasSinResultado = totalConsultas - consultasExitosas,
                    TasaExito = totalConsultas > 0 ? (double)consultasExitosas / totalConsultas * 100 : 0,
                    CategoriasPopulares = categoriasPopulares,
                    ConsultasRecientes = consultasRecientes
                };

                return Ok(metricas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo métricas");
                return StatusCode(500, new MetricasChat());
            }
        }

        [HttpGet("productos")]
        public async Task<ActionResult<List<Producto>>> ObtenerProductos()
        {
            try
            {
                var productos = await ObtenerProductosDisponibles();
                return Ok(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo productos");
                return StatusCode(500, new List<Producto>());
            }
        }

        // Métodos privados auxiliares
        private async Task<List<Producto>> ObtenerProductosDisponibles()
        {
            return await _context.Productos
                .Where(p => p.Cantidad > 0)
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Nombre)
                .ToListAsync();
        }

        private async Task<List<Producto>> ObtenerProductosAlternativos(string consulta, int cantidad)
        {
            // Lógica simple para obtener productos alternativos
            // Esto se puede mejorar con más análisis de la consulta
            var consultaLower = consulta.ToLower();

            var productos = await _context.Productos
                .Where(p => p.Cantidad > 0)
                .Where(p => p.Nombre!.ToLower().Contains(consultaLower) ||
                           p.Descripcion!.ToLower().Contains(consultaLower) ||
                           p.Categoria!.ToLower().Contains(consultaLower))
                .Take(cantidad)
                .ToListAsync();

            // Si no encuentra por búsqueda de texto, devolver productos populares
            if (!productos.Any())
            {
                productos = await _context.Productos
                    .Where(p => p.Cantidad > 0)
                    .OrderBy(p => p.Id)
                    .Take(cantidad)
                    .ToListAsync();
            }

            return productos;
        }
    }
}