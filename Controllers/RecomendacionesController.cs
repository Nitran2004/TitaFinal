using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Models;
using ProyectoIdentity.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProyectoIdentity.Datos;

[ApiController]
[Route("api/[controller]")]
public class RecomendacionesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly RecomendadorProductos _recomendador;
    private readonly ILogger<RecomendacionesController> _logger;

    public RecomendacionesController(
        ApplicationDbContext context,
        RecomendadorProductos recomendador,
        ILogger<RecomendacionesController> logger)
    {
        _context = context;
        _recomendador = recomendador;
        _logger = logger;
    }

    [HttpPost("consultar")]
    public async Task<IActionResult> ConsultarRecomendacion([FromBody] ConsultaRequest request)
    {
        try
        {
            _logger.LogInformation("Recibida consulta: {Consulta}", request.Consulta);

            // 1. Obtener TODOS los productos de tu base de datos
            var productos = await _context.Productos
                .Where(p => p.Cantidad > 0) // Solo productos disponibles
                .Select(p => new Producto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Categoria = p.Categoria,
                    Precio = p.Precio,
                    Alergenos = p.Alergenos,
                    InfoNutricional = p.InfoNutricional,
                    Ingredientes = p.Ingredientes,
                    Cantidad = p.Cantidad
                })
                .ToListAsync();

            _logger.LogInformation("Encontrados {Count} productos en la base de datos", productos.Count);

            if (!productos.Any())
            {
                return BadRequest(new { mensaje = "No hay productos disponibles en la base de datos" });
            }

            // 2. Inicializar el recomendador con TUS productos
            _recomendador.Inicializar(productos);

            // 3. Obtener recomendación basada en TUS datos
            var recomendacion = await _recomendador.ObtenerRecomendacion(request.Consulta, productos);

            // 4. Si encontró un producto específico, enriquecer con más datos
            if (recomendacion.ProductoId > 0)
            {
                var productoCompleto = productos.FirstOrDefault(p => p.Id == recomendacion.ProductoId);
                if (productoCompleto != null)
                {
                    recomendacion.InfoNutricional = productoCompleto.InfoNutricional;
                    recomendacion.Ingredientes = productoCompleto.Ingredientes;
                recomendacion.Cantidad = productoCompleto.Cantidad ?? 0;
                }
            }

            return Ok(new
            {
                exito = true,
                recomendacion = recomendacion,
                totalProductosConsiderados = productos.Count,
                mensaje = recomendacion.ProductoId > 0
                    ? "Recomendación basada en tu inventario"
                    : "No se encontró un producto específico, pero aquí tienes información útil"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando consulta de recomendación");
            return StatusCode(500, new { mensaje = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("verificar")]
    public async Task<IActionResult> VerificarSistema()
    {
        try
        {
            // Verificar conexión a base de datos
            var cantidadProductos = await _context.Productos.CountAsync();

            // Verificar conexión a Ollama (necesitas inyectar OllamaService también)
            var ollamaDisponible = true; // Implementar verificación de Ollama

            return Ok(new
            {
                baseDatos = new { conectada = true, productos = cantidadProductos },
                ollama = new { conectada = ollamaDisponible },
                estado = "Sistema listo"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error verificando sistema", error = ex.Message });
        }
    }

    [HttpGet("productos")]
    public async Task<IActionResult> ObtenerProductos()
    {
        try
        {
            var productos = await _context.Productos
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Descripcion,
                    p.Categoria,
                    p.Precio,
                    p.Cantidad,
                    p.Alergenos,
                    p.Ingredientes
                })
                .ToListAsync();

            return Ok(productos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error obteniendo productos", error = ex.Message });
        }
    }
}

public class ConsultaRequest
{
    public string Consulta { get; set; } = string.Empty;
}