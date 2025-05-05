using Microsoft.AspNetCore.Mvc;

namespace ProyectoIdentity.Controllers
{
    public class RecomendacionController : Controller
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

        // Datos del menú
        private readonly List<Plato> _data = new List<Plato>
        {
            // Pizzas
            new Plato { Nombre = "Margarita", Precio = 8, Calorias = 250, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, tomate cherry, albahaca" },
            new Plato { Nombre = "Napolitana", Precio = 10, Calorias = 280, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, tomate, anchoas" },
            new Plato { Nombre = "Vegetariana", Precio = 12, Calorias = 240, Categoria = "Pizzas", Ingredientes = "Queso mozzarella, pimientos, champiñones, cebolla, aceitunas" },
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
            
            // Añade más categorías según sea necesario
            
            // Categoría Cerveza
            new Plato { Nombre = "Jarro Cerveza", Precio = 4, Calorias = 150, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            new Plato { Nombre = "Pinta Cerveza", Precio = 6, Calorias = 200, Categoria = "Cerveza", Ingredientes = "Cerveza artesanal" },
            
            // Categoría Promos
            new Plato { Nombre = "Promo Pilas", Precio = 16, Calorias = 750, Categoria = "Promos", Ingredientes = "Pizza mediana, 2 bebidas, postre" },
            new Plato { Nombre = "Promo Lovers", Precio = 20, Calorias = 1000, Categoria = "Promos", Ingredientes = "Pizza grande, 2 bebidas, postre, pan de ajo" }
            
            // Puedes añadir el resto de los datos del archivo original aquí
        };

        // Página principal
        public ActionResult Recomendacion()
        {
            // Obtener todas las categorías distintas
            var categorias = _data.Select(p => p.Categoria).Distinct().ToList();
            ViewBag.Categorias = categorias;
            return View();
        }

        // Método para manejar las recomendaciones
        [HttpPost]
        public ActionResult ObtenerRecomendaciones(string categoria, decimal presupuesto, string ingredientes)
        {
            try
            {
                // Filtrado inicial por precio y categoría
                var filtrado = _data.Where(item => item.Precio <= presupuesto).ToList();

                if (!string.IsNullOrEmpty(categoria) && categoria != "Cualquiera")
                {
                    filtrado = filtrado.Where(item => item.Categoria == categoria).ToList();
                }

                // Filtrado por preferencia de carne
                if (!string.IsNullOrEmpty(ingredientes))
                {
                    var ingredientesLower = ingredientes.ToLower();

                    if (ingredientesLower.Contains("sin carne"))
                    {
                        filtrado = filtrado.Where(item =>
                            !item.Ingredientes.ToLower().Contains("carne") &&
                            !item.Ingredientes.ToLower().Contains("jamón") &&
                            !item.Ingredientes.ToLower().Contains("pepperoni")
                        ).ToList();
                    }
                    else if (ingredientesLower.Contains("con carne"))
                    {
                        filtrado = filtrado.Where(item =>
                            item.Ingredientes.ToLower().Contains("carne") ||
                            item.Ingredientes.ToLower().Contains("jamón") ||
                            item.Ingredientes.ToLower().Contains("pepperoni")
                        ).ToList();
                    }
                    else
                    {
                        // Implementación simplificada de la similitud basada en ingredientes
                        var terminosConsulta = ingredientesLower.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var plato in filtrado)
                        {
                            var ingredientesPlato = plato.Ingredientes.ToLower();
                            int coincidencias = 0;

                            foreach (var termino in terminosConsulta)
                            {
                                if (ingredientesPlato.Contains(termino))
                                {
                                    coincidencias++;
                                }
                            }

                            // Calcular un valor de similitud simple (0 a 1)
                            plato.Similitud = terminosConsulta.Length > 0
                                ? (double)coincidencias / terminosConsulta.Length
                                : 0;
                        }

                        // Ordenar por similitud
                        filtrado = filtrado.OrderByDescending(p => p.Similitud).ToList();
                    }
                }

                // Tomar los 5 mejores resultados
                var recomendaciones = filtrado.Take(5).ToList();

                return Json(new { success = true, data = recomendaciones });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Pepperoni()
        {
            // Lógica para manejar la pizza Pepperoni
            return View(); // Redirige a la vista de recomendación para Pepperoni
        }

        public IActionResult Michamp()
        {
            // Lógica para manejar la pizza Mi Champ
            return View(); // Redirige a la vista de recomendación para Mi Champ
        }
    }
}