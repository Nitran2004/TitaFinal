using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Datos
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
                    ProductoId = 52, // Agua sin gas
                    Nombre = "Agua sin gas",
                    PrecioOriginal = 1.00m,
                    PuntosNecesarios = 200,
                    Categoria = "Bebidas"
                },

                new ProductoRecompensa {
                    ProductoId = 54, // Limonada
                    Nombre = "Limonada",
                    PrecioOriginal = 2.00m,
                    PuntosNecesarios = 350,
                    Categoria = "Bebidas"
                },

                new ProductoRecompensa {
                    ProductoId = 61, // Café americano
                    Nombre = "Café americano",
                    PrecioOriginal = 1.50m,
                    PuntosNecesarios = 300,
                    Categoria = "Bebidas"
                },
                
                // CERVEZAS - Recompensas de nivel medio
                new ProductoRecompensa {
                    ProductoId = 11, // Jarro
                    Nombre = "Jarro",
                    PrecioOriginal = 4.00m,
                    PuntosNecesarios = 700,
                    Categoria = "Cerveza"
                },

                new ProductoRecompensa {
                    ProductoId = 17, // Pilsener
                    Nombre = "Pilsener",
                    PrecioOriginal = 4.00m,
                    PuntosNecesarios = 700,
                    Categoria = "Cerveza"
                },
                
                // SHOTS - Opciones atractivas con costo moderado
                new ProductoRecompensa {
                    ProductoId = 73, // Shot de aguardiente
                    Nombre = "Shot de aguardiente",
                    PrecioOriginal = 3.00m,
                    PuntosNecesarios = 500,
                    Categoria = "Shot"
                },
                
                // SÁNDUCHES - Opciones de comida de nivel medio
                new ProductoRecompensa {
                    ProductoId = 69, // Tradicional
                    Nombre = "Tradicional",
                    PrecioOriginal = 5.00m,
                    PuntosNecesarios = 800,
                    Categoria = "Sánduches"
                },

                new ProductoRecompensa {
                    ProductoId = 71, // Veggie
                    Nombre = "Veggie",
                    PrecioOriginal = 5.00m,
                    PuntosNecesarios = 800,
                    Categoria = "Sánduches"
                },
                
                // PICADAS - Opciones para compartir
                new ProductoRecompensa {
                    ProductoId = 77, // Nachos Verace
                    Nombre = "Nachos Cheddar",
                    PrecioOriginal = 5.00m,
                    PuntosNecesarios = 850,
                    Categoria = "Picadas"
                },

                new ProductoRecompensa {
                    ProductoId = 11, // Bread Sticks (mismo que Jarro - verificar si es correcto)
                    Nombre = "Bread Sticks",
                    PrecioOriginal = 5.00m,
                    PuntosNecesarios = 850,
                    Categoria = "Picadas"
                },
                
                // PIZZAS - Recompensas más costosas para usuarios frecuentes
                new ProductoRecompensa {
                    ProductoId = 1, // Pepperoni
                    Nombre = "Pepperoni",
                    PrecioOriginal = 8.00m,
                    PuntosNecesarios = 1400,
                    Categoria = "Pizza"
                },

                new ProductoRecompensa {
                    ProductoId = 3, // Hawaiana
                    Nombre = "Hawaiana",
                    PrecioOriginal = 8.00m,
                    PuntosNecesarios = 1400,
                    Categoria = "Pizza"
                },

                new ProductoRecompensa {
                    ProductoId = 4, // Margarita
                    Nombre = "Margarita",
                    PrecioOriginal = 8.00m,
                    PuntosNecesarios = 1400,
                    Categoria = "Pizza"
                },
                
                // COCTELES - Opciones premium para usuarios frecuentes
                new ProductoRecompensa {
                    ProductoId = 38, // Cuba libre
                    Nombre = "Cuba libre",
                    PrecioOriginal = 6.00m,
                    PuntosNecesarios = 1000,
                    Categoria = "Cocteles"
                },

                new ProductoRecompensa {
                    ProductoId = 39, // Mojito
                    Nombre = "Mojito",
                    PrecioOriginal = 6.00m,
                    PuntosNecesarios = 1000,
                    Categoria = "Cocteles"
                },

                new ProductoRecompensa {
                    ProductoId = 43, // Gin Tonic
                    Nombre = "Gin Tonic",
                    PrecioOriginal = 6.00m,
                    PuntosNecesarios = 1000,
                    Categoria = "Cocteles"
                },
                
                // COMBOS CERVEZA - Recompensas de alto nivel para clientes muy frecuentes
                new ProductoRecompensa {
                    ProductoId = 21, // 3 jarros cerveza artesanal
                    Nombre = "3 jarros cerveza artesanal",
                    PrecioOriginal = 10.00m,
                    PuntosNecesarios = 1800,
                    Categoria = "Cerveza"
                },

                new ProductoRecompensa {
                    ProductoId = 24, // Combo 3 Pilsener
                    Nombre = "Combo 3 Pilsener",
                    PrecioOriginal = 10.00m,
                    PuntosNecesarios = 1800,
                    Categoria = "Cerveza"
                },
                
                // PROMO - Recompensa máxima para clientes muy fieles
                new ProductoRecompensa {
                    ProductoId = 67, // Promo Sanduchera
                    Nombre = "Promo Sanduchera",
                    PrecioOriginal = 10.00m,
                    PuntosNecesarios = 2000,
                    Categoria = "Promo"
                }
            };

            context.ProductosRecompensa.AddRange(productosRecompensa);
            context.SaveChanges();
        }
    }
}