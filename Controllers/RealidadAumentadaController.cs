using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoIdentity.Controllers
{
    //[Authorize]

    [Route("[controller]")]
    public class RealidadAumentadaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RealidadAumentadaController(ApplicationDbContext context) => _context = context;

        [HttpGet("VistaAR")]
        public async Task<IActionResult> VistaAR(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.ProductoId = id;
            ViewBag.ProductoNombre = producto.Nombre;
            ViewBag.ProductoPrecio = producto.Precio;
            ViewBag.ProductoDescripcion = producto.Descripcion;
            ViewBag.InfoNutricional = producto.InfoNutricional ?? "";
            ViewBag.Alergenos = producto.Alergenos ?? "";
            ViewBag.ModeloPath = DeterminarModelo3D(producto.Nombre);
            ViewBag.ModeloArchivo = DeterminarModeloPorId(id.Value);

            return View();
        }

        [HttpGet("VistaSimple")]
        public async Task<IActionResult> VistaSimple(int? id)
        {
            if (id == null)
            {
                // Si no se proporciona ID, usar por defecto
                ViewBag.ProductoId = 1;
                ViewBag.ProductoNombre = "Pizza Por Defecto";
                ViewBag.ProductoDescripcion = "Pizza deliciosa por defecto";
                ViewBag.InfoNutricional = "Peso:210g|Calorías:517Kcal - 26%|Grasas:26g - 33%|Carbohidratos:42g - 14%|Proteínas:28g - 57%|Sodio:1020mg - 42%";
                ViewBag.Alergenos = "Contiene lácteos|Puede contener gluten|Puede contener trazas de frutos secos";
                ViewBag.ModeloArchivo = "pizza2.glb";
                ViewBag.ModeloPath = "/RealidadAumentada/GetGLBFile?archivo=pizza2.glb";
            }
            else
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null) return NotFound();

                ViewBag.ProductoId = id;
                ViewBag.ProductoNombre = producto.Nombre;
                ViewBag.ProductoDescripcion = producto.Descripcion;
                ViewBag.InfoNutricional = producto.InfoNutricional ?? "";
                ViewBag.Alergenos = producto.Alergenos ?? "";

                // Determinar archivo según ID
                string archivoModelo = DeterminarModeloPorId(id.Value);
                ViewBag.ModeloArchivo = archivoModelo;
                ViewBag.ModeloPath = $"/RealidadAumentada/GetGLBFile?archivo={archivoModelo}";
            }

            return View();
        }

        [HttpGet("Debug")]
        public async Task<IActionResult> Debug(int? id)
        {
            if (id == null)
            {
                ViewBag.ProductoId = 1;
                ViewBag.ProductoNombre = "Debug - Pizza Por Defecto";
                ViewBag.ProductoDescripcion = "Pizza debug por defecto";
                ViewBag.InfoNutricional = "Peso:210g|Calorías:517Kcal - 26%|Grasas:26g - 33%|Carbohidratos:42g - 14%|Proteínas:28g - 57%|Sodio:1020mg - 42%";
                ViewBag.Alergenos = "Contiene lácteos|Puede contener gluten|Puede contener trazas de frutos secos";
                ViewBag.ModeloArchivo = "pizza2.glb";
                ViewBag.ModeloPath = "/RealidadAumentada/GetGLBFile?archivo=pizza2.glb";
            }
            else
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null) return NotFound();

                ViewBag.ProductoId = id;
                ViewBag.ProductoNombre = $"Debug - {producto.Nombre}";
                ViewBag.ProductoDescripcion = producto.Descripcion;
                ViewBag.InfoNutricional = producto.InfoNutricional ?? "";
                ViewBag.Alergenos = producto.Alergenos ?? "";

                string archivoModelo = DeterminarModeloPorId(id.Value);
                ViewBag.ModeloArchivo = archivoModelo;
                ViewBag.ModeloPath = $"/RealidadAumentada/GetGLBFile?archivo={archivoModelo}";
            }

            return View();
        }

        // ... resto de métodos sin cambios (GetGLBFile, Test, TestArchivos, DeterminarModelo3D, DeterminarModeloPorId)

        [HttpGet("GetGLBFile")]
        public IActionResult GetGLBFile(string archivo = "pizza2.glb")
        {
            // Validar que el archivo solicitado sea válido (solo archivos que se ven correctamente)
            var archivosPermitidos = new[] {
                "pizza2.glb", // Por defecto
                "pizza_pepperoni.glb", // ID 1
                "pizza_margarita.glb", // ID 4
                "pizza_cheddar.glb", // ID 5
                "pizza_diavola.glb", // ID 6
                "pizza_say_cheese.glb", // ID 9
                "pizza_verace.glb", // ID 10
                "pizza_meat_lovers.glb", // ID 7
                "pizza_michamp.glb", // ID 2
                "pizza_veggielovers.glb", // ID 8
                "pizza_h.glb" // ID 3
            };

            if (!archivosPermitidos.Contains(archivo))
            {
                archivo = "pizza2.glb"; // Archivo por defecto
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models3d", archivo);

            Console.WriteLine($"Archivo solicitado: {archivo}");
            Console.WriteLine($"Ruta completa: {path}");
            Console.WriteLine($"El archivo existe: {System.IO.File.Exists(path)}");

            if (!System.IO.File.Exists(path))
            {
                return NotFound($"Archivo no encontrado: {archivo} en {path}");
            }

            return PhysicalFile(path, "model/gltf-binary");
        }

        [HttpGet("Test")]
        public IActionResult Test()
        {
            return Content("El controlador RealidadAumentada está funcionando correctamente");
        }

        [HttpGet("TestArchivos")]
        public IActionResult TestArchivos()
        {
            var modelsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models3d");
            var resultado = new List<string>();

            resultado.Add($"<h4>📁 Información del Directorio</h4>");
            resultado.Add($"<strong>Ruta:</strong> {modelsPath}");
            resultado.Add($"<strong>Existe:</strong> {(Directory.Exists(modelsPath) ? "✅ Sí" : "❌ No")}");

            if (Directory.Exists(modelsPath))
            {
                var archivos = Directory.GetFiles(modelsPath, "*.glb");
                resultado.Add($"<strong>Archivos .glb encontrados:</strong> {archivos.Length}");

                if (archivos.Length > 0)
                {
                    resultado.Add("<h4>📄 Lista de Archivos:</h4>");
                    resultado.Add("<table style='width:100%; border-collapse: collapse;'>");
                    resultado.Add("<tr style='background: #f0f0f0;'><th style='border: 1px solid #ddd; padding: 8px;'>Archivo</th><th style='border: 1px solid #ddd; padding: 8px;'>Tamaño</th><th style='border: 1px solid #ddd; padding: 8px;'>Última modificación</th></tr>");

                    foreach (var archivo in archivos)
                    {
                        var info = new FileInfo(archivo);
                        var sizeKB = info.Length / 1024.0;
                        var sizeDisplay = sizeKB < 1024 ? $"{sizeKB:F1} KB" : $"{sizeKB / 1024:F1} MB";

                        resultado.Add($"<tr>");
                        resultado.Add($"<td style='border: 1px solid #ddd; padding: 8px;'>{info.Name}</td>");
                        resultado.Add($"<td style='border: 1px solid #ddd; padding: 8px;'>{sizeDisplay}</td>");
                        resultado.Add($"<td style='border: 1px solid #ddd; padding: 8px;'>{info.LastWriteTime:yyyy-MM-dd HH:mm}</td>");
                        resultado.Add($"</tr>");
                    }
                    resultado.Add("</table>");
                }
                else
                {
                    resultado.Add("<p style='color: red;'>❌ No se encontraron archivos .glb en el directorio</p>");
                }

                // Verificar permisos
                try
                {
                    var testFile = Path.Combine(modelsPath, "test_permissions.tmp");
                    System.IO.File.WriteAllText(testFile, "test");
                    System.IO.File.Delete(testFile);
                    resultado.Add("<p style='color: green;'>✅ Permisos de lectura/escritura: OK</p>");
                }
                catch (Exception ex)
                {
                    resultado.Add($"<p style='color: orange;'>⚠️ Problema con permisos: {ex.Message}</p>");
                }
            }
            else
            {
                resultado.Add("<p style='color: red;'>❌ El directorio models3d no existe. Debe crearlo en wwwroot/models3d/</p>");

                // Sugerir crear el directorio
                try
                {
                    Directory.CreateDirectory(modelsPath);
                    resultado.Add("<p style='color: green;'>✅ Directorio creado automáticamente</p>");
                }
                catch (Exception ex)
                {
                    resultado.Add($"<p style='color: red;'>❌ No se pudo crear el directorio: {ex.Message}</p>");
                }
            }

            // Información adicional del sistema
            resultado.Add("<h4>💻 Información del Sistema:</h4>");
            resultado.Add($"<strong>Directorio de trabajo:</strong> {Directory.GetCurrentDirectory()}");
            resultado.Add($"<strong>Directorio wwwroot:</strong> {Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")}");
            resultado.Add($"<strong>wwwroot existe:</strong> {(Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")) ? "✅ Sí" : "❌ No")}");

            return Content(string.Join("<br>", resultado), "text/html");
        }

        private string DeterminarModelo3D(string nombreProducto)
        {
            if (string.IsNullOrEmpty(nombreProducto)) return "";

            string nombreNormalizado = nombreProducto.ToLower().Trim();

            switch (nombreNormalizado)
            {
                case "pepperoni":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_pepperoni.glb";
                case "margarita":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_margarita.glb";
                case "cheddar":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_cheddar.glb";
                case "diavola":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_diavola.glb";
                case "say cheese":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_say_cheese.glb";
                case "verace":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_verace.glb";
                case "meat lovers":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_meat_lovers.glb";
                case "mi champ":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_michamp.glb";
                case "veggie lovers":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_veggielovers.glb";
                case "hawaiana":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza_h.glb";
                case "tradicional":
                    return "/RealidadAumentada/GetGLBFile?archivo=sandwich_tradicional.glb";
                case "carne mechada":
                    return "/RealidadAumentada/GetGLBFile?archivo=sandwich_carne_mechada.glb";
                case "veggie":
                    return "/RealidadAumentada/GetGLBFile?archivo=sandwich_veggie.glb";
                case "nachos cheddar":
                    return "/RealidadAumentada/GetGLBFile?archivo=nachos_cheddar.glb";
                case "nachos verace":
                    return "/RealidadAumentada/GetGLBFile?archivo=nachos_verace.glb";
                case "bread sticks":
                    return "/RealidadAumentada/GetGLBFile?archivo=breadsticks.glb";
                case "bread sticks verace":
                    return "/RealidadAumentada/GetGLBFile?archivo=breadsticks.glb";
                default:
                    return "";
            }
        }

        private string DeterminarModeloPorId(int id)
        {
            // Mapeo específico de IDs a archivos de modelo (solo los que funcionan)
            switch (id)
            {
                case 1:
                    return "pizza_pepperoni.glb";
                case 2:
                    return "pizza_michamp.glb";
                case 3:
                    return "pizza_h.glb"; // Hawaiana
                case 4:
                    return "pizza_margarita.glb";
                case 5:
                    return "pizza_cheddar.glb";
                case 6:
                    return "pizza_diavola.glb";
                case 7:
                    return "pizza_meat_lovers.glb";
                case 8:
                    return "pizza_veggielovers.glb";
                case 9:
                    return "pizza_say_cheese.glb";
                case 10:
                    return "pizza_verace.glb";
                case 69:
                    return "sandwich_tradicional.glb";
                case 70:
                    return "sandwich_carne_mechada.glb";
                case 71:
                    return "sandwich_veggie.glb";
                case 76:
                    return "nachos_cheddar.glb";
                case 77:
                    return "nachos_verace.glb";
                case 78:
                    return "breadsticks.glb";
                case 79:
                    return "breadsticks.glb";
                default:
                    return "pizza2.glb"; // Por defecto para cualquier ID no mapeado
            }
        }
    }
}