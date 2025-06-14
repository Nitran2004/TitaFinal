using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Controllers
{
    [Authorize] // Requiere que el usuario esté logueado
    public class PedidoRecomendacionController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public PedidoRecomendacionController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // Modelos para el carrito
        public class ItemCarrito
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
            public decimal Subtotal { get; set; }
        }

        public class Pedido
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public List<ItemCarrito> Items { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; }
            public string UsuarioId { get; set; } // ID del usuario de Identity
            public string UsuarioEmail { get; set; } // Email del usuario
            public string ClienteNombre { get; set; } // Nombre del usuario
            public string ClienteTelefono { get; set; } // Teléfono del usuario
            public string Direccion { get; set; } // Dirección del usuario
            public string TipoServicio { get; set; } // "Servir aquí" o "Para llevar"
            public string Observaciones { get; set; } // Observaciones adicionales opcionales
        }

        // Lista estática para simular base de datos de pedidos
        private static List<Pedido> _pedidos = new List<Pedido>();
        private static int _siguienteId = 1;

        // Vista del carrito
        public IActionResult VerCarrito()
        {
            return View();
        }

        // Procesar pedido desde el carrito - USANDO DATOS DEL USUARIO LOGUEADO
        [HttpPost]
        public async Task<IActionResult> ProcesarPedido([FromBody] PedidoRequest request)
        {
            try
            {
                // Obtener el usuario logueado
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                // Si el usuario es tipo AppUsuario, obtener datos adicionales
                var appUsuario = usuario as AppUsuario;

                var pedido = new Pedido
                {
                    Id = _siguienteId++,
                    Fecha = DateTime.Now,
                    Items = request.Items,
                    Total = request.Items.Sum(i => i.Subtotal),
                    Estado = "Pendiente",
                    UsuarioId = usuario.Id,
                    UsuarioEmail = usuario.Email,
                    ClienteNombre = appUsuario?.Nombre ?? usuario.UserName, // Usar el nombre del perfil o email
                    ClienteTelefono = appUsuario?.Telefono ?? "No especificado",
                    Direccion = appUsuario?.Direccion ?? "No especificada",
                    TipoServicio = request.TipoServicio,
                    Observaciones = request.Observaciones ?? ""
                };

                _pedidos.Add(pedido);

                return Json(new
                {
                    success = true,
                    pedidoId = pedido.Id,
                    message = "Pedido creado exitosamente"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al procesar el pedido: " + ex.Message
                });
            }
        }

        // Ver confirmación del pedido
        public IActionResult Confirmacion(int id)
        {
            var pedido = _pedidos.FirstOrDefault(p => p.Id == id);
            if (pedido == null)
            {
                return NotFound();
            }
            return View(pedido);
        }

        // Listar todos los pedidos (para administración)
        public IActionResult ListarPedidos()
        {
            return View(_pedidos.OrderByDescending(p => p.Fecha));
        }

        // Listar pedidos del usuario logueado
        public async Task<IActionResult> MisPedidos()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return RedirectToAction("Acceso", "Cuentas");
            }

            var pedidosUsuario = _pedidos
                .Where(p => p.UsuarioId == usuario.Id)
                .OrderByDescending(p => p.Fecha)
                .ToList();

            return View(pedidosUsuario);
        }

        // Modelo para recibir el pedido - CON TIPO DE SERVICIO
        public class PedidoRequest
        {
            public List<ItemCarrito> Items { get; set; }
            public string TipoServicio { get; set; } // "Servir aquí" o "Para llevar"
            public string Observaciones { get; set; } // Observaciones adicionales opcionales
        }
    }
}