using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ProyectoIdentity.Controllers
{
    [Authorize] // Requiere que el usuario esté logueado
    public class MenuRecomendacionController : Controller
    {
        public class Plato
        {
            public int Id { get; set; } // Agregar propiedad Id
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Calorias { get; set; }
            public string Categoria { get; set; }
            public string Ingredientes { get; set; }
            public double? Similitud { get; set; }
        }

        // Datos completos del menú (traducidos desde React) - AHORA CON IDs
        private readonly List<Plato> _data = new List<Plato>
        {
            // Pizzas
            new Plato { Id = 1, Nombre = "Margarita", Precio = 8, Calorias = 250, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, tomate cherry, albahaca" },
            new Plato { Id = 2, Nombre = "Pepperoni", Precio = 9, Calorias = 300, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, pepperoni" },
            new Plato { Id = 3, Nombre = "Hawaiana", Precio = 11, Calorias = 320, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, jamón, piña" },
            new Plato { Id = 4, Nombre = "Veggie Lovers", Precio = 12, Calorias = 260, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, pimientos, champiñones, cebolla" },
            new Plato { Id = 5, Nombre = "Mi Champ", Precio = 8, Calorias = 270, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, champiñones" },
            new Plato { Id = 6, Nombre = "Cheddar", Precio = 10, Calorias = 330, Categoria = "Pizzas", Ingredientes = "Queso cheddar, queso mozzarella" },
            new Plato { Id = 7, Nombre = "Diavola", Precio = 10, Calorias = 310, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, salami picante, aceite de chile" },
            new Plato { Id = 8, Nombre = "Meat Lovers", Precio = 10, Calorias = 350, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, pepperoni, jamón, carne molida, tocino" },
            new Plato { Id = 9, Nombre = "Say Cheese", Precio = 12, Calorias = 360, Categoria = "Pizzas", Ingredientes = "Mezcla de quesos: mozzarella, cheddar, parmesano, gorgonzola" },
            new Plato { Id = 10, Nombre = "Verace", Precio = 12, Calorias = 290, Categoria = "Pizzas", Ingredientes = "Queso mozzarella de búfala, tomate cherry, albahaca, aceite de oliva extra virgen" },
            
            // Sánduches
            new Plato { Id = 11, Nombre = "Tradicional", Precio = 5, Calorias = 300, Categoria = "Sanduches", Ingredientes = "Jamón, queso, tomate, lechuga" },
            new Plato { Id = 12, Nombre = "Carne Mechada", Precio = 5, Calorias = 350, Categoria = "Sanduches", Ingredientes = "Carne desmenuzada, queso, cebolla caramelizada" },
            new Plato { Id = 13, Nombre = "Veggie", Precio = 5, Calorias = 220, Categoria = "Sanduches", Ingredientes = "Queso, tomate, lechuga, pepino, palta" },
            
            // Picadas
            new Plato { Id = 14, Nombre = "Nachos Cheddar", Precio = 5, Calorias = 400, Categoria = "Picadas", Ingredientes = "Nachos, queso cheddar, jalapeños" },
            new Plato { Id = 15, Nombre = "Nachos Verace", Precio = 8, Calorias = 450, Categoria = "Picadas", Ingredientes = "Nachos, queso cheddar, guacamole, pico de gallo, crema agria" },
            new Plato { Id = 16, Nombre = "Bread Sticks", Precio = 5, Calorias = 280, Categoria = "Picadas", Ingredientes = "Masa de pizza, ajo, parmesano" },
            new Plato { Id = 17, Nombre = "Bread Sticks Verace", Precio = 8, Calorias = 310, Categoria = "Picadas", Ingredientes = "Masa de pizza artesanal, ajo asado, parmesano reggiano, salsa marinara" },
            
            // Bebidas
            new Plato { Id = 18, Nombre = "Agua sin gas", Precio = 1, Calorias = 0, Categoria = "Bebidas", Ingredientes = "Agua purificada" },
            new Plato { Id = 19, Nombre = "Agua mineral", Precio = 1.5m, Calorias = 0, Categoria = "Bebidas", Ingredientes = "Agua mineral con gas natural" },
            new Plato { Id = 20, Nombre = "Limonada", Precio = 3.5m, Calorias = 80, Categoria = "Bebidas", Ingredientes = "Limón, agua, azúcar" },
            new Plato { Id = 21, Nombre = "Limonada Rosa", Precio = 3.5m, Calorias = 90, Categoria = "Bebidas", Ingredientes = "Limón, agua, azúcar, frutos rojos" },
            new Plato { Id = 22, Nombre = "Té caliente", Precio = 1.5m, Calorias = 5, Categoria = "Bebidas", Ingredientes = "Té, agua caliente" },
            new Plato { Id = 23, Nombre = "Coca-Cola", Precio = 1.5m, Calorias = 140, Categoria = "Bebidas", Ingredientes = "Refresco carbonatado" },
            new Plato { Id = 24, Nombre = "Fanta", Precio = 1.5m, Calorias = 160, Categoria = "Bebidas", Ingredientes = "Refresco carbonatado sabor naranja" },
            new Plato { Id = 25, Nombre = "Fioravanti", Precio = 1.5m, Calorias = 150, Categoria = "Bebidas", Ingredientes = "Refresco carbonatado sabor fresa" },
            new Plato { Id = 26, Nombre = "Sprite", Precio = 1.5m, Calorias = 140, Categoria = "Bebidas", Ingredientes = "Refresco carbonatado sabor lima limón" },
            new Plato { Id = 27, Nombre = "Café americano", Precio = 1.5m, Calorias = 5, Categoria = "Bebidas", Ingredientes = "Café, agua caliente" },
            new Plato { Id = 28, Nombre = "Capuccino", Precio = 2.5m, Calorias = 120, Categoria = "Bebidas", Ingredientes = "Café espresso, leche espumada" },
            new Plato { Id = 29, Nombre = "Iced Coffee", Precio = 3.5m, Calorias = 90, Categoria = "Bebidas", Ingredientes = "Café, hielo, leche, azúcar" },
            
            // Promos
            new Plato { Id = 30, Nombre = "Promo Pilas", Precio = 16, Calorias = 750, Categoria = "Promos", Ingredientes = "Pizza mediana, 2 bebidas, postre" },
            new Plato { Id = 31, Nombre = "Promo Lovers", Precio = 20, Calorias = 1000, Categoria = "Promos", Ingredientes = "Pizza grande, 2 bebidas, postre, pan de ajo" },
            new Plato { Id = 32, Nombre = "Promo King", Precio = 24, Calorias = 1500, Categoria = "Promos", Ingredientes = "Pizza familiar, 4 bebidas, 2 postres, 2 panes de ajo" },
            new Plato { Id = 33, Nombre = "Promo Sanduchera", Precio = 10, Calorias = 650, Categoria = "Promos", Ingredientes = "2 sánduches a elección, 2 bebidas" },
            new Plato { Id = 34, Nombre = "Promo Piqueo", Precio = 18, Calorias = 1200, Categoria = "Promos", Ingredientes = "Nachos cheddar, bread sticks, 4 bebidas" },
            
            // Cerveza
            new Plato { Id = 35, Nombre = "Jarro Cerveza", Precio = 4, Calorias = 150, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Id = 36, Nombre = "Pinta Cerveza", Precio = 6, Calorias = 200, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Id = 37, Nombre = "Litro Cerveza", Precio = 12, Calorias = 400, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Id = 38, Nombre = "Growler", Precio = 15, Calorias = 1200, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Id = 39, Nombre = "Stella Artois", Precio = 5, Calorias = 150, Categoria = "Cerveza", Ingredientes = "Cerveza lager premium" },
            new Plato { Id = 40, Nombre = "Corona", Precio = 5, Calorias = 140, Categoria = "Cerveza", Ingredientes = "Cerveza lager mexicana" },
            new Plato { Id = 41, Nombre = "Pilsener", Precio = 4, Calorias = 150, Categoria = "Cerveza", Ingredientes = "Cerveza lager nacional" },
            new Plato { Id = 42, Nombre = "Club", Precio = 4.5m, Calorias = 150, Categoria = "Cerveza", Ingredientes = "Cerveza lager premium nacional" },
            new Plato { Id = 43, Nombre = "Michelada Clásica", Precio = 1.5m, Calorias = 180, Categoria = "Cerveza", Ingredientes = "Cerveza, limón, sal, salsa" },
            new Plato { Id = 44, Nombre = "Michelada Maracuyá", Precio = 1.5m, Calorias = 190, Categoria = "Cerveza", Ingredientes = "Cerveza, limón, sal, salsa, jugo de maracuyá" },
            new Plato { Id = 45, Nombre = "3 jarros cerveza artesanal", Precio = 10, Calorias = 450, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Id = 46, Nombre = "3 pintas cualquier estilo", Precio = 15, Calorias = 600, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Id = 47, Nombre = "3 Stella Artois / Corona", Precio = 20, Calorias = 450, Categoria = "Cerveza", Ingredientes = "Cerveza premium" },
            new Plato { Id = 48, Nombre = "Combo 3 Pilsener", Precio = 10, Calorias = 450, Categoria = "Cerveza", Ingredientes = "Cerveza lager nacional" },
            new Plato { Id = 49, Nombre = "Combo 3 Club", Precio = 12, Calorias = 450, Categoria = "Cerveza", Ingredientes = "Cerveza lager premium nacional" },
            
            // Cocteles (lista parcial por espacio)
            new Plato { Id = 50, Nombre = "Copa de vino tinto", Precio = 5, Calorias = 120, Categoria = "Cocteles", Ingredientes = "Vino tinto" },
            new Plato { Id = 51, Nombre = "Copa de calimotcho", Precio = 6, Calorias = 150, Categoria = "Cocteles", Ingredientes = "Vino tinto, coca-cola" },
            new Plato { Id = 52, Nombre = "Copa de tinto de verano", Precio = 6, Calorias = 130, Categoria = "Cocteles", Ingredientes = "Vino tinto, limón, gaseosa" },
            new Plato { Id = 53, Nombre = "Mojito", Precio = 6, Calorias = 210, Categoria = "Cocteles", Ingredientes = "Ron, lima, azúcar, hierbabuena, soda" },
            new Plato { Id = 54, Nombre = "Margarita clásica", Precio = 6, Calorias = 220, Categoria = "Cocteles", Ingredientes = "Tequila, triple sec, lima" },
            
            // Shots
            new Plato { Id = 55, Nombre = "Shot de tequila", Precio = 3, Calorias = 100, Categoria = "Shots", Ingredientes = "Tequila" },
            new Plato { Id = 56, Nombre = "Shot de aguardiente", Precio = 3, Calorias = 110, Categoria = "Shots", Ingredientes = "Aguardiente" },
            new Plato { Id = 57, Nombre = "Shot de Jager", Precio = 6, Calorias = 110, Categoria = "Shots", Ingredientes = "Jagermeister" },
            new Plato { Id = 58, Nombre = "Jager Bomb", Precio = 10, Calorias = 240, Categoria = "Shots", Ingredientes = "Jagermeister, bebida energética" }
        };

        // Estructura TF-IDF
        private class TFIDFData
        {
            public List<double[]> VectoresTFIDF { get; set; }
            public List<string> Terminos { get; set; }
        }

        // Vista principal
        public IActionResult Recomendacion()
        {
            // Obtener todas las categorías distintas
            var categorias = _data.Select(p => p.Categoria).Distinct().ToList();
            ViewBag.Categorias = categorias;
            return View();
        }

        // Método para obtener recomendaciones
        [HttpPost]
        public IActionResult ObtenerRecomendaciones(string categoria, decimal presupuesto, string ingredientes)
        {
            try
            {
                // Logging para debug
                Console.WriteLine($"Categoría: {categoria}, Presupuesto: {presupuesto}, Ingredientes: {ingredientes}");

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
                            var indice = _data.FindIndex(item => item.Id == plato.Id); // Usar ID en lugar de nombre
                            if (indice != -1 && indice < tfidfData.VectoresTFIDF.Count)
                            {
                                var similitud = SimilitudCoseno(vectorConsulta, tfidfData.VectoresTFIDF[indice]);
                                platosConSimilitud.Add(new Plato
                                {
                                    Id = plato.Id, // Mantener el ID original
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
                    id = p.Id, // Incluir el ID real
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

        public IActionResult Detalle(int id)
        {
            try
            {
                // Buscar el plato por ID
                var plato = _data.FirstOrDefault(p => p.Id == id);
                if (plato == null)
                {
                    return NotFound();
                }

                ViewBag.IndiceProducto = id; // Pasar el ID para JavaScript
                return View(plato);
            }
            catch (Exception ex)
            {
                // Log del error si es necesario
                return View("Error");
            }
        }
    }
}