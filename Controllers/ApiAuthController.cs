// ============== CREAR ESTE ARCHIVO: ApiAuthController.cs ==============

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProyectoIdentity.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class ApiAuthController : ControllerBase
    {
        [HttpGet("verificar")]
        public IActionResult VerificarAutenticacion()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var userName = User.Identity.Name;
                    var email = User.FindFirstValue(ClaimTypes.Email);
                    var roles = User.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToArray();

                    return Ok(new
                    {
                        success = true,
                        autenticado = true,
                        usuario = new
                        {
                            id = userId,
                            nombre = userName,
                            email = email,
                            roles = roles,
                            esAdmin = roles.Contains("Administrador")
                        },
                        mensaje = "Usuario autenticado correctamente"
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        autenticado = false,
                        mensaje = "Usuario no autenticado",
                        loginUrl = "/Cuentas/Acceso"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    mensaje = "Error verificando autenticación",
                    error = ex.Message
                });
            }
        }
    }
}