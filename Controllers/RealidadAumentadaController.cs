using Microsoft.AspNetCore.Mvc;
using ProyectoIdentity.Datos;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace ProyectoIdentity.Controllers
{
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
                ViewBag.ModeloArchivo = "pizza1.glb";
                ViewBag.ModeloPath = "/RealidadAumentada/GetGLBFile?archivo=pizza1.glb";
            }
            else
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null) return NotFound();

                ViewBag.ProductoId = id;
                ViewBag.ProductoNombre = producto.Nombre;

                // Determinar archivo según ID
                string archivoModelo = DeterminarModeloPorId(id.Value);
                ViewBag.ModeloArchivo = archivoModelo;
                ViewBag.ModeloPath = $"/RealidadAumentada/GetGLBFile?archivo={archivoModelo}";
            }

            return View();
        }

        // Método modificado para servir archivos específicos
        [HttpGet("GetGLBFile")]
        public IActionResult GetGLBFile(string archivo = "pizza1.glb")
        {
            // Validar que el archivo solicitado sea válido
            var archivosPermitidos = new[] { "pizza1.glb", "pizza2.glb", "pizza3.glb", "pizza4.glb" };
            if (!archivosPermitidos.Contains(archivo))
            {
                archivo = "pizza1.glb"; // Archivo por defecto
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

        [HttpGet("Debug")]
        public async Task<IActionResult> Debug(int? id)
        {
            if (id == null)
            {
                ViewBag.ProductoId = 1;
                ViewBag.ProductoNombre = "Debug - Pizza Por Defecto";
                ViewBag.ModeloArchivo = "pizza1.glb";
                ViewBag.ModeloPath = "/RealidadAumentada/GetGLBFile?archivo=pizza1.glb";
            }
            else
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null) return NotFound();

                ViewBag.ProductoId = id;
                ViewBag.ProductoNombre = $"Debug - {producto.Nombre}";

                string archivoModelo = DeterminarModeloPorId(id.Value);
                ViewBag.ModeloArchivo = archivoModelo;
                ViewBag.ModeloPath = $"/RealidadAumentada/GetGLBFile?archivo={archivoModelo}";
            }

            return View();
        }

        private string DeterminarModelo3D(string nombreProducto)
        {
            if (string.IsNullOrEmpty(nombreProducto)) return "";

            string nombreNormalizado = nombreProducto.ToLower().Trim();

            switch (nombreNormalizado)
            {
                case "pepperoni":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza1.glb";
                case "mi champ":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza2.glb";
                case "say cheese":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza3.glb";
                case "verace":
                    return "/RealidadAumentada/GetGLBFile?archivo=pizza4.glb";
                default:
                    return "";
            }
        }

        private string DeterminarModeloPorId(int id)
        {
            // Mapeo específico de IDs a archivos de modelo
            switch (id)
            {
                case 1:
                    return "pizza1.glb";
                case 2:
                    return "pizza2.glb";
                case 9:
                    return "pizza3.glb";
                case 10:
                    return "pizza4.glb";
                default:
                    return "pizza1.glb"; // Por defecto
            }
        }
    }
}