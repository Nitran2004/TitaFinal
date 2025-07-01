using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Globalization;
using ProyectoIdentity.Models;
using ProyectoIdentity.Controllers; // Para usar la clase del PersonalizacionController
using System.Security.Claims;

namespace ProyectoIdentity.Controllers
{
    [Authorize] // Requiere que el usuario esté logueado
    public class MenuRecomendacionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuRecomendacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class Plato
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Calorias { get; set; }
            public string Categoria { get; set; }
            public string Ingredientes { get; set; }
            public double? Similitud { get; set; }
        }

        // Estructura TF-IDF
        private class TFIDFData
        {
            public List<double[]> VectoresTFIDF { get; set; }
            public List<string> Terminos { get; set; }
        }

        // ✅ MÉTODO PARA EXTRAER CALORÍAS DEL STRING DE INFO NUTRICIONAL
        private int ExtraerCalorias(string infoNutricional)
        {
            if (string.IsNullOrEmpty(infoNutricional))
                return 0;

            try
            {
                // Buscar patrón "Calorías:517" o "Calorías:517Kcal"
                var match = Regex.Match(infoNutricional, @"Calorías:(\d+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }
            }
            catch
            {
                // Si no se puede extraer, retornar 0
            }

            return 0;
        }

        // ✅ MÉTODO PARA CONVERTIR PRODUCTOS DE BD A PLATOS
        private List<Plato> ConvertirProductosAPlatos(List<ProyectoIdentity.Models.Producto> productos)
        {
            var platos = new List<Plato>();

            foreach (var producto in productos)
            {
                // Extraer ingredientes del JSON o usar descripción
                string ingredientesTexto = producto.Descripcion ?? "";

                if (!string.IsNullOrEmpty(producto.Ingredientes))
                {
                    try
                    {
                        // Intentar parsear el JSON de ingredientes
                        var ingredientesJson = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(producto.Ingredientes);
                        if (ingredientesJson != null && ingredientesJson.Any())
                        {
                            ingredientesTexto = string.Join(", ", ingredientesJson.Select(i => i["Nombre"].ToString()));
                        }
                    }
                    catch
                    {
                        // Si falla el JSON, usar la descripción
                        ingredientesTexto = producto.Descripcion ?? "";
                    }
                }

                var plato = new Plato
                {
                    Id = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Calorias = ExtraerCalorias(producto.InfoNutricional), // ✅ EXTRAER CALORÍAS
                    Categoria = producto.Categoria,
                    Ingredientes = ingredientesTexto
                };

                platos.Add(plato);
            }

            return platos;
        }

        // Vista principal
        public async Task<IActionResult> Recomendacion()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var (permitido, productosActivos, productosCarritos, disponibles, mensaje) = await ValidarLimitesGlobales(userId);

                if (!permitido)
                {
                    var viewModelLimite = new ProyectoIdentity.Controllers.LimiteAlcanzadoViewModel // ✅ USAR LA CLASE COMPLETA
                    {
                        PedidosActivos = productosActivos + productosCarritos,
                        LimiteMaximo = 3,
                        PedidosPendientes = new List<ProyectoIdentity.Controllers.PedidoPendienteInfo>()
                    };
                    return View("../Shared/LimiteAlcanzado", viewModelLimite);
                }

                ViewBag.ProductosActivos = productosActivos;
                ViewBag.ProductosEnCarritos = productosCarritos;
                ViewBag.Disponibles = disponibles;
                ViewBag.LimiteMaximo = 3;
            }

            var categorias = await _context.Productos
                .Where(p => !string.IsNullOrEmpty(p.Categoria))
                .Select(p => p.Categoria)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categorias = categorias;
            return View();
        }

        // Método para obtener recomendaciones
        [HttpPost]
        public async Task<IActionResult> ObtenerRecomendaciones(string categoria, decimal presupuesto, string ingredientes)
        {
            try
            {
                // Logging para debug
                Console.WriteLine($"Categoría: {categoria}, Presupuesto: {presupuesto}, Ingredientes: {ingredientes}");

                // ✅ OBTENER PRODUCTOS DESDE LA BD
                var productosDB = await _context.Productos.ToListAsync();
                var _data = ConvertirProductosAPlatos(productosDB);

                Console.WriteLine($"Productos cargados desde BD: {_data.Count}");

                // Filtrado inicial por precio y categoría
                var filtrado = _data.Where(item => item.Precio <= presupuesto).ToList();
                Console.WriteLine($"Platos después de filtro por presupuesto: {filtrado.Count}");

                if (!string.IsNullOrEmpty(categoria) && categoria != "Cualquiera")
                {
                    filtrado = filtrado.Where(item => item.Categoria == categoria).ToList();
                    Console.WriteLine($"Platos después de filtro por categoría: {filtrado.Count}");
                }

                // Filtrado por ingredientes
                if (!string.IsNullOrEmpty(ingredientes) && ingredientes.ToLower() != "todos")
                {
                    var ingredientesLower = ingredientes.ToLower();

                    // Lista de filtros posibles para ingredientes

                    // Filtros de carne
                    if (ingredientesLower.Contains("sin carne"))
                    {
                        filtrado = filtrado.Where(item =>
                            !item.Ingredientes.ToLower().Contains("carne") &&
                            !item.Ingredientes.ToLower().Contains("jamón") &&
                            !item.Ingredientes.ToLower().Contains("pepperoni") &&
                            !item.Ingredientes.ToLower().Contains("tocino") &&
                            !item.Ingredientes.ToLower().Contains("salami")
                        ).ToList();
                    }
                    else if (ingredientesLower.Contains("con carne"))
                    {
                        filtrado = filtrado.Where(item =>
                            item.Ingredientes.ToLower().Contains("carne") ||
                            item.Ingredientes.ToLower().Contains("jamón") ||
                            item.Ingredientes.ToLower().Contains("pepperoni") ||
                            item.Ingredientes.ToLower().Contains("tocino") ||
                            item.Ingredientes.ToLower().Contains("salami")
                        ).ToList();
                    }

                    // Filtros de queso
                    if (ingredientesLower.Contains("sin queso"))
                    {
                        filtrado = filtrado.Where(item =>
                            !item.Ingredientes.ToLower().Contains("queso") &&
                            !item.Ingredientes.ToLower().Contains("mozzarella") &&
                            !item.Ingredientes.ToLower().Contains("cheddar") &&
                            !item.Ingredientes.ToLower().Contains("parmesano") &&
                            !item.Ingredientes.ToLower().Contains("gorgonzola")
                        ).ToList();
                    }
                    else if (ingredientesLower.Contains("con queso"))
                    {
                        filtrado = filtrado.Where(item =>
                            item.Ingredientes.ToLower().Contains("queso") ||
                            item.Ingredientes.ToLower().Contains("mozzarella") ||
                            item.Ingredientes.ToLower().Contains("cheddar") ||
                            item.Ingredientes.ToLower().Contains("parmesano") ||
                            item.Ingredientes.ToLower().Contains("gorgonzola")
                        ).ToList();
                    }

                    // Filtros de tomate
                    if (ingredientesLower.Contains("sin tomate"))
                    {
                        filtrado = filtrado.Where(item =>
                            !item.Ingredientes.ToLower().Contains("tomate")
                        ).ToList();
                    }
                    else if (ingredientesLower.Contains("con tomate"))
                    {
                        filtrado = filtrado.Where(item =>
                            item.Ingredientes.ToLower().Contains("tomate")
                        ).ToList();
                    }

                    // Filtros de champiñones
                    if (ingredientesLower.Contains("sin champiñones") || ingredientesLower.Contains("sin champinones") || ingredientesLower.Contains("sin hongos"))
                    {
                        filtrado = filtrado.Where(item =>
                            !item.Ingredientes.ToLower().Contains("champiñones") &&
                            !item.Ingredientes.ToLower().Contains("hongos")
                        ).ToList();
                    }
                    else if (ingredientesLower.Contains("con champiñones") || ingredientesLower.Contains("con champinones") || ingredientesLower.Contains("con hongos"))
                    {
                        filtrado = filtrado.Where(item =>
                            item.Ingredientes.ToLower().Contains("champiñones") ||
                            item.Ingredientes.ToLower().Contains("hongos")
                        ).ToList();
                    }

                    // Filtros de cebolla
                    if (ingredientesLower.Contains("sin cebolla"))
                    {
                        filtrado = filtrado.Where(item =>
                            !item.Ingredientes.ToLower().Contains("cebolla")
                        ).ToList();
                    }
                    else if (ingredientesLower.Contains("con cebolla"))
                    {
                        filtrado = filtrado.Where(item =>
                            item.Ingredientes.ToLower().Contains("cebolla")
                        ).ToList();
                    }

                    // Filtros de piña (importante para pizzas hawaianas)
                    if (ingredientesLower.Contains("sin piña") || ingredientesLower.Contains("sin pina"))
                    {
                        filtrado = filtrado.Where(item =>
                            !item.Ingredientes.ToLower().Contains("piña")
                        ).ToList();
                    }
                    else if (ingredientesLower.Contains("con piña") || ingredientesLower.Contains("con pina"))
                    {
                        filtrado = filtrado.Where(item =>
                            item.Ingredientes.ToLower().Contains("piña")
                        ).ToList();
                    }

                    // Filtros de picante
                    if (ingredientesLower.Contains("sin picante"))
                    {
                        filtrado = filtrado.Where(item =>
                            !item.Ingredientes.ToLower().Contains("picante") &&
                            !item.Ingredientes.ToLower().Contains("chile") &&
                            !item.Ingredientes.ToLower().Contains("jalapeños")
                        ).ToList();
                    }
                    else if (ingredientesLower.Contains("con picante") || ingredientesLower.Contains("picante"))
                    {
                        filtrado = filtrado.Where(item =>
                            item.Ingredientes.ToLower().Contains("picante") ||
                            item.Ingredientes.ToLower().Contains("chile") ||
                            item.Ingredientes.ToLower().Contains("jalapeños")
                        ).ToList();
                    }

                    // Filtros vegetarianos
                    if (ingredientesLower.Contains("vegetariano") || ingredientesLower.Contains("vegetariana"))
                    {
                        filtrado = filtrado.Where(item =>
                            !item.Ingredientes.ToLower().Contains("carne") &&
                            !item.Ingredientes.ToLower().Contains("jamón") &&
                            !item.Ingredientes.ToLower().Contains("pepperoni") &&
                            !item.Ingredientes.ToLower().Contains("tocino") &&
                            !item.Ingredientes.ToLower().Contains("salami")
                        ).ToList();
                    }

                    // Si no se encuentra ninguno de los filtros específicos anteriores, se utiliza el TF-IDF
                    else if (!ingredientesLower.Contains("sin carne") &&
                             !ingredientesLower.Contains("con carne") &&
                             !ingredientesLower.Contains("sin queso") &&
                             !ingredientesLower.Contains("con queso") &&
                             !ingredientesLower.Contains("sin tomate") &&
                             !ingredientesLower.Contains("con tomate") &&
                             !ingredientesLower.Contains("sin champiñones") &&
                             !ingredientesLower.Contains("sin champinones") &&
                             !ingredientesLower.Contains("sin hongos") &&
                             !ingredientesLower.Contains("con champiñones") &&
                             !ingredientesLower.Contains("con champinones") &&
                             !ingredientesLower.Contains("con hongos") &&
                             !ingredientesLower.Contains("sin cebolla") &&
                             !ingredientesLower.Contains("con cebolla") &&
                             !ingredientesLower.Contains("sin piña") &&
                             !ingredientesLower.Contains("sin pina") &&
                             !ingredientesLower.Contains("con piña") &&
                             !ingredientesLower.Contains("con pina") &&
                             !ingredientesLower.Contains("sin picante") &&
                             !ingredientesLower.Contains("con picante") &&
                             !ingredientesLower.Contains("picante") &&
                             !ingredientesLower.Contains("vegetariano") &&
                             !ingredientesLower.Contains("vegetariana"))
                    {
                        // Implementar TF-IDF para búsqueda por ingredientes específicos
                        Console.WriteLine("Aplicando TF-IDF...");
                        var tfidfData = CrearVectoresTFIDF(_data);
                        var terminosConsulta = ingredientesLower.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var vectorConsulta = new double[tfidfData.Terminos.Count];

                        // Crear vector de consulta
                        foreach (var termino in terminosConsulta)
                        {
                            var indice = tfidfData.Terminos.IndexOf(termino);
                            if (indice != -1)
                            {
                                vectorConsulta[indice] = 1;
                            }
                        }

                        // Calcular similitud para cada plato filtrado
                        var platosConSimilitud = new List<Plato>();
                        foreach (var plato in filtrado)
                        {
                            var indice = _data.FindIndex(item => item.Id == plato.Id);
                            if (indice != -1 && indice < tfidfData.VectoresTFIDF.Count)
                            {
                                var similitud = SimilitudCoseno(vectorConsulta, tfidfData.VectoresTFIDF[indice]);
                                platosConSimilitud.Add(new Plato
                                {
                                    Id = plato.Id,
                                    Nombre = plato.Nombre,
                                    Precio = plato.Precio,
                                    Calorias = plato.Calorias,
                                    Categoria = plato.Categoria,
                                    Ingredientes = plato.Ingredientes,
                                    Similitud = similitud
                                });
                            }
                        }

                        // Ordenar por similitud y filtrar los que tienen similitud > 0
                        filtrado = platosConSimilitud
                            .Where(p => p.Similitud > 0)
                            .OrderByDescending(p => p.Similitud)
                            .ToList();

                        // Si no hay resultados con similitud > 0, tomar todos ordenados
                        if (filtrado.Count == 0)
                        {
                            filtrado = platosConSimilitud.OrderByDescending(p => p.Similitud).ToList();
                        }
                    }
                }

                // Tomar los 5 mejores resultados
                var recomendaciones = filtrado.Take(5).ToList();
                Console.WriteLine($"Devolviendo {recomendaciones.Count} recomendaciones");

                // Serialización manual para evitar problemas con el formato
                var result = recomendaciones.Select(p => new
                {
                    id = p.Id,
                    nombre = p.Nombre,
                    precio = p.Precio,
                    calorias = p.Calorias,
                    categoria = p.Categoria,
                    ingredientes = p.Ingredientes,
                    similitud = p.Similitud
                }).ToList();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Implementación de TF-IDF
        private TFIDFData CrearVectoresTFIDF(List<Plato> datos)
        {
            var todosTerminos = new HashSet<string>();
            var documentosProcesados = new List<List<string>>();

            // Procesar ingredientes
            foreach (var plato in datos)
            {
                var terminos = plato.Ingredientes.ToLower()
                    .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => t.Length > 2)
                    .ToList();

                documentosProcesados.Add(terminos);
                terminos.ForEach(t => todosTerminos.Add(t));
            }

            var terminosList = todosTerminos.ToList();
            var idf = new Dictionary<string, double>();

            // Calcular IDF
            foreach (var termino in terminosList)
            {
                var documentosConTermino = documentosProcesados.Count(doc => doc.Contains(termino));
                idf[termino] = Math.Log((double)datos.Count / (1 + documentosConTermino));
            }

            // Calcular vectores TF-IDF
            var vectoresTFIDF = new List<double[]>();

            foreach (var doc in documentosProcesados)
            {
                var vector = new double[terminosList.Count];
                var frecuencias = new Dictionary<string, int>();

                // Calcular frecuencias
                foreach (var termino in doc)
                {
                    frecuencias[termino] = frecuencias.ContainsKey(termino) ? frecuencias[termino] + 1 : 1;
                }

                // Calcular TF-IDF
                for (int i = 0; i < terminosList.Count; i++)
                {
                    var termino = terminosList[i];
                    var tf = frecuencias.ContainsKey(termino) ? frecuencias[termino] : 0;
                    vector[i] = tf * idf[termino];
                }

                vectoresTFIDF.Add(vector);
            }

            return new TFIDFData { VectoresTFIDF = vectoresTFIDF, Terminos = terminosList };
        }

        // Calcular similitud coseno
        private double SimilitudCoseno(double[] vectorA, double[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                throw new ArgumentException("Los vectores deben tener la misma longitud");

            double producto = 0;
            double normaA = 0;
            double normaB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                producto += vectorA[i] * vectorB[i];
                normaA += vectorA[i] * vectorA[i];
                normaB += vectorB[i] * vectorB[i];
            }

            normaA = Math.Sqrt(normaA);
            normaB = Math.Sqrt(normaB);

            if (normaA == 0 || normaB == 0)
                return 0;

            return producto / (normaA * normaB);
        }

        // ✅ REEMPLAZAR el método Detalle existente
        public async Task<IActionResult> Detalle(int id)
        {
            try
            {
                // ✅ BUSCAR EL PRODUCTO EN LA BD
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                {
                    return NotFound();
                }

                // ✅ CONVERTIR A PLATO
                var platos = ConvertirProductosAPlatos(new List<ProyectoIdentity.Models.Producto> { producto });
                var plato = platos.FirstOrDefault();

                if (plato == null)
                {
                    return NotFound();
                }

                // ✅ PASAR EL PRODUCTO COMPLETO PARA IMAGEN E INFORMACIÓN
                ViewBag.ProductoCompleto = producto;
                ViewBag.IndiceProducto = id;

                Console.WriteLine($"[DEBUG] Producto: {producto.Nombre}, Imagen: {producto.Imagen?.Length ?? 0} bytes");

                return View(plato);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Detalle: {ex.Message}");
                return View("Error");
            }
        }

        // ✅ MÉTODO AGREGAR AL CARRITO CORREGIDO
        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito([FromBody] PersonalizacionRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // ✅ VALIDAR LÍMITES UNIFICADOS
                var (permitido, disponibles, mensaje) = await ValidarAgregarProductosUnificado(userId, request.Cantidad);
                if (!permitido)
                {
                    return Json(new { success = false, message = mensaje });
                }

                var producto = await _context.Productos.FindAsync(request.ProductoId);
                if (producto == null)
                    return Json(new { success = false, message = "Producto no encontrado" });

                var carrito = GetCarritoRecomendacion();

                // ✅ VALIDAR LÍMITE EN CARRITOS COMBINADOS
                int productosEnCarritos = ContarProductosEnCarritos(userId);
                if (productosEnCarritos + request.Cantidad > disponibles + productosEnCarritos)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Solo puedes agregar {disponibles} producto(s) más considerando ambos carritos."
                    });
                }

                var itemCarrito = new ItemCarritoPersonalizado
                {
                    Id = request.ProductoId,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Cantidad = request.Cantidad,
                    IngredientesRemovidos = request.IngredientesRemovidos ?? new List<string>(),
                    NotasEspeciales = request.NotasEspeciales ?? "Pedido por recomendación IA",
                    AhorroInterno = 0, // Sin descuentos en recomendaciones IA
                    Subtotal = producto.Precio * request.Cantidad
                };

                carrito.Add(itemCarrito);
                SetCarritoRecomendacion(carrito);

                // ✅ OBTENER LÍMITES ACTUALIZADOS
                var (_, productosActivos, productosCarritos, disponiblesActualizados, _) = await ValidarLimitesGlobales(userId);

                return Json(new
                {
                    success = true,
                    message = "Producto agregado al carrito",
                    totalItems = carrito.Sum(c => c.Cantidad),
                    totalCarrito = carrito.Sum(c => c.Subtotal),
                    // ✅ INFORMACIÓN UNIFICADA
                    disponibles = disponiblesActualizados,
                    productosActivos = productosActivos,
                    productosEnCarritos = productosCarritos
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult VerCarrito()
        {
            try
            {
                var carrito = GetCarritoRecomendacion();

                Console.WriteLine($"[DEBUG] VerCarrito - Cantidad de items: {carrito.Count}");

                // Validar que todos los items tengan datos válidos
                foreach (var item in carrito)
                {
                    if (item.Subtotal <= 0 && item.Precio > 0 && item.Cantidad > 0)
                    {
                        item.Subtotal = item.Precio * item.Cantidad;
                        Console.WriteLine($"[DEBUG] Recalculando subtotal para {item.Nombre}: {item.Subtotal}");
                    }

                    Console.WriteLine($"[DEBUG] Item: {item.Nombre}, Precio: {item.Precio}, Cantidad: {item.Cantidad}, Total: {item.Subtotal}");
                }

                return View(carrito);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en VerCarrito: {ex.Message}");
                // Devolver carrito vacío en caso de error
                return View(new List<ItemCarritoPersonalizado>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarPedido([FromBody] PedidoRequest request)
        {
            try
            {

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var (countActivos, pedidosActivos) = await ContarPedidosActivos(currentUserId);

                    if (countActivos >= 3)
                    {
                        var viewModelLimite = CrearViewModelLimite(countActivos, pedidosActivos);
                        return View("LimiteAlcanzado", viewModelLimite);
                    }

                    ViewBag.PedidosActivos = countActivos;
                    ViewBag.LimiteMaximo = 3;
                }
                var carritoItems = GetCarritoRecomendacion();

                Console.WriteLine($"[DEBUG] Carrito obtenido: {carritoItems.Count} items");

                // ✅ VALIDACIÓN MEJORADA DEL CARRITO
                if (carritoItems == null || !carritoItems.Any())
                {
                    Console.WriteLine("[DEBUG] Carrito está vacío o es null");
                    return Json(new { success = false, message = "El carrito está vacío" });
                }

                // ✅ VALIDAR QUE TODOS LOS ITEMS TENGAN DATOS VÁLIDOS
                var itemsValidos = carritoItems.Where(item =>
                    item.Id > 0 &&
                    item.Cantidad > 0 &&
                    item.Precio > 0 &&
                    !string.IsNullOrEmpty(item.Nombre)
                ).ToList();

                if (!itemsValidos.Any())
                {
                    Console.WriteLine("[DEBUG] No hay items válidos en el carrito");
                    return Json(new { success = false, message = "No hay productos válidos en el carrito" });
                }

                // ✅ RECALCULAR SUBTOTALES PARA EVITAR NaN
                foreach (var item in itemsValidos)
                {
                    item.Subtotal = item.Precio * item.Cantidad;
                    Console.WriteLine($"[DEBUG] Item: {item.Nombre}, Precio: {item.Precio}, Cantidad: {item.Cantidad}, Subtotal: {item.Subtotal}");
                }

                var total = itemsValidos.Sum(item => item.Subtotal);
                Console.WriteLine($"[DEBUG] Total calculado: {total}");

                // ✅ VALIDAR QUE EL TOTAL SEA VÁLIDO
                if (total <= 0)
                {
                    return Json(new { success = false, message = "El total del pedido no es válido" });
                }

                // ✅ USAR SUCURSAL DE TEMPDATA SI EXISTE
                int? sucursalId = TempData["SucursalId"] as int?;
                var sucursal = sucursalId.HasValue
                    ? await _context.Sucursales.FindAsync(sucursalId.Value)
                    : await _context.Sucursales.FirstOrDefaultAsync();

                if (sucursal == null)
                {
                    return Json(new { success = false, message = "No hay sucursales disponibles" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var pedido = new Pedido
                {
                    UsuarioId = userId,
                    SucursalId = sucursal.Id,
                    TipoServicio = request.TipoServicio,
                    Comentario = request.Observaciones,
                    Fecha = DateTime.Now,
                    Estado = "Preparándose",
                    Total = total,
                    Detalles = itemsValidos.Select(item => new PedidoDetalle
                    {
                        ProductoId = item.Id,
                        PrecioUnitario = item.Precio,
                        Cantidad = item.Cantidad,
                        IngredientesRemovidos = "[]",
                        NotasEspeciales = item.NotasEspeciales ?? "Pedido por recomendación IA"
                    }).ToList()
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // ✅ AGREGAR PUNTOS AL USUARIO CON VALIDACIÓN
                if (User.Identity.IsAuthenticated && total > 0)
                {
                    await AgregarPuntosAUsuarioRecomendacion(userId, total);
                }

                LimpiarCarritoRecomendacion();

                Console.WriteLine($"[DEBUG] Pedido creado exitosamente: ID {pedido.Id}");

                return Json(new
                {
                    success = true,
                    pedidoId = pedido.Id,
                    total = total,
                    puntosGanados = (int)(total * 30)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en ProcesarPedido: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Error interno del servidor: " + ex.Message });
            }
        }

        private async Task AgregarPuntosAUsuarioRecomendacion(string usuarioId, decimal totalPedido)
        {
            if (string.IsNullOrEmpty(usuarioId)) return;

            var usuario = await _context.AppUsuario.FindAsync(usuarioId);
            if (usuario == null) return;

            int puntosGanados = (int)(totalPedido * 30);
            usuario.PuntosFidelidad = (usuario.PuntosFidelidad ?? 0) + puntosGanados;

            var transaccion = new TransaccionPuntos
            {
                UsuarioId = usuarioId,
                Puntos = puntosGanados,
                Tipo = "Ganancia",
                Descripcion = $"Puntos ganados por pedido de recomendación IA - Total: ${totalPedido:F2}",
                Fecha = DateTime.Now
            };

            _context.TransaccionesPuntos.Add(transaccion);
            await _context.SaveChangesAsync();
        }

        private void SetCarritoRecomendacion(List<ItemCarritoPersonalizado> carrito)
        {
            try
            {
                var carritoJson = JsonSerializer.Serialize(carrito);
                HttpContext.Session.SetString("CarritoRecomendacion", carritoJson);
            }
            catch
            {
                // Error al guardar
            }
        }

        // ✅ AGREGAR ESTE MÉTODO EN AMBOS CONTROLADORES (PersonalizacionController y MenuRecomendacionController)

        private async Task<(int count, List<Pedido> pedidosActivos)> ContarPedidosActivos(string usuarioId)
        {
            if (string.IsNullOrEmpty(usuarioId))
                return (0, new List<Pedido>());

            var pedidosActivos = await _context.Pedidos
                .Where(p => p.UsuarioId == usuarioId &&
                           (p.Estado == "Preparándose" || p.Estado == "Listo para entregar"))
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            return (pedidosActivos.Count, pedidosActivos);
        }

        // ✅ MÉTODO PARA CREAR EL VIEWMODEL DE LÍMITE
        private LimiteAlcanzadoViewModel CrearViewModelLimite(int countActivos, List<Pedido> pedidosActivos)
        {
            return new LimiteAlcanzadoViewModel
            {
                PedidosActivos = countActivos,
                LimiteMaximo = 3,
                PedidosPendientes = pedidosActivos.Select(p => new ProyectoIdentity.Controllers.PedidoPendienteInfo
                {
                    Id = p.Id,
                    Fecha = p.Fecha,
                    Total = p.Total,
                    Estado = p.Estado,
                    TipoServicio = p.TipoServicio
                }).ToList()
            };
        }
        private void LimpiarCarritoRecomendacion()
        {
            HttpContext.Session.Remove("CarritoRecomendacion");
            Console.WriteLine("[DEBUG] Carrito limpiado de la sesión");
        }

        // ✅ MÉTODO PARA DEBUG - TEMPORAL
        [HttpGet]
        public IActionResult DebugCarrito()
        {
            var carrito = GetCarritoRecomendacion();
            return Json(new
            {
                success = true,
                carrito = carrito,
                count = carrito.Count,
                total = carrito.Sum(c => c.Subtotal),
                sessionId = HttpContext.Session.Id
            });
        }

        // AGREGAR ESTOS MÉTODOS AL FINAL DEL MenuRecomendacionController EXISTENTE

        // 1. SELECCIONAR PUNTO DE RECOLECCIÓN
        public async Task<IActionResult> SeleccionarRecoleccion(int productoId)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) return RedirectToAction("Recomendacion");

            // Solo usar el punto con ID=1
            var puntos = await _context.CollectionPoints
                .Include(cp => cp.Sucursal)
                .Where(cp => cp.Id == 1)
                .ToListAsync();

            ViewBag.ProductoId = productoId;
            ViewBag.ProductoNombre = producto.Nombre;

            return View(puntos);
        }

        // 2. CONFIRMAR PUNTO DE RECOLECCIÓN - RECIBE DATOS DE GEOLOCALIZACIÓN
        [HttpPost]
        public async Task<IActionResult> ConfirmarRecoleccion(int id, int productoId, double userLat = -0.1857, double userLng = -78.4954, double distancia = 0)
        {
            Console.WriteLine($"[DEBUG] Valores recibidos:");
            Console.WriteLine($"[DEBUG] userLat: {userLat}");
            Console.WriteLine($"[DEBUG] userLng: {userLng}");
            Console.WriteLine($"[DEBUG] distancia: {distancia}");
            var punto = await _context.CollectionPoints
                .Include(cp => cp.Sucursal)
                .FirstOrDefaultAsync(cp => cp.Id == id);

            if (punto == null) return RedirectToAction("SeleccionarRecoleccion", new { productoId });

            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) return RedirectToAction("Recomendacion");

            // ✅ USAR LA DISTANCIA QUE VIENE DEL FORM, NO CALCULAR DE NUEVO
            // NO CALCULAR NADA, SOLO USAR LO QUE VIENE

            // GUARDAR INFO DE SUCURSAL PARA USAR EN CONFIRMACIÓN
            TempData["SucursalSeleccionada"] = punto.Sucursal.Nombre;
            TempData["DireccionSeleccionada"] = punto.Address;
            TempData["SucursalId"] = punto.Sucursal.Id;

            // ✅ PASAR LA DISTANCIA TAL COMO VIENE
            ViewBag.ProductoId = productoId;
            ViewBag.ProductoNombre = producto.Nombre;
            ViewBag.PuntoRecoleccionId = id;
            if (distancia <= 0 || distancia > 100) // Si la distancia es sospechosa
            {
                Console.WriteLine("[DEBUG] Distancia sospechosa, recalculando...");
                distancia = CalcularDistancia(userLat, userLng,
                    (double)punto.Sucursal.Latitud, (double)punto.Sucursal.Longitud);
                Console.WriteLine($"[DEBUG] Distancia recalculada: {distancia}");
            }
            ViewBag.Distancia = distancia; // ✅ LA MISMA QUE VIENE DEL FORM

            return View(punto);
        }

        [HttpPost]
        public IActionResult ActualizarCarrito([FromBody] List<ItemCarritoPersonalizado> carrito)
        {
            try
            {
                Console.WriteLine($"[DEBUG] ActualizarCarrito - Recibidos {carrito.Count} items");

                // Validar items antes de guardar
                foreach (var item in carrito)
                {
                    if (item.Subtotal <= 0 && item.Precio > 0 && item.Cantidad > 0)
                    {
                        item.Subtotal = item.Precio * item.Cantidad;
                    }
                }

                SetCarritoRecomendacion(carrito);
                return Json(new { success = true, totalItems = carrito.Sum(c => c.Cantidad) });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error en ActualizarCarrito: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 3. REDIRIGIR AUTOMÁTICAMENTE AL DETALLE CON EL ID
        // ✅ REEMPLAZAR el método ContinuarConDetalle existente
        [HttpPost]
        public async Task<IActionResult> ContinuarConDetalle(int productoId, int puntoRecoleccionId)
        {
            // MANTENER LA INFO DE SUCURSAL EN TEMPDATA para el carrito
            var punto = await _context.CollectionPoints
                .Include(cp => cp.Sucursal)
                .FirstOrDefaultAsync(cp => cp.Id == puntoRecoleccionId);

            if (punto != null)
            {
                TempData["SucursalSeleccionada"] = punto.Sucursal.Nombre;
                TempData["DireccionSeleccionada"] = punto.Address;
                TempData["SucursalId"] = punto.Sucursal.Id;

                // ✅ MANTENER ESTOS DATOS PARA MÚLTIPLES REQUESTS
                TempData.Keep("SucursalSeleccionada");
                TempData.Keep("DireccionSeleccionada");
                TempData.Keep("SucursalId");
            }

            // ✅ REDIRIGIR AL DETALLE CON EL ID DEL PRODUCTO SELECCIONADO
            // Esto permite al usuario ver los puntos y agregar al carrito
            return RedirectToAction("Detalle", new { id = productoId });
        }

        // 4. MÉTODO AUXILIAR PARA CALCULAR DISTANCIA
        private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radio de la Tierra en kilómetros

            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        private async Task<(bool permitido, int productosActuales, string mensaje)> ValidarLimiteProductos(string usuarioId)
        {
            if (string.IsNullOrEmpty(usuarioId))
                return (true, 0, "");

            // Obtener todos los pedidos activos del usuario
            var pedidosActivos = await _context.Pedidos
                .Where(p => p.UsuarioId == usuarioId &&
                           (p.Estado == "Preparándose" || p.Estado == "Listo para entregar"))
                .ToListAsync();

            int totalProductos = 0;

            // Contar productos en cada pedido
            foreach (var pedido in pedidosActivos)
            {
                // Contar en Detalles (personalización) - Cantidad es INT
                var detalles = await _context.PedidoDetalles
                    .Where(d => d.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += detalles.Sum(d => d.Cantidad); // ✅ SIN ?? porque es int

                // Contar en PedidoProductos (pedidos normales) - Cantidad es INT?
                var productos = await _context.PedidoProductos
                    .Where(pp => pp.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += productos.Sum(pp => pp.Cantidad ?? 0); // ✅ CON ?? porque es int?
            }

            if (totalProductos >= 3)
            {
                return (false, totalProductos, $"Ya tienes {totalProductos}/3 productos en pedidos activos. Espera a que se entreguen para pedir más.");
            }

            return (true, totalProductos, "");
        }

        private async Task<(bool permitido, int productosDisponibles, string mensaje)> ValidarAgregarProductos(string usuarioId, int cantidadAAgregar)
        {
            var (permitido, productosActuales, mensaje) = await ValidarLimiteProductos(usuarioId);

            if (!permitido)
                return (false, 0, mensaje);

            int productosDisponibles = 3 - productosActuales;

            if (cantidadAAgregar > productosDisponibles)
            {
                return (false, productosDisponibles,
                    $"Solo puedes agregar {productosDisponibles} producto(s) más. Actualmente tienes {productosActuales}/3 productos en pedidos activos.");
            }

            return (true, productosDisponibles, "");
        }

        // ✅ MÉTODO UNIFICADO PARA CONTAR TODOS LOS PRODUCTOS
        private async Task<(int totalProductos, List<Pedido> pedidosActivos)> ContarTodosLosProductosActivos(string usuarioId)
        {
            if (string.IsNullOrEmpty(usuarioId))
                return (0, new List<Pedido>());

            var pedidosActivos = await _context.Pedidos
                .Where(p => p.UsuarioId == usuarioId &&
                           (p.Estado == "Entregado"))
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            int totalProductos = 0;

            // ✅ CONTAR PRODUCTOS DE AMBOS TIPOS DE PEDIDOS
            foreach (var pedido in pedidosActivos)
            {
                // Productos de personalización (PedidoDetalles)
                var detalles = await _context.PedidoDetalles
                    .Where(d => d.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += detalles.Sum(d => d.Cantidad);

                // Productos de pedidos normales (PedidoProductos)
                var productos = await _context.PedidoProductos
                    .Where(pp => pp.PedidoId == pedido.Id)
                    .ToListAsync();
                totalProductos += productos.Sum(pp => pp.Cantidad ?? 0);
            }

            Console.WriteLine($"[DEBUG] Usuario {usuarioId}: {totalProductos} productos activos en {pedidosActivos.Count} pedidos");

            return (totalProductos, pedidosActivos);
        }

        // ✅ MÉTODO UNIFICADO PARA CONTAR PRODUCTOS EN CARRITOS
        private int ContarProductosEnCarritos(string usuarioId)
        {
            int productosEnCarritos = 0;

            try
            {
                // Productos en carrito de personalización
                var carritoPersonalizacion = GetCarritoPersonalizacion();
                productosEnCarritos += carritoPersonalizacion?.Sum(c => c.Cantidad) ?? 0;

                // Productos en carrito de recomendación IA
                var carritoRecomendacion = GetCarritoRecomendacion();
                productosEnCarritos += carritoRecomendacion?.Sum(c => c.Cantidad) ?? 0;

                Console.WriteLine($"[DEBUG] Productos en carritos: Personalización={carritoPersonalizacion?.Sum(c => c.Cantidad) ?? 0}, Recomendación={carritoRecomendacion?.Sum(c => c.Cantidad) ?? 0}, Total={productosEnCarritos}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al contar productos en carritos: {ex.Message}");
            }

            return productosEnCarritos;
        }

        // ✅ MÉTODO UNIFICADO PARA VALIDAR LÍMITES GLOBALES
        private async Task<(bool permitido, int productosActivos, int productosCarritos, int disponibles, string mensaje)> ValidarLimitesGlobales(string usuarioId)
        {
            const int LIMITE_MAXIMO = 3;

            if (string.IsNullOrEmpty(usuarioId))
                return (true, 0, 0, LIMITE_MAXIMO, "");

            // ✅ SOLO CONTAR PRODUCTOS EN PEDIDOS ENTREGADOS/ACTIVOS REALES
            var pedidosActivos = await _context.Pedidos
            .Where(p => p.UsuarioId == usuarioId && (p.Estado == "Preparándose" || p.Estado == "Listo para entregar"))
            .ToListAsync();

            int productosActivos = 0;
            foreach (var pedido in pedidosActivos)
            {
                var detalles = await _context.PedidoDetalles.Where(d => d.PedidoId == pedido.Id).ToListAsync();
                productosActivos += detalles.Sum(d => d.Cantidad);

                var productos = await _context.PedidoProductos.Where(pp => pp.PedidoId == pedido.Id).ToListAsync();
                productosActivos += productos.Sum(pp => pp.Cantidad ?? 0);
            }

            // ✅ CONTAR PRODUCTOS SOLO DEL CARRITO ACTUAL (NO AMBOS)
            int productosCarritos = 0;
            // NO contar productos de carritos aquí - solo en el momento de procesar pedido

            int disponibles = Math.Max(0, LIMITE_MAXIMO - productosActivos);
            bool permitido = disponibles > 0;

            string mensaje = "";
            if (!permitido)
            {
                mensaje = $"Tienes {productosActivos} productos en pedidos activos. Espera a que se entreguen.";
            }

            return (permitido, productosActivos, productosCarritos, disponibles, mensaje);
        }
        // ✅ MÉTODO UNIFICADO PARA VALIDAR AGREGAR PRODUCTOS
        private async Task<(bool permitido, int disponibles, string mensaje)> ValidarAgregarProductosUnificado(string usuarioId, int cantidadAAgregar)
        {
            var (permitidoGlobal, productosActivos, productosCarritos, disponibles, mensajeGlobal) = await ValidarLimitesGlobales(usuarioId);

            if (!permitidoGlobal)
                return (false, 0, mensajeGlobal);

            if (cantidadAAgregar > disponibles)
            {
                string mensaje = $"Solo puedes agregar {disponibles} producto(s) más. " +
                                $"Límite: 3 productos total. " +
                                $"Actualmente: {productosActivos} activos + {productosCarritos} en carritos = {productosActivos + productosCarritos}/3";
                return (false, disponibles, mensaje);
            }

            return (true, disponibles, "");
        }

        // ✅ REEMPLAZAR EL MÉTODO ObtenerLimitesProductos EN AMBOS CONTROLADORES
        [HttpGet]
        public async Task<IActionResult> ObtenerLimitesProductos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new
                {
                    productosActivos = 0,
                    limite = 3,
                    disponibles = 3,
                    productosEnCarritos = 0,
                    totalOcupados = 0,
                    mensaje = ""
                });
            }

            var (permitido, productosActivos, productosCarritos, disponibles, mensaje) = await ValidarLimitesGlobales(userId);

            return Json(new
            {
                productosActivos = productosActivos,
                limite = 3,
                disponibles = disponibles,
                productosEnCarritos = productosCarritos,
                totalOcupados = productosActivos + productosCarritos,
                mensaje = mensaje,
                permitido = permitido
            });
        }

        // ✅ MÉTODOS AUXILIARES PARA CARRITOS (AGREGAR SOLO SI NO EXISTEN)
        private List<ItemCarritoPersonalizado> GetCarritoPersonalizacion()
        {
            try
            {
                var carritoJson = HttpContext.Session.GetString("CarritoPersonalizacion");
                if (string.IsNullOrEmpty(carritoJson))
                    return new List<ItemCarritoPersonalizado>();

                return JsonSerializer.Deserialize<List<ItemCarritoPersonalizado>>(carritoJson) ?? new List<ItemCarritoPersonalizado>();
            }
            catch
            {
                return new List<ItemCarritoPersonalizado>();
            }
        }

        private List<ItemCarritoPersonalizado> GetCarritoRecomendacion()
        {
            try
            {
                var carritoJson = HttpContext.Session.GetString("CarritoRecomendacion");
                if (string.IsNullOrEmpty(carritoJson))
                    return new List<ItemCarritoPersonalizado>();

                return JsonSerializer.Deserialize<List<ItemCarritoPersonalizado>>(carritoJson) ?? new List<ItemCarritoPersonalizado>();
            }
            catch
            {
                return new List<ItemCarritoPersonalizado>();
            }
        }


        public class PersonalizacionRequest
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; } = 1;
            public List<string> IngredientesRemovidos { get; set; } = new();
            public string? NotasEspeciales { get; set; }
        }

        public class PedidoRequest
        {
            public string TipoServicio { get; set; }
            public string Observaciones { get; set; }
        }

        //public class LimiteAlcanzadoViewModel
        //{
        //    public int PedidosActivos { get; set; }
        //    public int LimiteMaximo { get; set; }
        //    public string MensajeUnificado { get; set; } = "";
        //    public int ProductosEnPersonalizacion { get; set; }
        //    public int ProductosEnRecomendacion { get; set; }
        //    public List<PedidoPendienteInfo> PedidosPendientes { get; set; } = new();
        //    public string MensajePersonalizado =>
        //        $"Tienes {PedidosActivos} de {LimiteMaximo} productos activos. Espera a que se entreguen para hacer más pedidos.";
        //}

        public class PedidoPendienteInfo
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public decimal Total { get; set; }
            public string Estado { get; set; } = "";
            public string TipoServicio { get; set; } = "";

            public string FechaFormateada => Fecha.ToString("dd/MM/yyyy HH:mm");
            public string EstadoBadgeClass => Estado switch
            {
                "Preparándose" => "bg-warning",
                "Listo para entregar" => "bg-success",
                _ => "bg-secondary"
            };
        }
    }
}