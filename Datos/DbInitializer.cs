using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;

namespace ProyectoIdentity.Datos
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            if (context.Productos.Any())
            {
                return; // DB ya tiene datos
            }

            var productos = new List<Producto>
            {
                // PIZZAS
                new Producto {
                    Nombre = "Pepperoni",
                    Precio = 15,
                    Categoria = "Pizza",
                    Descripcion = "Mozzarella, jamón y champiñones.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/pexels-muffin-1653877.jpg"),
                    InfoNutricional = "Peso:210g|Calorías:517Kcal - 26%|Grasas:26g - 33%|Carbohidratos Totales:42g - 14%|Proteínas:28g - 57%|Sodio:1020mg - 42%|Grasas trans:0.12g - 0%|Grasas Saturadas:8.8g - 44%|Fibra:0.8g - 0%",
                    Alergenos = "Leche|Lactosa|Pimienta|Gluten|Sésamo"
                },                new Producto { Nombre = "Mi Champ", Precio = 8, Categoria = "Pizza", Imagen = File.ReadAllBytes("wwwroot/images1/champ.jpg") },
                new Producto {
                    Nombre = "Hawaiana",
                    Precio = 8,
                    Categoria = "Pizza",
                    Descripcion = "Pizza con jamón y piña, combinación dulce y salada con base de queso mozzarella.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/pexels-brettjordan-842519.jpg"),
                    InfoNutricional = "Peso:230g|Calorías:495Kcal - 25%|Grasas:22g - 28%|Carbohidratos Totales:48g - 16%|Proteínas:24g - 48%|Sodio:890mg - 37%|Grasas trans:0.08g - 0%|Grasas Saturadas:7.5g - 38%|Fibra:1.5g - 6%|Azúcares:12g - 24%",
                    Alergenos = "Leche|Lactosa|Gluten|Trigo|Puede contener trazas de sésamo"
                },
                // Pizza Margarita
                new Producto {
                    Nombre = "Margarita",
                    Precio = 8,
                    Categoria = "Pizza",
                    Descripcion = "Pizza tradicional italiana con salsa de tomate, mozzarella fresca y albahaca.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/marga.jpg"),
                    InfoNutricional = "Peso:200g|Calorías:450Kcal - 23%|Grasas:18g - 23%|Carbohidratos Totales:40g - 13%|Proteínas:22g - 44%|Sodio:780mg - 33%|Grasas trans:0.05g - 0%|Grasas Saturadas:7.2g - 36%|Fibra:1.8g - 7%|Azúcares:5g - 10%",
                    Alergenos = "Leche|Lactosa|Gluten|Trigo"
                },

                // Pizza Cheddar
                new Producto {
                    Nombre = "Cheddar",
                    Precio = 10,
                    Categoria = "Pizza",
                    Descripcion = "Pizza gourmet con base de crema, queso cheddar madurado, champiñones frescos y hierbas.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/champi.jpg"),
                    InfoNutricional = "Peso:220g|Calorías:520Kcal - 26%|Grasas:28g - 36%|Carbohidratos Totales:38g - 13%|Proteínas:26g - 52%|Sodio:920mg - 38%|Grasas trans:0.15g - 1%|Grasas Saturadas:12g - 60%|Fibra:1.2g - 5%|Azúcares:3g - 6%",
                    Alergenos = "Leche|Lactosa|Gluten|Trigo|Puede contener trazas de frutos secos"
                },

                // Pizza Diavola
                new Producto {
                    Nombre = "Diavola",
                    Precio = 10,
                    Categoria = "Pizza",
                    Descripcion = "Pizza picante con salami italiano, chile, ajo y aceite de oliva picante.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/diablo.jpg"),
                    InfoNutricional = "Peso:215g|Calorías:540Kcal - 27%|Grasas:30g - 38%|Carbohidratos Totales:42g - 14%|Proteínas:25g - 50%|Sodio:1200mg - 50%|Grasas trans:0.2g - 1%|Grasas Saturadas:11g - 55%|Fibra:1.5g - 6%|Azúcares:4g - 8%",
                    Alergenos = "Leche|Lactosa|Gluten|Trigo|Puede contener pimienta|Mostaza"
                },

                // Pizza Meat Lovers
                new Producto {
                    Nombre = "Meat Lovers",
                    Precio = 10,
                    Categoria = "Pizza",
                    Descripcion = "Una combinación de carnes: pepperoni, jamón, tocino y salchicha italiana sobre queso mozzarella.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/meat.jpg"),
                    InfoNutricional = "Peso:240g|Calorías:620Kcal - 31%|Grasas:35g - 45%|Carbohidratos Totales:40g - 13%|Proteínas:32g - 64%|Sodio:1350mg - 56%|Grasas trans:0.25g - 1%|Grasas Saturadas:14g - 70%|Fibra:0.8g - 3%|Azúcares:3g - 6%",
                    Alergenos = "Leche|Lactosa|Gluten|Trigo|Puede contener mostaza|Derivados de cerdo"
                },

                // Pizza Veggie Lovers
                new Producto {
                    Nombre = "Veggie Lovers",
                    Precio = 10,
                    Categoria = "Pizza",
                    Descripcion = "Deliciosa pizza con pimientos, champiñones, aceitunas, cebolla, tomate y queso 100% vegetal.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/vege.jpg"),
                    InfoNutricional = "Peso:220g|Calorías:420Kcal - 21%|Grasas:16g - 21%|Carbohidratos Totales:52g - 17%|Proteínas:18g - 36%|Sodio:650mg - 27%|Grasas trans:0g - 0%|Grasas Saturadas:4g - 20%|Fibra:6g - 24%|Azúcares:7g - 14%",
                    Alergenos = "Gluten|Trigo|Puede contener trazas de frutos secos|Apto para veganos"
                },

                // Pizza Say Cheese
                new Producto {
                    Nombre = "Say Cheese",
                    Precio = 12,
                    Categoria = "Pizza",
                    Descripcion = "Combinación de cuatro quesos premium: mozzarella, gorgonzola, parmesano y provolone.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/pexels-vince-2471171.jpg"),
                    InfoNutricional = "Peso:210g|Calorías:560Kcal - 28%|Grasas:32g - 41%|Carbohidratos Totales:36g - 12%|Proteínas:30g - 60%|Sodio:950mg - 40%|Grasas trans:0.1g - 0%|Grasas Saturadas:16g - 80%|Fibra:0.5g - 2%|Azúcares:2g - 4%",
                    Alergenos = "Leche|Lactosa|Gluten|Trigo|Quesos madurados"
                },

                // Pizza Verace
                new Producto {
                    Nombre = "Verace",
                    Precio = 12,
                    Categoria = "Pizza",
                    Descripcion = "Auténtica pizza napolitana con masa fermentada 48 horas, tomates San Marzano y mozzarella de búfala.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/pexels-ahmedbhutta11-7350139.jpg"),
                    InfoNutricional = "Peso:230g|Calorías:480Kcal - 24%|Grasas:20g - 26%|Carbohidratos Totales:45g - 15%|Proteínas:24g - 48%|Sodio:820mg - 34%|Grasas trans:0g - 0%|Grasas Saturadas:8g - 40%|Fibra:2.2g - 9%|Azúcares:6g - 12%",
                    Alergenos = "Leche|Leche de búfala|Lactosa|Gluten|Trigo"
                },
            
                // CERVEZA
                new Producto {
                    Nombre = "Jarro",
                    Precio = 4,
                    Categoria = "Cerveza",
                    Descripcion = "Bebida con alcohol. Cerveza artesanal de 330 ml.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/jarro.jpg"),
                    InfoNutricional = "Volumen:330ml|Calorías:140Kcal - 7%|Carbohidratos:12g - 4%|Proteínas:1.2g - 2%|Grasas:0g - 0%|Alcohol:4.5% vol.|Sodio:10mg - 0%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Puede contener trazas de trigo"
                },

                new Producto {
                    Nombre = "Pinta",
                    Precio = 6,
                    Categoria = "Cerveza",
                    Descripcion = "Bebida con alcohol. Cerveza artesanal de 500 ml",
                    Imagen = File.ReadAllBytes("wwwroot/images1/pinta.jpg"),
                    InfoNutricional = "Volumen:473ml|Calorías:200Kcal - 10%|Carbohidratos:17g - 6%|Proteínas:1.7g - 3%|Grasas:0g - 0%|Alcohol:4.5% vol.|Sodio:14mg - 1%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Puede contener trazas de trigo"
                },

                new Producto {
                    Nombre = "Litro",
                    Precio = 12,
                    Categoria = "Cerveza",
                    Descripcion = "Bebida con alcohol. Cerveza artesanal de 1000 ml",
                    Imagen = File.ReadAllBytes("wwwroot/images1/cervegrande.jpg"),
                    InfoNutricional = "Volumen:1000ml|Calorías:420Kcal - 21%|Carbohidratos:36g - 12%|Proteínas:3.6g - 7%|Grasas:0g - 0%|Alcohol:4.5% vol.|Sodio:30mg - 1%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Puede contener trazas de trigo"
                },

                new Producto {
                    Nombre = "Growler",
                    Precio = 15,
                    Categoria = "Cerveza",
                    Descripcion = "Bebida con alcohol. Cerveza artesanal de 1000 ml.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/growler.jpg"),
                    InfoNutricional = "Volumen:1900ml|Calorías:800Kcal - 40%|Carbohidratos:68g - 23%|Proteínas:6.8g - 14%|Grasas:0g - 0%|Alcohol:4.5% vol.|Sodio:57mg - 2%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Puede contener trazas de trigo"
                },

                // CERVEZAS DE MARCA
                new Producto {
                    Nombre = "Stella Artois",
                    Precio = 5,
                    Categoria = "Cerveza",
                    Descripcion = "Cerveza premium lager belga. Botella de 330ml.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/stella.jpg"),
                    InfoNutricional = "Volumen:330ml|Calorías:155Kcal - 8%|Carbohidratos:12.5g - 4%|Proteínas:1.3g - 3%|Grasas:0g - 0%|Alcohol:5.2% vol.|Sodio:12mg - 1%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Malta"
                },

                new Producto {
                    Nombre = "Corona",
                    Precio = 5,
                    Categoria = "Cerveza",
                    Descripcion = "Bebida con alcohol. Cerveza internacional de 330 ml.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/corona.jpg"),
                    InfoNutricional = "Volumen:355ml|Calorías:148Kcal - 7%|Carbohidratos:13.9g - 5%|Proteínas:1.2g - 2%|Grasas:0g - 0%|Alcohol:4.6% vol.|Sodio:13mg - 1%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Malta"
                },

                new Producto {
                    Nombre = "Pilsener",
                    Precio = 4,
                    Categoria = "Cerveza",
                    Descripcion = "Bebida con alcohol. Cerveza nacional de 330 ml.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/pilsener1.jpg"),
                    InfoNutricional = "Volumen:330ml|Calorías:143Kcal - 7%|Carbohidratos:10.8g - 4%|Proteínas:1.1g - 2%|Grasas:0g - 0%|Alcohol:4.3% vol.|Sodio:12mg - 1%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Malta"
                },

                new Producto {
                    Nombre = "Club",
                    Precio = 4.5M,
                    Categoria = "Cerveza",
                    Descripcion = "Bebida con alcohol. Cerveza nacional de 330 ml.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/club.jpg"),
                    InfoNutricional = "Volumen:330ml|Calorías:147Kcal - 7%|Carbohidratos:11.5g - 4%|Proteínas:1.2g - 2%|Grasas:0g - 0%|Alcohol:4.4% vol.|Sodio:11mg - 0%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Malta"
                },

                // MICHELADAS
                new Producto {
                    Nombre = "Clásico",
                    Precio = 1.5M,
                    Categoria = "Cerveza",
                    Descripcion = "Michelada clásica con cerveza, limón, sal y especias.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/miche.jpg"),
                    InfoNutricional = "Volumen:355ml|Calorías:165Kcal - 8%|Carbohidratos:15g - 5%|Proteínas:1.2g - 2%|Grasas:0g - 0%|Alcohol:4% vol.|Sodio:450mg - 19%|Azúcares:2g - 4%",
                    Alergenos = "Cebada|Gluten|Puede contener trazas de pimienta y chile"
                },

                new Producto {
                    Nombre = "Maracuyá",
                    Precio = 1.5M,
                    Categoria = "Cerveza",
                    Descripcion = "Bebida con alcohol. Cerveza con maracuya, sal, limon, salsa y especias.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/maracuya.jpg"),
                    InfoNutricional = "Volumen:355ml|Calorías:180Kcal - 9%|Carbohidratos:18g - 6%|Proteínas:1.2g - 2%|Grasas:0g - 0%|Alcohol:4% vol.|Sodio:420mg - 18%|Azúcares:6g - 12%",
                    Alergenos = "Cebada|Gluten|Puede contener trazas de pimienta y chile"
                },

                // COMBOS DE CERVEZAS
                new Producto {
                    Nombre = "3 jarros cerveza artesanal",
                    Precio = 10,
                    Categoria = "Cerveza",
                    Descripcion = "3 Bebidas con alcohol. Cerveza artesanal de 1000 ml.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/jarro1.jpg"),
                    InfoNutricional = "Volumen:990ml (3x330ml)|Calorías:420Kcal - 21%|Carbohidratos:36g - 12%|Proteínas:3.6g - 7%|Grasas:0g - 0%|Alcohol:4.5% vol.|Sodio:30mg - 1%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Puede contener trazas de trigo"
                },

                new Producto {
                    Nombre = "3 pintas cualquier estilo",
                    Precio = 15,
                    Categoria = "Cerveza",
                    Descripcion = "3 Bebidas con alcohol. Cerveza artesanal de 1000 ml",
                    Imagen = File.ReadAllBytes("wwwroot/images1/jarra3.jpg"),
                    InfoNutricional = "Volumen:1419ml (3x473ml)|Calorías:600Kcal - 30%|Carbohidratos:51g - 17%|Proteínas:5.1g - 10%|Grasas:0g - 0%|Alcohol:4.5% vol.|Sodio:42mg - 2%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Puede contener trazas de trigo"
                },

                new Producto {
                    Nombre = "3 Stella Artois / Corona",
                    Precio = 20,
                    Categoria = "Cerveza",
                    Descripcion = "Pack de 3 botellas de cerveza premium a elegir entre Stella Artois o Corona.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/stella3.jpg"),
                    InfoNutricional = "Volumen:990ml (3x330ml)|Calorías:465Kcal - 23%|Carbohidratos:37.5g - 13%|Proteínas:3.9g - 8%|Grasas:0g - 0%|Alcohol:5.2% vol.|Sodio:36mg - 2%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Malta"
                },

                new Producto {
                    Nombre = "Combo 3 Pilsener",
                    Precio = 10,
                    Categoria = "Cerveza",
                    Descripcion = "3 Bebidas con alcohol. Cerveza nacional de 330 ml",
                    Imagen = File.ReadAllBytes("wwwroot/images1/pilsener.jpg"),
                    InfoNutricional = "Volumen:990ml (3x330ml)|Calorías:429Kcal - 21%|Carbohidratos:32.4g - 11%|Proteínas:3.3g - 7%|Grasas:0g - 0%|Alcohol:4.3% vol.|Sodio:36mg - 2%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Malta"
                },

                new Producto {
                    Nombre = "Combo 3 Club",
                    Precio = 12,
                    Categoria = "Cerveza",
                    Descripcion = "3 Bebidas con alcohol. Cerveza nacional de 330 ml",
                    Imagen = File.ReadAllBytes("wwwroot/images1/club3.jpg"),
                    InfoNutricional = "Volumen:990ml (3x330ml)|Calorías:441Kcal - 22%|Carbohidratos:34.5g - 12%|Proteínas:3.6g - 7%|Grasas:0g - 0%|Alcohol:4.4% vol.|Sodio:33mg - 1%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten|Malta"
                },
                // COCTELES - VINO
                new Producto {
                    Nombre = "Botella de vino tinto",
                    Precio = 20m,
                    Categoria = "Cocteles",
                    Descripcion = "Botella de vino tinto de selección de la casa. Ideal para compartir.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/botellavinotinto.jpg"),
                    InfoNutricional = "Volumen:750ml|Calorías:625Kcal - 31%|Carbohidratos:12g - 4%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:13.5% vol.|Sodio:15mg - 1%|Azúcares:7g - 14%|Taninos:Medio-Alto|Resveratrol:Contiene",
                    Alergenos = "Sulfitos|Puede contener trazas de huevo y leche (clarificantes)"
                },

                new Producto {
                    Nombre = "Jarra de calimotcho",
                    Precio = 18m,
                    Categoria = "Cocteles",
                    Descripcion = "Jarra de calimotxo tradicional: mezcla de vino tinto y refresco de cola.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/jarrajalimocho.jpg"),
                    InfoNutricional = "Volumen:1000ml|Calorías:500Kcal - 25%|Carbohidratos:65g - 22%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:7% vol.|Sodio:20mg - 1%|Azúcares:45g - 90%|Cafeína:30mg",
                    Alergenos = "Sulfitos|Puede contener caramelo (colorante E-150d)"
                },

                new Producto {
                    Nombre = "Jarra de tinto de verano",
                    Precio = 18m,
                    Categoria = "Cocteles",
                    Descripcion = "Jarra de refrescante tinto de verano: vino tinto, gaseosa y frutas frescas.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/jarratintoverano.jpg"),
                    InfoNutricional = "Volumen:1000ml|Calorías:450Kcal - 23%|Carbohidratos:55g - 18%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:6% vol.|Sodio:18mg - 1%|Azúcares:40g - 80%",
                    Alergenos = "Sulfitos|Cítricos"
                },

                // COCTELES - AGUARDIENTE Y CAÑA
                new Producto {
                    Nombre = "Manaba mule",
                    Precio = 6m,
                    Categoria = "Cocteles",
                    Descripcion = "Versión ecuatoriana del Moscow Mule: caña manabita, jengibre, limón y ginger beer.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/manamule.jpg"),
                    InfoNutricional = "Volumen:375ml|Calorías:220Kcal - 11%|Carbohidratos:35g - 12%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:8% vol.|Sodio:15mg - 1%|Azúcares:25g - 50%|Jengibre:Alto contenido",
                    Alergenos = "Cítricos|Puede contener trazas de sulfitos"
                },

                new Producto {
                    Nombre = "Caipirinha manaba",
                    Precio = 6m,
                    Categoria = "Cocteles",
                    Descripcion = "Cóctel con caña manabita, lima, azúcar y hielo molido.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/caipimanaba.jpg"),
                    InfoNutricional = "Volumen:250ml|Calorías:205Kcal - 10%|Carbohidratos:20g - 7%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:12% vol.|Sodio:5mg - 0%|Azúcares:18g - 36%",
                    Alergenos = "Cítricos"
                },

                new Producto {
                    Nombre = "Botella de caña manabita",
                    Precio = 25m,
                    Categoria = "Cocteles",
                    Descripcion = "Botella de caña manabita artesanal, destilado tradicional ecuatoriano.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/cañamanaba.jpg"),
                    InfoNutricional = "Volumen:750ml|Calorías:1650Kcal - 83%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:40% vol.|Sodio:0mg - 0%|Azúcares:0g - 0%",
                    Alergenos = "Sin alérgenos principales"
                },

                new Producto {
                    Nombre = "Botella de Antioqueño",
                    Precio = 45m,
                    Categoria = "Cocteles",
                    Descripcion = "Botella de aguardiente Antioqueño, el licor tradicional colombiano con sabor anisado.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/botellaantioqueño.jpg"),
                    InfoNutricional = "Volumen:750ml|Calorías:1620Kcal - 81%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:37.5% vol.|Sodio:0mg - 0%|Azúcares:0g - 0%|Anís:Contiene",
                    Alergenos = "Sin alérgenos principales|Contiene anís"
                },

                // COCTELES - TEQUILA
                new Producto {
                    Nombre = "Paloma",
                    Precio = 8m,
                    Categoria = "Cocteles",
                    Descripcion = "Cóctel mexicano con tequila, refresco de toronja, limón y sal.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/paloma.jpg"),
                    InfoNutricional = "Volumen:350ml|Calorías:180Kcal - 9%|Carbohidratos:15g - 5%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:10% vol.|Sodio:200mg - 8%|Azúcares:14g - 28%",
                    Alergenos = "Cítricos|Puede contener sulfitos"
                },

                new Producto {
                    Nombre = "Margarita clásica",
                    Precio = 6m,
                    Categoria = "Cocteles",
                    Descripcion = "Tequila, limón y azúcar, el cóctel mexicano por excelencia.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/margarira.jpg"),
                    InfoNutricional = "Volumen:250ml|Calorías:220Kcal - 11%|Carbohidratos:12g - 4%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:16% vol.|Sodio:250mg - 10%|Azúcares:10g - 20%",
                    Alergenos = "Cítricos|Puede contener sulfitos"
                },

                new Producto {
                    Nombre = "Margarita maracuyá",
                    Precio = 6m,
                    Categoria = "Cocteles",
                    Descripcion = "Margarita clásica con un toque tropical de maracuyá y especias.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/maracuya.jpg"),
                    InfoNutricional = "Volumen:250ml|Calorías:235Kcal - 12%|Carbohidratos:25g - 8%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:15% vol.|Sodio:230mg - 10%|Azúcares:20g - 40%",
                    Alergenos = "Cítricos|Maracuyá|Puede contener sulfitos"
                },

                new Producto {
                    Nombre = "Margarita frutos rojos",
                    Precio = 6m,
                    Categoria = "Cocteles",
                    Descripcion = "Refrescante margarita con mezcla de frutos rojos y tequila.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/frutosrojos.jpg"),
                    InfoNutricional = "Volumen:250ml|Calorías:230Kcal - 12%|Carbohidratos:24g - 8%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:15% vol.|Sodio:200mg - 8%|Azúcares:18g - 36%|Antocianinas:Alto contenido",
                    Alergenos = "Cítricos|Frutos rojos|Puede contener sulfitos"
                },

                new Producto {
                    Nombre = "Botella de tequila",
                    Precio = 45m,
                    Categoria = "Cocteles",
                    Descripcion = "Botella de tequila José Cuervo Especial, ideal para compartir.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/josecuervo.jpg"),
                    InfoNutricional = "Volumen:750ml|Calorías:1650Kcal - 83%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:38% vol.|Sodio:0mg - 0%|Azúcares:0g - 0%",
                    Alergenos = "Sin alérgenos principales|Agave 100%"
                },

                // COCTELES - RON
                new Producto {
                    Nombre = "Cuba libre",
                    Precio = 6m,
                    Categoria = "Cocteles",
                    Descripcion = "Cóctel clásico con ron, refresco de cola y limón.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/cubalibre.jpg"),
                    InfoNutricional = "Volumen:320ml|Calorías:185Kcal - 9%|Carbohidratos:22g - 7%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:11% vol.|Sodio:15mg - 1%|Azúcares:20g - 40%|Cafeína:20mg",
                    Alergenos = "Cítricos|Puede contener caramelo (colorante E-150d)"
                },

                new Producto {
                    Nombre = "Mojito",
                    Precio = 6m,
                    Categoria = "Cocteles",
                    Descripcion = "Refrescante cóctel cubano con ron, hierbabuena, lima, azúcar y soda.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/mojito.jpg"),
                    InfoNutricional = "Volumen:350ml|Calorías:175Kcal - 9%|Carbohidratos:20g - 7%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:10% vol.|Sodio:10mg - 0%|Azúcares:18g - 36%",
                    Alergenos = "Cítricos|Menta"
                },

                new Producto {
                    Nombre = "Mojito maracuyá",
                    Precio = 7m,
                    Categoria = "Cocteles",
                    Descripcion = "Versión tropical del mojito con pulpa de maracuyá, ron, hierbabuena y limón.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/mojitomaracuya.jpg"),
                    InfoNutricional = "Volumen:350ml|Calorías:200Kcal - 10%|Carbohidratos:28g - 9%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:10% vol.|Sodio:10mg - 0%|Azúcares:25g - 50%",
                    Alergenos = "Cítricos|Maracuyá|Menta"
                },

                new Producto {
                    Nombre = "Mojito frutos rojos",
                    Precio = 7m,
                    Categoria = "Cocteles",
                    Descripcion = "Delicioso mojito con frutos rojos, ron, hierbabuena y limón.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/mojitofrutosrojos.jpg"),
                    InfoNutricional = "Volumen:350ml|Calorías:195Kcal - 10%|Carbohidratos:26g - 9%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:10% vol.|Sodio:10mg - 0%|Azúcares:24g - 48%|Antocianinas:Alto contenido",
                    Alergenos = "Cítricos|Frutos rojos|Menta"
                },

                new Producto {
                    Nombre = "Botella de Ron Abuelo",
                    Precio = 45m,
                    Categoria = "Cocteles",
                    Descripcion = "Ron de la marca Ron Abuelo, ideal para cócteles o disfrutar solo.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/ronabuelo.jpg"),
                    InfoNutricional = "Volumen:750ml|Calorías:1620Kcal - 81%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:37.5% vol.|Sodio:0mg - 0%|Azúcares:0g - 0%",
                    Alergenos = "Sin alérgenos principales|Puede contener sulfitos|Caramelo (colorante)"
                },

                // COCTELES - GIN
                new Producto {
                    Nombre = "Gin Tonic",
                    Precio = 6m,
                    Categoria = "Cocteles",
                    Descripcion = "Gin, limón y agua tónica, el cóctel refrescante por excelencia.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/gintonic.jpg"),
                    InfoNutricional = "Volumen:320ml|Calorías:170Kcal - 9%|Carbohidratos:15g - 5%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:12% vol.|Sodio:10mg - 0%|Azúcares:14g - 28%|Quinina:Contiene",
                    Alergenos = "Cítricos|Botánicos|Enebro"
                },

                new Producto {
                    Nombre = "Gin Tonic Maracuyá",
                    Precio = 7m,
                    Categoria = "Cocteles",
                    Descripcion = "Gin Tonic con un toque tropical de maracuyá.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/gintonicmaracuya.jpg"),
                    InfoNutricional = "Volumen:320ml|Calorías:195Kcal - 10%|Carbohidratos:24g - 8%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:12% vol.|Sodio:10mg - 0%|Azúcares:20g - 40%|Quinina:Contiene",
                    Alergenos = "Cítricos|Botánicos|Enebro|Maracuyá"
                },

                new Producto {
                    Nombre = "Gin Tonic Frutos rojos",
                    Precio = 7m,
                    Categoria = "Cocteles",
                    Descripcion = "Gin tonic con infusión de frutos rojos.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/gintonicfrutosrojos.jpg"),
                    InfoNutricional = "Volumen:320ml|Calorías:190Kcal - 10%|Carbohidratos:22g - 7%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:12% vol.|Sodio:10mg - 0%|Azúcares:18g - 36%|Quinina:Contiene|Antocianinas:Alto contenido",
                    Alergenos = "Cítricos|Botánicos|Enebro|Frutos rojos"
                },

                // COCTELES - VODKA
                new Producto {
                    Nombre = "Moscow Mule",
                    Precio = 8m,
                    Categoria = "Cocteles",
                    Descripcion = "Cóctel clásico con vodka, cerveza de jengibre, lima fresca y hielo, servido en taza de cobre.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/moscowmule.jpg"),
                    InfoNutricional = "Volumen:350ml|Calorías:210Kcal - 11%|Carbohidratos:30g - 10%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:10% vol.|Sodio:15mg - 1%|Azúcares:28g - 56%|Jengibre:Alto contenido",
                    Alergenos = "Cítricos|Puede contener trazas de sulfitos"
                },

                // COCTELES - JAGERMEISTER
                new Producto {
                    Nombre = "Jager Sour",
                    Precio = 12m,
                    Categoria = "Cocteles",
                    Descripcion = "Jagermeister con clara de huevo, limón y azúcar, un cóctel cítrico y suave.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/jagersour.jpg"),
                    InfoNutricional = "Volumen:250ml|Calorías:240Kcal - 12%|Carbohidratos:25g - 8%|Proteínas:4g - 8%|Grasas:0g - 0%|Alcohol:15% vol.|Sodio:35mg - 1%|Azúcares:22g - 44%|Hierbas:Alto contenido",
                    Alergenos = "Cítricos|Huevo|Hierbas|Puede contener trazas de gluten"
                },

                new Producto {
                    Nombre = "Jagerito",
                    Precio = 12m,
                    Categoria = "Cocteles",
                    Descripcion = "Jagermeister con clara de huevo, limón y azúcar, un cóctel cítrico y suave.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/jagerito.jpg"),
                    InfoNutricional = "Volumen:350ml|Calorías:220Kcal - 11%|Carbohidratos:22g - 7%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:13% vol.|Sodio:10mg - 0%|Azúcares:20g - 40%|Hierbas:Alto contenido",
                    Alergenos = "Cítricos|Menta|Hierbas|Puede contener trazas de gluten"
                },

                // COCTELES - WHISKY
                new Producto {
                    Nombre = "Whisky Sour",
                    Precio = 12m,
                    Categoria = "Cocteles",
                    Descripcion = "Whisky, clara de huevo, limón y azúcar, un cóctel suave y ligeramente ácido.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/whiskysour.jpg"),
                    InfoNutricional = "Volumen:250ml|Calorías:230Kcal - 12%|Carbohidratos:20g - 7%|Proteínas:4g - 8%|Grasas:0g - 0%|Alcohol:18% vol.|Sodio:30mg - 1%|Azúcares:18g - 36%",
                    Alergenos = "Cítricos|Huevo|Cebada|Gluten"
                },

                new Producto {
                    Nombre = "New York Sour",
                    Precio = 12m,
                    Categoria = "Cocteles",
                    Descripcion = "Whisky sour con una capa flotante de vino tinto. Elegante y sofisticado.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/newyork.jpg"),
                    InfoNutricional = "Volumen:250ml|Calorías:245Kcal - 12%|Carbohidratos:22g - 7%|Proteínas:4g - 8%|Grasas:0g - 0%|Alcohol:20% vol.|Sodio:30mg - 1%|Azúcares:19g - 38%|Taninos:Contiene",
                    Alergenos = "Cítricos|Huevo|Cebada|Gluten|Sulfitos"
                },

                new Producto {
                    Nombre = "Whisky on the rocks",
                    Precio = 12m,
                    Categoria = "Cocteles",
                    Descripcion = "Whisky servido con hielo, ideal para disfrutar solo o en compañía",
                    Imagen = File.ReadAllBytes("wwwroot/images1/whiskyrocks.jpg"),
                    InfoNutricional = "Volumen:100ml|Calorías:220Kcal - 11%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:40% vol.|Sodio:0mg - 0%|Azúcares:0g - 0%",
                    Alergenos = "Cebada|Gluten"
                },

                // BEBIDAS
                new Producto {
                    Nombre = "Agua sin gas",
                    Precio = 1m,
                    Categoria = "Bebidas",
                    Descripcion = "Agua natural purificada, refrescante.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/aguasingas.jpg"),
                    InfoNutricional = "Volumen:500ml|Calorías:0Kcal - 0%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:5mg - 0%|Azúcares:0g - 0%|Minerales:Contiene",
                    Alergenos = "Sin alérgenos"
                },

                new Producto {
                    Nombre = "Agua mineral",
                    Precio = 1.5m,
                    Categoria = "Bebidas",
                    Descripcion = "Agua mineral con gas natural.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/download (8).jpeg"),
                    InfoNutricional = "Volumen:500ml|Calorías:0Kcal - 0%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:20mg - 1%|Azúcares:0g - 0%|Minerales:Alto contenido",
                    Alergenos = "Sin alérgenos"
                },

                new Producto {
                    Nombre = "Limonada",
                    Precio = 2m,
                    Categoria = "Bebidas",
                    Descripcion = "Refrescante limonada casera preparada con limones frescos y un toque de azúcar.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/imperial.jpg"),
                    InfoNutricional = "Volumen:400ml|Calorías:90Kcal - 5%|Carbohidratos:22g - 7%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:5mg - 0%|Azúcares:20g - 40%|Vitamina C:60%",
                    Alergenos = "Cítricos"
                },

                new Producto {
                    Nombre = "Limonada Rosa",
                    Precio = 2.5m,
                    Categoria = "Bebidas",
                    Descripcion = "Limonada casera con un toque de frutos rojos que le dan su característico color rosa.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/rosa.jpg"),
                    InfoNutricional = "Volumen:400ml|Calorías:95Kcal - 5%|Carbohidratos:24g - 8%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:5mg - 0%|Azúcares:22g - 44%|Vitamina C:65%|Antocianinas:Contiene",
                    Alergenos = "Cítricos|Frutos rojos"
                },

                new Producto {
                    Nombre = "Té caliente",
                    Precio = 1.5m,
                    Categoria = "Bebidas",
                    Descripcion = "Té aromático servido caliente. Variedades: negro, verde, manzanilla o frutos rojos.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/tecaliente.jpg"),
                    InfoNutricional = "Volumen:300ml|Calorías:5Kcal - 0%|Carbohidratos:1g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:5mg - 0%|Azúcares:0g - 0%|Antioxidantes:Alto contenido",
                    Alergenos = "Sin alérgenos principales|Puede contener trazas de hierbas aromáticas"
                },

                new Producto {
                    Nombre = "Coca-Cola",
                    Precio = 2m,
                    Categoria = "Bebidas",
                    Descripcion = "Refresco carbonatado clásico servido frío con hielo.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/coca.jpg"),
                    InfoNutricional = "Volumen:355ml|Calorías:150Kcal - 8%|Carbohidratos:39g - 13%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:35mg - 1%|Azúcares:39g - 78%|Cafeína:35mg",
                    Alergenos = "Sin alérgenos principales|Contiene caramelo (colorante E-150d)"
                },

                new Producto {
                    Nombre = "Fanta",
                    Precio = 2m,
                    Categoria = "Bebidas",
                    Descripcion = "Refresco con sabor a naranja, burbujeante y refrescante.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/fanta.jpg"),
                    InfoNutricional = "Volumen:355ml|Calorías:160Kcal - 8%|Carbohidratos:42g - 14%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:30mg - 1%|Azúcares:42g - 84%|Colorantes:E-110",
                    Alergenos = "Sin alérgenos principales|Contiene colorantes artificiales"
                },

                new Producto {
                    Nombre = "Fioravanti",
                    Precio = 2m,
                    Categoria = "Bebidas",
                    Descripcion = "Tradicional soda ecuatoriana con sabor a fresa, dulce y refrescante.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/fiora.jpg"),
                    InfoNutricional = "Volumen:355ml|Calorías:155Kcal - 8%|Carbohidratos:40g - 13%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:25mg - 1%|Azúcares:40g - 80%|Colorantes:E-129",
                    Alergenos = "Sin alérgenos principales|Contiene colorantes artificiales"
                },

                new Producto {
                    Nombre = "Sprite",
                    Precio = 2m,
                    Categoria = "Bebidas",
                    Descripcion = "Refresco transparente con sabor a lima-limón, refrescante y burbujeante.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/sprite.jpg"),
                    InfoNutricional = "Volumen:355ml|Calorías:140Kcal - 7%|Carbohidratos:38g - 13%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:30mg - 1%|Azúcares:38g - 76%",
                    Alergenos = "Sin alérgenos principales|Puede contener aromas de cítricos"
                },

                new Producto {
                    Nombre = "Café americano",
                    Precio = 1.5m,
                    Categoria = "Bebidas",
                    Descripcion = "Café filtrado elaborado con granos seleccionados de Ecuador.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/americano.jpg"),
                    InfoNutricional = "Volumen:240ml|Calorías:5Kcal - 0%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Sodio:5mg - 0%|Azúcares:0g - 0%|Cafeína:120mg",
                    Alergenos = "Sin alérgenos principales"
                },

                new Producto {
                    Nombre = "Capuccino",
                    Precio = 2m,
                    Categoria = "Bebidas",
                    Descripcion = "Delicioso cappuccino con espresso, leche vaporizada y espuma de leche.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/capuccino.jpg"),
                    InfoNutricional = "Volumen:240ml|Calorías:120Kcal - 6%|Carbohidratos:12g - 4%|Proteínas:8g - 16%|Grasas:4g - 5%|Sodio:100mg - 4%|Azúcares:12g - 24%|Cafeína:150mg",
                    Alergenos = "Leche|Lactosa"
                },

                new Producto {
                    Nombre = "Iced Coffee",
                    Precio = 2.5m,
                    Categoria = "Bebidas",
                    Descripcion = "Café frío con leche, hielo y un toque de vainilla, refrescante y energizante.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/cafefrio.jpg"),
                    InfoNutricional = "Volumen:350ml|Calorías:150Kcal - 8%|Carbohidratos:18g - 6%|Proteínas:8g - 16%|Grasas:5g - 6%|Sodio:120mg - 5%|Azúcares:17g - 34%|Cafeína:160mg",
                    Alergenos = "Leche|Lactosa|Puede contener trazas de vainilla"
                },

                // PROMOS
                new Producto {
                    Nombre = "Promo Pilas",
                    Precio = 16m,
                    Categoria = "Promo",
                    Descripcion = "Promoción ideal para empezar la noche: 1 pizza mediana a elección + 4 cervezas nacionales.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/470140898_18004212455697669_2952221237043222814_n.jpg"),
                    InfoNutricional = "Porción total:Para 4 personas|Calorías:1800Kcal - 90%|Carbohidratos:160g - 53%|Proteínas:60g - 120%|Grasas:55g - 70%|Sodio:2400mg - 100%|Alcohol:4.5% vol. (por cerveza)",
                    Alergenos = "Leche|Lactosa|Gluten|Cebada|Trigo|Puede variar según pizza elegida"
                },

                new Producto {
                    Nombre = "Promo Lovers",
                    Precio = 20m,
                    Categoria = "Promo",
                    Descripcion = "Pack romántico: 1 pizza mediana especial + 2 cócteles + postre para compartir.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/promolovers.jpg"),
                    InfoNutricional = "Porción total:Para 2 personas|Calorías:1600Kcal - 80%|Carbohidratos:170g - 57%|Proteínas:40g - 80%|Grasas:45g - 58%|Sodio:1800mg - 75%|Alcohol:Varía según cócteles",
                    Alergenos = "Leche|Lactosa|Gluten|Trigo|Cítricos|Puede contener frutos secos|Varía según elecciones"
                },

                new Producto {
                    Nombre = "Promo King",
                    Precio = 24m,
                    Categoria = "Promo",
                    Descripcion = "Combo festivo: 1 pizza familiar + 6 cervezas nacionales + nachos con queso.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/promoking1.jpg"),
                    InfoNutricional = "Porción total:Para 6 personas|Calorías:2400Kcal - 120%|Carbohidratos:220g - 73%|Proteínas:80g - 160%|Grasas:85g - 109%|Sodio:3200mg - 133%|Alcohol:4.5% vol. (por cerveza)",
                    Alergenos = "Leche|Lactosa|Gluten|Cebada|Trigo|Maíz|Puede variar según pizza elegida"
                },

                new Producto {
                    Nombre = "Promo Sanduchera",
                    Precio = 10m,
                    Categoria = "Promo",
                    Descripcion = "Promoción para la hora del almuerzo: 2 sánduches a elección + 2 bebidas no alcohólicas.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/278953595_514677723447399_1453067101951070993_n.webp"),
                    InfoNutricional = "Porción total:Para 2 personas|Calorías:900Kcal - 45%|Carbohidratos:100g - 33%|Proteínas:35g - 70%|Grasas:30g - 38%|Sodio:1600mg - 67%|Azúcares:Varía según bebida",
                    Alergenos = "Gluten|Trigo|Puede contener lácteos|Puede contener mostaza|Puede variar según elecciones"
                },

                new Producto {
                    Nombre = "Promo Piqueo",
                    Precio = 18m,
                    Categoria = "Promo",
                    Descripcion = "Tabla para compartir con variedad de picadas + 4 cervezas nacionales.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/promopiqueo.avif"),
                    InfoNutricional = "Porción total:Para 4 personas|Calorías:1500Kcal - 75%|Carbohidratos:120g - 40%|Proteínas:50g - 100%|Grasas:60g - 77%|Sodio:2200mg - 92%|Alcohol:4.5% vol. (por cerveza)",
                    Alergenos = "Leche|Lactosa|Gluten|Cebada|Trigo|Puede contener frutos secos"
                },

                // SÁNDUCHES
                new Producto {
                    Nombre = "Tradicional",
                    Precio = 5m,
                    Categoria = "Sánduches",
                    Descripcion = "Sánduche clásico con jamón, queso, lechuga, tomate y mayonesa casera en pan artesanal.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/sanduchesp1.jpg"),
                    InfoNutricional = "Peso:220g|Calorías:380Kcal - 19%|Carbohidratos:35g - 12%|Proteínas:15g - 30%|Grasas:18g - 23%|Sodio:800mg - 33%|Fibra:2g - 8%|Azúcares:3g - 6%",
                    Alergenos = "Gluten|Trigo|Leche|Lactosa|Huevo (en mayonesa)"
                },

                new Producto {
                    Nombre = "Carne Mechada",
                    Precio = 5m,
                    Categoria = "Sánduches",
                    Descripcion = "Sánduche gourmet con carne mechada, queso derretido, cebolla caramelizada y salsa BBQ.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/sanduches.png"),
                    InfoNutricional = "Peso:250g|Calorías:450Kcal - 23%|Carbohidratos:40g - 13%|Proteínas:25g - 50%|Grasas:20g - 26%|Sodio:950mg - 40%|Fibra:2g - 8%|Azúcares:8g - 16%",
                    Alergenos = "Gluten|Trigo|Leche|Lactosa|Puede contener mostaza"
                },

                new Producto {
                    Nombre = "Veggie",
                    Precio = 5m,
                    Categoria = "Sánduches",
                    Descripcion = "Sánduche vegetariano con aguacate, queso fresco, tomate, rúcula y pesto en pan integral.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/veggie.webp"),
                    InfoNutricional = "Peso:230g|Calorías:350Kcal - 18%|Carbohidratos:30g - 10%|Proteínas:12g - 24%|Grasas:22g - 28%|Sodio:600mg - 25%|Fibra:6g - 24%|Azúcares:4g - 8%",
                    Alergenos = "Gluten|Trigo|Leche|Lactosa|Frutos secos (en pesto)"
                },

                // SHOTS
                new Producto {
                    Nombre = "Shot de tequila",
                    Precio = 3m,
                    Categoria = "Shot",
                    Descripcion = "Shot de tequila mexicano José Cuervo, servido con limón y sal.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/shottequila.jpg"),
                    InfoNutricional = "Volumen:45ml|Calorías:98Kcal - 5%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:38% vol.|Sodio:0mg - 0%|Azúcares:0g - 0%",
                    Alergenos = "Sin alérgenos principales|Agave 100%"
                },

                new Producto {
                    Nombre = "Shot de aguardiente",
                    Precio = 3m,
                    Categoria = "Shot",
                    Descripcion = "Shot de aguardiente colombiano Antioqueño, licor anisado tradicional.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/shotardiente.jpg"),
                    InfoNutricional = "Volumen:45ml|Calorías:97Kcal - 5%|Carbohidratos:0g - 0%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:37.5% vol.|Sodio:0mg - 0%|Azúcares:0g - 0%|Anís:Contiene",
                    Alergenos = "Sin alérgenos principales|Contiene anís"
                },

                new Producto {
                    Nombre = "Shot de Jager",
                    Precio = 6m,
                    Categoria = "Shot",
                    Descripcion = "Shot de licor de hierbas alemán Jägermeister, servido helado.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/jaggerbomb.jpg"),
                    InfoNutricional = "Volumen:45ml|Calorías:103Kcal - 5%|Carbohidratos:11g - 4%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:35% vol.|Sodio:0mg - 0%|Azúcares:11g - 22%|Hierbas:Alto contenido",
                    Alergenos = "Hierbas|Puede contener trazas de gluten"
                },

                new Producto {
                    Nombre = "Jager Bomb",
                    Precio = 10m,
                    Categoria = "Shot",
                    Descripcion = "Shot de Jägermeister sumergido en bebida energética. Potente y refrescante.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/jaggerbomb.jpg"),
                    InfoNutricional = "Volumen:150ml|Calorías:210Kcal - 11%|Carbohidratos:30g - 10%|Proteínas:0g - 0%|Grasas:0g - 0%|Alcohol:13% vol.|Sodio:40mg - 2%|Azúcares:29g - 58%|Cafeína:80mg|Taurina:Contiene",
                    Alergenos = "Hierbas|Puede contener trazas de gluten"
                },

                // PICADAS
                new Producto {
                    Nombre = "Nachos Cheddar",
                    Precio = 5m,
                    Categoria = "Picadas",
                    Descripcion = "Crujientes nachos de maíz bañados en queso cheddar fundido y jalapeños.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/cheddar.jpg"),
                    InfoNutricional = "Porción:200g|Calorías:480Kcal - 24%|Carbohidratos:50g - 17%|Proteínas:12g - 24%|Grasas:26g - 33%|Sodio:850mg - 35%|Fibra:3g - 12%|Azúcares:2g - 4%",
                    Alergenos = "Maíz|Leche|Lactosa|Puede contener trazas de trigo"
                },

                new Producto {
                    Nombre = "Nachos Verace",
                    Precio = 5m,
                    Categoria = "Picadas",
                    Descripcion = "Nachos gourmet con mezcla especial de quesos, guacamole, pico de gallo y crema agria.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/dbb444de-5ec0-41c1-bfc3-658c0ee76d06.jpg"),
                    InfoNutricional = "Porción:250g|Calorías:520Kcal - 26%|Carbohidratos:55g - 18%|Proteínas:15g - 30%|Grasas:30g - 38%|Sodio:920mg - 38%|Fibra:5g - 20%|Azúcares:3g - 6%",
                    Alergenos = "Maíz|Leche|Lactosa|Puede contener trazas de trigo|Cítricos"
                },

                new Producto {
                    Nombre = "Bread Sticks",
                    Precio = 5m,
                    Categoria = "Picadas",
                    Descripcion = "Bastones de pan casero con ajo y queso parmesano, acompañados de salsa de tomate.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/stciks.jpg"),
                    InfoNutricional = "Porción:180g|Calorías:420Kcal - 21%|Carbohidratos:60g - 20%|Proteínas:14g - 28%|Grasas:16g - 21%|Sodio:780mg - 33%|Fibra:2g - 8%|Azúcares:4g - 8%",
                    Alergenos = "Gluten|Trigo|Leche|Lactosa|Ajo"
                },

                new Producto {
                    Nombre = "Bread Sticks Verace",
                    Precio = 8m,
                    Categoria = "Picadas",
                    Descripcion = "Bastones de pan gourmet con mix de hierbas frescas, queso parmesano y dip de queso provolone.",
                    Imagen = File.ReadAllBytes("wwwroot/images1/sticks2.jpg"),
                    InfoNutricional = "Porción:220g|Calorías:490Kcal - 25%|Carbohidratos:65g - 22%|Proteínas:18g - 36%|Grasas:22g - 28%|Sodio:820mg - 34%|Fibra:3g - 12%|Azúcares:4g - 8%",
                    Alergenos = "Gluten|Trigo|Leche|Lactosa|Ajo|Hierbas"
                },
            };

            context.Productos.AddRange(productos);
            context.SaveChanges();
        }
    }
}
