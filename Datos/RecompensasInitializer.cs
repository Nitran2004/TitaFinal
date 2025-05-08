using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProyectoIdentity.Datos;  // Ajusta este namespace según tu proyecto
using ProyectoIdentity.Models;  // Ajusta este namespace según tu proyecto

namespace ProyectoIdentity.Datos  // Ajusta este namespace según tu proyecto
{
    public static class RecompensasInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Verificar si ya existen registros
            if (context.ProductosRecompensa.Any())
            {
                return; // Base de datos ya inicializada con productos de recompensa
            }

            var productosRecompensa = new List<ProductoRecompensa>
            {
                // BEBIDAS - Bajo costo, ideal para recompensas iniciales
                new ProductoRecompensa {
                    ProductoId = 1, // Asignar ID correspondiente a "Agua sin gas"
                    Nombre = "Agua sin gas",
                    PrecioOriginal = 1.00m,
                    PuntosNecesarios = 200,
                    Imagen = "~/images1/aguasingas.jpg",
                    Categoria = "Bebidas"
                },

                new ProductoRecompensa {
                    ProductoId = 2,
                    Nombre = "Limonada",
                    PrecioOriginal = 2.00m,
                    PuntosNecesarios = 350,
                    Imagen = "~/images1/imperial.jpg",
                    Categoria = "Bebidas"
                },

                new ProductoRecompensa {
                    ProductoId = 3,
                    Nombre = "Café americano",
                    PrecioOriginal = 1.50m,
                    PuntosNecesarios = 300,
                    Imagen = "~/images1/americano.jpg",
                    Categoria = "Bebidas"
                },
                
                // CERVEZAS - Recompensas de nivel medio
                new ProductoRecompensa {
                    ProductoId = 4,
                    Nombre = "Jarro",
                    PrecioOriginal = 4.00m,
                    PuntosNecesarios = 700,
                    Imagen = "~/images1/jarro.jpg",
                    Categoria = "Cerveza"
                },

                new ProductoRecompensa {
                    ProductoId = 5,
                    Nombre = "Pilsener",
                    PrecioOriginal = 4.00m,
                    PuntosNecesarios = 700,
                    Imagen = "~/images1/pilsener1.jpg",
                    Categoria = "Cerveza"
                },
                
                // SHOTS - Opciones atractivas con costo moderado
                new ProductoRecompensa {
                    ProductoId = 6,
                    Nombre = "Shot de aguardiente",
                    PrecioOriginal = 3.00m,
                    PuntosNecesarios = 500,
                    Imagen = "~/images1/shotardiente.jpg",
                    Categoria = "Shot"
                },

                new ProductoRecompensa {
                    ProductoId = 7,
                    Nombre = "Shot de tequila",
                    PrecioOriginal = 3.00m,
                    PuntosNecesarios = 500,
                    Imagen = "~/images1/shottequila.jpg",
                    Categoria = "Shot"
                },
                
                // SÁNDUCHES - Opciones de comida de nivel medio
                new ProductoRecompensa {
                    ProductoId = 8,
                    Nombre = "Tradicional",
                    PrecioOriginal = 5.00m,
                    PuntosNecesarios = 800,
                    Imagen = "~/images1/sanduchesp1.jpg",
                    Categoria = "Sánduches"
                },

                new ProductoRecompensa {
                    ProductoId = 9,
                    Nombre = "Veggie",
                    PrecioOriginal = 5.00m,
                    PuntosNecesarios = 800,
                    Imagen = "~/images1/veggie.webp",
                    Categoria = "Sánduches"
                },
                
                // PICADAS - Opciones para compartir
                new ProductoRecompensa {
                    ProductoId = 10,
                    Nombre = "Nachos Cheddar",
                    PrecioOriginal = 5.00m,
                    PuntosNecesarios = 850,
                    Imagen = "~/images1/cheddar.jpg",
                    Categoria = "Picadas"
                },

                new ProductoRecompensa {
                    ProductoId = 11,
                    Nombre = "Bread Sticks",
                    PrecioOriginal = 5.00m,
                    PuntosNecesarios = 850,
                    Imagen = "~/images1/stciks.jpg",
                    Categoria = "Picadas"
                },
                
                // PIZZAS - Recompensas más costosas para usuarios frecuentes
                new ProductoRecompensa {
                    ProductoId = 12,
                    Nombre = "Pepperoni",
                    PrecioOriginal = 8.00m,
                    PuntosNecesarios = 1400,
                    Imagen = "~/images1/pexels-muffin-1653877.jpg",
                    Categoria = "Pizza"
                },

                new ProductoRecompensa {
                    ProductoId = 13,
                    Nombre = "Hawaiana",
                    PrecioOriginal = 8.00m,
                    PuntosNecesarios = 1400,
                    Imagen = "~/images1/pexels-brettjordan-842519.jpg",
                    Categoria = "Pizza"
                },

                new ProductoRecompensa {
                    ProductoId = 14,
                    Nombre = "Margarita",
                    PrecioOriginal = 8.00m,
                    PuntosNecesarios = 1400,
                    Imagen = "~/images1/marga.jpg",
                    Categoria = "Pizza"
                },
                
                // COCTELES - Opciones premium para usuarios frecuentes
                new ProductoRecompensa {
                    ProductoId = 15,
                    Nombre = "Cuba libre",
                    PrecioOriginal = 6.00m,
                    PuntosNecesarios = 1000,
                    Imagen = "~/images1/cubalibre.jpg",
                    Categoria = "Cocteles"
                },

                new ProductoRecompensa {
                    ProductoId = 16,
                    Nombre = "Mojito",
                    PrecioOriginal = 6.00m,
                    PuntosNecesarios = 1000,
                    Imagen = "~/images1/mojito.jpg",
                    Categoria = "Cocteles"
                },

                new ProductoRecompensa {
                    ProductoId = 17,
                    Nombre = "Gin Tonic",
                    PrecioOriginal = 6.00m,
                    PuntosNecesarios = 1000,
                    Imagen = "~/images1/gintonic.jpg",
                    Categoria = "Cocteles"
                },
                
                // COMBOS CERVEZA - Recompensas de alto nivel para clientes muy frecuentes
                new ProductoRecompensa {
                    ProductoId = 18,
                    Nombre = "3 jarros cerveza artesanal",
                    PrecioOriginal = 10.00m,
                    PuntosNecesarios = 1800,
                    Imagen = "~/images1/jarro1.jpg",
                    Categoria = "Cerveza"
                },

                new ProductoRecompensa {
                    ProductoId = 19,
                    Nombre = "Combo 3 Pilsener",
                    PrecioOriginal = 10.00m,
                    PuntosNecesarios = 1800,
                    Imagen = "~/images1/pilsener.jpg",
                    Categoria = "Cerveza"
                },
                
                // PROMO - Recompensa máxima para clientes muy fieles
                new ProductoRecompensa {
                    ProductoId = 20,
                    Nombre = "Promo Sanduchera",
                    PrecioOriginal = 10.00m,
                    PuntosNecesarios = 2000,
                    Imagen = "~/images1/278953595_514677723447399_1453067101951070993_n.webp",
                    Categoria = "Promo"
                }
            };

            context.ProductosRecompensa.AddRange(productosRecompensa);
            context.SaveChanges();
        }
    }
}