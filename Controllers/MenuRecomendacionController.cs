using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Globalization;

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
            // ✅ OBTENER CATEGORÍAS DESDE LA BD
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

        // ✅ DETALLE DESDE BD
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

                ViewBag.IndiceProducto = id;
                return View(plato);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Detalle: {ex.Message}");
                return View("Error");
            }
        }

        // AGREGAR ESTOS 3 MÉTODOS AL FINAL DEL MenuRecomendacionController EXISTENTE

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

        // 3. REDIRIGIR AUTOMÁTICAMENTE AL DETALLE CON EL ID
        [HttpPost]
        public async Task<IActionResult> ContinuarConDetalle(int productoId, int puntoRecoleccionId)
        {
            // MANTENER LA INFO DE SUCURSAL EN TEMPDATA
            var punto = await _context.CollectionPoints
                .Include(cp => cp.Sucursal)
                .FirstOrDefaultAsync(cp => cp.Id == puntoRecoleccionId);

            if (punto != null)
            {
                TempData["SucursalSeleccionada"] = punto.Sucursal.Nombre;
                TempData["DireccionSeleccionada"] = punto.Address;
                TempData["SucursalId"] = punto.Sucursal.Id;
            }

            // REDIRIGIR AL DETALLE CON EL ID DEL PRODUCTO SELECCIONADO
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
    }
}