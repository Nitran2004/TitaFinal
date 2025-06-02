using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ProyectoIdentity.Controllers
{
    public class MenuRecomendacionController : Controller
    {
        public class Plato
        {
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Calorias { get; set; }
            public string Categoria { get; set; }
            public string Ingredientes { get; set; }
            public double? Similitud { get; set; }
        }

        // Datos completos del menú (traducidos desde React)
        private readonly List<Plato> _data = new List<Plato>
        {
            // Pizzas
            new Plato { Nombre = "Margarita", Precio = 8, Calorias = 250, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, tomate cherry, albahaca" },
            new Plato { Nombre = "Pepperoni", Precio = 9, Calorias = 300, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, pepperoni" },
            new Plato { Nombre = "Hawaiana", Precio = 11, Calorias = 320, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, jamón, piña" },
            new Plato { Nombre = "Veggie Lovers", Precio = 12, Calorias = 260, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, pimientos, champiñones, cebolla" },
            new Plato { Nombre = "Mi Champ", Precio = 8, Calorias = 270, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, champiñones" },
            new Plato { Nombre = "Cheddar", Precio = 10, Calorias = 330, Categoria = "Pizzas", Ingredientes = "Queso cheddar, queso mozzarella" },
            new Plato { Nombre = "Diavola", Precio = 10, Calorias = 310, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, salami picante, aceite de chile" },
            new Plato { Nombre = "Meat Lovers", Precio = 10, Calorias = 350, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, pepperoni, jamón, carne molida, tocino" },
            new Plato { Nombre = "Say Cheese", Precio = 12, Calorias = 360, Categoria = "Pizzas", Ingredientes = "Mezcla de quesos: mozzarella, cheddar, parmesano, gorgonzola" },
            new Plato { Nombre = "Verace", Precio = 12, Calorias = 290, Categoria = "Pizzas", Ingredientes = "Queso mozzarella de búfala, tomate cherry, albahaca, aceite de oliva extra virgen" },
            
            // Sánduches
            new Plato { Nombre = "Tradicional", Precio = 5, Calorias = 300, Categoria = "Sanduches", Ingredientes = "Jamón, queso, tomate, lechuga" },
            new Plato { Nombre = "Carne Mechada", Precio = 5, Calorias = 350, Categoria = "Sanduches", Ingredientes = "Carne desmenuzada, queso, cebolla caramelizada" },
            new Plato { Nombre = "Veggie", Precio = 5, Calorias = 220, Categoria = "Sanduches", Ingredientes = "Queso, tomate, lechuga, pepino, palta" },
            
            // Picadas
            new Plato { Nombre = "Nachos Cheddar", Precio = 5, Calorias = 400, Categoria = "Picadas", Ingredientes = "Nachos, queso cheddar, jalapeños" },
            new Plato { Nombre = "Nachos Verace", Precio = 8, Calorias = 450, Categoria = "Picadas", Ingredientes = "Nachos, queso cheddar, guacamole, pico de gallo, crema agria" },
            new Plato { Nombre = "Bread Sticks", Precio = 5, Calorias = 280, Categoria = "Picadas", Ingredientes = "Masa de pizza, ajo, parmesano" },
            new Plato { Nombre = "Bread Sticks Verace", Precio = 8, Calorias = 310, Categoria = "Picadas", Ingredientes = "Masa de pizza artesanal, ajo asado, parmesano reggiano, salsa marinara" },
            
            // Bebidas
            new Plato { Nombre = "Agua sin gas", Precio = 1, Calorias = 0, Categoria = "Bebidas", Ingredientes = "Agua purificada" },
            new Plato { Nombre = "Agua mineral", Precio = 1.5m, Calorias = 0, Categoria = "Bebidas", Ingredientes = "Agua mineral con gas natural" },
            new Plato { Nombre = "Limonada", Precio = 3.5m, Calorias = 80, Categoria = "Bebidas", Ingredientes = "Limón, agua, azúcar" },
            new Plato { Nombre = "Limonada Rosa", Precio = 3.5m, Calorias = 90, Categoria = "Bebidas", Ingredientes = "Limón, agua, azúcar, frutos rojos" },
            new Plato { Nombre = "Té caliente", Precio = 1.5m, Calorias = 5, Categoria = "Bebidas", Ingredientes = "Té, agua caliente" },
            new Plato { Nombre = "Coca-Cola", Precio = 1.5m, Calorias = 140, Categoria = "Bebidas", Ingredientes = "Refresco carbonatado" },
            new Plato { Nombre = "Fanta", Precio = 1.5m, Calorias = 160, Categoria = "Bebidas", Ingredientes = "Refresco carbonatado sabor naranja" },
            new Plato { Nombre = "Fioravanti", Precio = 1.5m, Calorias = 150, Categoria = "Bebidas", Ingredientes = "Refresco carbonatado sabor fresa" },
            new Plato { Nombre = "Sprite", Precio = 1.5m, Calorias = 140, Categoria = "Bebidas", Ingredientes = "Refresco carbonatado sabor lima limón" },
            new Plato { Nombre = "Café americano", Precio = 1.5m, Calorias = 5, Categoria = "Bebidas", Ingredientes = "Café, agua caliente" },
            new Plato { Nombre = "Capuccino", Precio = 2.5m, Calorias = 120, Categoria = "Bebidas", Ingredientes = "Café espresso, leche espumada" },
            new Plato { Nombre = "Iced Coffee", Precio = 3.5m, Calorias = 90, Categoria = "Bebidas", Ingredientes = "Café, hielo, leche, azúcar" },
            
            // Promos
            new Plato { Nombre = "Promo Pilas", Precio = 16, Calorias = 750, Categoria = "Promos", Ingredientes = "Pizza mediana, 2 bebidas, postre" },
            new Plato { Nombre = "Promo Lovers", Precio = 20, Calorias = 1000, Categoria = "Promos", Ingredientes = "Pizza grande, 2 bebidas, postre, pan de ajo" },
            new Plato { Nombre = "Promo King", Precio = 24, Calorias = 1500, Categoria = "Promos", Ingredientes = "Pizza familiar, 4 bebidas, 2 postres, 2 panes de ajo" },
            new Plato { Nombre = "Promo Sanduchera", Precio = 10, Calorias = 650, Categoria = "Promos", Ingredientes = "2 sánduches a elección, 2 bebidas" },
            new Plato { Nombre = "Promo Piqueo", Precio = 18, Calorias = 1200, Categoria = "Promos", Ingredientes = "Nachos cheddar, bread sticks, 4 bebidas" },
            
            // Cerveza
            new Plato { Nombre = "Jarro Cerveza", Precio = 4, Calorias = 150, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Nombre = "Pinta Cerveza", Precio = 6, Calorias = 200, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Nombre = "Litro Cerveza", Precio = 12, Calorias = 400, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Nombre = "Growler", Precio = 15, Calorias = 1200, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Nombre = "Stella Artois", Precio = 5, Calorias = 150, Categoria = "Cerveza", Ingredientes = "Cerveza lager premium" },
            new Plato { Nombre = "Corona", Precio = 5, Calorias = 140, Categoria = "Cerveza", Ingredientes = "Cerveza lager mexicana" },
            new Plato { Nombre = "Pilsener", Precio = 4, Calorias = 150, Categoria = "Cerveza", Ingredientes = "Cerveza lager nacional" },
            new Plato { Nombre = "Club", Precio = 4.5m, Calorias = 150, Categoria = "Cerveza", Ingredientes = "Cerveza lager premium nacional" },
            new Plato { Nombre = "Michelada Clásica", Precio = 1.5m, Calorias = 180, Categoria = "Cerveza", Ingredientes = "Cerveza, limón, sal, salsa" },
            new Plato { Nombre = "Michelada Maracuyá", Precio = 1.5m, Calorias = 190, Categoria = "Cerveza", Ingredientes = "Cerveza, limón, sal, salsa, jugo de maracuyá" },
            new Plato { Nombre = "3 jarros cerveza artesanal", Precio = 10, Calorias = 450, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Nombre = "3 pintas cualquier estilo", Precio = 15, Calorias = 600, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Nombre = "3 Stella Artois / Corona", Precio = 20, Calorias = 450, Categoria = "Cerveza", Ingredientes = "Cerveza premium" },
            new Plato { Nombre = "Combo 3 Pilsener", Precio = 10, Calorias = 450, Categoria = "Cerveza", Ingredientes = "Cerveza lager nacional" },
            new Plato { Nombre = "Combo 3 Club", Precio = 12, Calorias = 450, Categoria = "Cerveza", Ingredientes = "Cerveza lager premium nacional" },
            
            // Cocteles (lista parcial por espacio)
            new Plato { Nombre = "Copa de vino tinto", Precio = 5, Calorias = 120, Categoria = "Cocteles", Ingredientes = "Vino tinto" },
            new Plato { Nombre = "Copa de calimotcho", Precio = 6, Calorias = 150, Categoria = "Cocteles", Ingredientes = "Vino tinto, coca-cola" },
            new Plato { Nombre = "Copa de tinto de verano", Precio = 6, Calorias = 130, Categoria = "Cocteles", Ingredientes = "Vino tinto, limón, gaseosa" },
            new Plato { Nombre = "Mojito", Precio = 6, Calorias = 210, Categoria = "Cocteles", Ingredientes = "Ron, lima, azúcar, hierbabuena, soda" },
            new Plato { Nombre = "Margarita clásica", Precio = 6, Calorias = 220, Categoria = "Cocteles", Ingredientes = "Tequila, triple sec, lima" },
            
            // Shots
            new Plato { Nombre = "Shot de tequila", Precio = 3, Calorias = 100, Categoria = "Shots", Ingredientes = "Tequila" },
            new Plato { Nombre = "Shot de aguardiente", Precio = 3, Calorias = 110, Categoria = "Shots", Ingredientes = "Aguardiente" },
            new Plato { Nombre = "Shot de Jager", Precio = 6, Calorias = 110, Categoria = "Shots", Ingredientes = "Jagermeister" },
            new Plato { Nombre = "Jager Bomb", Precio = 10, Calorias = 240, Categoria = "Shots", Ingredientes = "Jagermeister, bebida energética" }
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
                            var indice = _data.FindIndex(item => item.Nombre == plato.Nombre);
                            if (indice != -1 && indice < tfidfData.VectoresTFIDF.Count)
                            {
                                var similitud = SimilitudCoseno(vectorConsulta, tfidfData.VectoresTFIDF[indice]);
                                platosConSimilitud.Add(new Plato
                                {
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
                // Como usas una lista estática, buscamos por índice
                if (id < 0 || id >= _data.Count)
                {
                    return NotFound();
                }

                var plato = _data[id];
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