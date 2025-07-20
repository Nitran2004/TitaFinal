using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using ProyectoIdentity.Servicios;
using System.Collections.Generic;
using System.Diagnostics;

namespace ProyectoIdentity.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly RecomendadorProductos _recomendador;

        public HomeController(ILogger<HomeController> logger,
                              ApplicationDbContext context,
                              RecomendadorProductos recomendador)
        {
            _logger = logger;
            _context = context;
            _recomendador = recomendador;
        }

        // GET: /Home/Index
        public IActionResult Index()
        {
            // Ya no necesitamos cargar productos en el Index
            // porque ahora solo mostramos enlaces a las categorías
            return View();
        }

        // GET: /Home/Chat - Vista del chat
        public IActionResult Chat()
        {
            return View();
        }

        // POST: /api/chat/mensaje - Endpoint para manejar mensajes del chat
        [HttpPost]
        [Route("api/chat/mensaje")]
        public async Task<IActionResult> ProcesarMensajeChat([FromBody] ChatRequest request)
        {
            try
            {
                _logger.LogInformation("Procesando mensaje de chat: {Mensaje}", request.Mensaje);

                // Obtener productos disponibles
                var productos = await _context.Productos
                    .Where(p => p.Cantidad > 0)
                    .ToListAsync();

                _logger.LogInformation("Tipo de la variable productos: {Tipo}", productos.GetType());


                if (!productos.Any())
                {
                    return Json(new
                    {
                        respuesta = "Lo siento, no tenemos productos disponibles en este momento.",
                        productoId = -1,
                        nombreProducto = "",
                        categoria = "",
                        precio = 0
                    });
                }

                // Inicializar el recomendador
                _recomendador.Inicializar(productos);

                // Obtener recomendación
                var recomendacion = await _recomendador.ObtenerRecomendacion(request.Mensaje,  productos);

                // Enriquecer respuesta con datos del producto
                if (recomendacion.ProductoId > 0)
                {
                    var producto = productos.FirstOrDefault(p => p.Id == recomendacion.ProductoId);
                    if (producto != null)
                    {
                        recomendacion.NombreProducto = producto.Nombre ?? "";
                        recomendacion.Categoria = producto.Categoria ?? "";
                        recomendacion.Precio = producto.Precio;
                        recomendacion.Descripcion = producto.Descripcion;
                        recomendacion.Cantidad = producto.Cantidad ?? 0; recomendacion.Alergenos = producto.Alergenos;
                        recomendacion.Ingredientes = producto.Ingredientes;
                    }
                }

                return Json(new
                {
                    respuesta = recomendacion.Respuesta,
                    productoId = recomendacion.ProductoId,
                    nombreProducto = recomendacion.NombreProducto,
                    categoria = recomendacion.Categoria,
                    precio = recomendacion.Precio.ToString("F0"),
                    descripcion = recomendacion.Descripcion,
                    cantidad = recomendacion.Cantidad,
                    alergenos = recomendacion.Alergenos,
                    ingredientes = recomendacion.Ingredientes,
                    puntuacion = recomendacion.Puntuacion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje de chat");

                return Json(new
                {
                    respuesta = "Lo siento, estoy teniendo dificultades técnicas. Por favor, intenta nuevamente.",
                    productoId = -1,
                    nombreProducto = "",
                    categoria = "",
                    precio = 0
                });
            }
        }

        // GET: /api/chat/verificar - Verificar estado del sistema
        [HttpGet]
        [Route("api/chat/verificar")]
        public async Task<IActionResult> VerificarSistema()
        {
            try
            {
                var cantidadProductos = await _context.Productos.CountAsync();
                var productosDisponibles = await _context.Productos.CountAsync(p => p.Cantidad > 0);

                return Json(new
                {
                    estado = "activo",
                    productos = cantidadProductos,
                    disponibles = productosDisponibles,
                    mensaje = $"Sistema funcionando. {productosDisponibles} productos disponibles."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando sistema");
                return Json(new
                {
                    estado = "error",
                    mensaje = "Error en el sistema"
                });
            }
        }

        //[Authorize(Roles= "Registrado,Administrador")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // Clase para manejar requests del chat desde el HomeController
    public class ChatRequest
    {
        public string Mensaje { get; set; } = string.Empty;
    }
}
