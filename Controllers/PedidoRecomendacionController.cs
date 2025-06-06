using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class PedidoRecomendacionController : Controller
    {
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
            public string ClienteNombre { get; set; }
            public string ClienteTelefono { get; set; }
            public string Direccion { get; set; }
            public string Observaciones { get; set; }
        }

        // Lista estática para simular base de datos de pedidos
        private static List<Pedido> _pedidos = new List<Pedido>();
        private static int _siguienteId = 1;

        // Referencia a los datos del menú del otro controlador
        private readonly List<MenuRecomendacionController.Plato> _menuData;

        public PedidoRecomendacionController()
        {
            // Inicializar con los mismos datos del menú
            var menuController = new MenuRecomendacionController();
            _menuData = GetMenuData();
        }

        // Método para obtener los datos del menú (copia de MenuRecomendacionController)
        private List<MenuRecomendacionController.Plato> GetMenuData()
        {
            return new List<MenuRecomendacionController.Plato>
            {
                // Solo algunos ejemplos, puedes copiar todos los datos del otro controlador
                new MenuRecomendacionController.Plato { Nombre = "Margarita", Precio = 8, Calorias = 250, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, tomate cherry, albahaca" },
                new MenuRecomendacionController.Plato { Nombre = "Pepperoni", Precio = 9, Calorias = 300, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, pepperoni" },
                // Agrega el resto de los datos aquí...
            };
        }

        // Vista del carrito
        public IActionResult VerCarrito()
        {
            return View();
        }

        // Procesar pedido desde el carrito
        [HttpPost]
        public IActionResult ProcesarPedido([FromBody] PedidoRequest request)
        {
            try
            {
                var pedido = new Pedido
                {
                    Id = _siguienteId++,
                    Fecha = DateTime.Now,
                    Items = request.Items,
                    Total = request.Items.Sum(i => i.Subtotal),
                    Estado = "Pendiente",
                    ClienteNombre = request.ClienteNombre,
                    ClienteTelefono = request.ClienteTelefono,
                    Direccion = request.Direccion,
                    Observaciones = request.Observaciones
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

        // Modelo para recibir el pedido
        public class PedidoRequest
        {
            public List<ItemCarrito> Items { get; set; }
            public string ClienteNombre { get; set; }
            public string ClienteTelefono { get; set; }
            public string Direccion { get; set; }
            public string Observaciones { get; set; }
        }
    }
}