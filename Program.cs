using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using ProyectoIdentity.Services;
using ProyectoIdentity.Servicios;
//using static ProyectoIdentity.Controllers.UsuariosController;

var builder = WebApplication.CreateBuilder(args);

// Configuración adicional para archivos de configuración
builder.Configuration.AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Configuración de la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSql")));

// Configuración de CORS
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Configuración de controladores
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


// Configuración de Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Configuración de opciones de Identity
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 10;

    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 0;
})
.AddPasswordValidator<CustomPasswordValidator>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddErrorDescriber<CustomIdentityErrorDescriber>()
.AddDefaultTokenProviders();

// Configuración de cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = new PathString("/Cuentas/Acceso");
    options.AccessDeniedPath = new PathString("/Cuentas/Denegado");
});

// Configuración de localización
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("es-ES") };
    options.DefaultRequestCulture = new RequestCulture("es-ES");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Configuración de sesión
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Servicios adicionales básicos
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IEmailSender, MailJetEmailSender>();

// ========================================
// SERVICIOS PARA IA Y RECOMENDACIONES (SIN OLLAMA)
// ========================================

// HttpClient para OpenAIService
builder.Services.AddHttpClient<OpenAIService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Timeout de 5 minutos para OpenAI
});

// Servicios de IA
builder.Services.AddScoped<OpenAIService>(); // ✅ Solo OpenAI
builder.Services.AddScoped<RecomendadorProductos>(); // ✅ Sin dependencia de Ollama

// Configuración de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

builder.Logging.SetMinimumLevel(LogLevel.Information);

// Construcción de la aplicación
var app = builder.Build();

// Configuración del pipeline de middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Inicialización de la base de datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
    var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Verificando conexión a base de datos...");

        // Solo verificar conexión, NO ejecutar migraciones
        if (context.Database.CanConnect())
        {
            logger.LogInformation("✅ Conexión a base de datos exitosa");

            // Verificar si existen las tablas del chat
            try
            {
                var solicitudesCount = context.SolicitudesChat.CountAsync().Result;
                var productosCount = context.Productos.CountAsync().Result;
                logger.LogInformation("✅ Tablas del sistema disponibles - Productos: {ProductosCount}, Solicitudes: {SolicitudesCount}", productosCount, solicitudesCount);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "⚠️ Algunas tablas del sistema no están disponibles. Ejecuta el script SQL proporcionado.");
            }
        }
        else
        {
            logger.LogError("❌ No se puede conectar a la base de datos");
        }

        // Inicializar datos existentes (si las tablas existen)
        try
        {
    DbInitializer.Initialize(context);
            logger.LogInformation("✅ DbInitializer ejecutado");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ Error en DbInitializer (puede ser normal si faltan tablas)");
        }

        try
        {
    RecompensasInitializer.Initialize(context);
            logger.LogInformation("✅ RecompensasInitializer ejecutado");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ Error en RecompensasInitializer (puede ser normal si faltan tablas)");
        }

        logger.LogInformation("✅ Verificación de base de datos completada");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error al verificar la base de datos");
        // NO lanzar excepción para que la app pueda iniciar sin problemas
    }
}

// Middleware
 app.UseHttpsRedirection();

// Configuración de archivos estáticos
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".glb"] = "model/gltf-binary";

// Usa archivos estáticos con la configuración personalizada
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();

// Middleware de CORS, sesión y autenticación
app.UseCors();
app.UseSession();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();

// Mapeo de rutas para APIs
app.MapControllers();

// Rutas específicas para el sistema de chat
app.MapControllerRoute(
    name: "chatApi",
    pattern: "api/chat/{action}",
    defaults: new { controller = "Home" });

app.MapControllerRoute(
    name: "recomendacionesApi",
    pattern: "api/recomendaciones/{action}",
    defaults: new { controller = "Recomendaciones" });

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapear Razor Pages
app.MapRazorPages();

// ========================================
// INICIALIZACIÓN DEL SISTEMA DE RECOMENDACIONES (SIN OLLAMA)
// ========================================
await InicializarSistemaRecomendaciones(app.Services);

// ========================================
// VERIFICACIÓN DEL SISTEMA AL INICIO
// ========================================
await VerificarSistema(app.Services);

app.Run();

// ========================================
// MÉTODOS AUXILIARES
// ========================================

static async Task InicializarSistemaRecomendaciones(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Inicializando sistema de recomendaciones con IA...");

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var recomendador = scope.ServiceProvider.GetRequiredService<RecomendadorProductos>();
        var openAIService = scope.ServiceProvider.GetRequiredService<OpenAIService>();

        // Obtener productos de la base de datos
        var productos = await context.Productos
            .Where(p => !string.IsNullOrEmpty(p.Nombre) &&
                       !string.IsNullOrEmpty(p.Categoria) &&
                       p.Cantidad > 0)
            .ToListAsync();

        if (!productos.Any())
        {
            logger.LogWarning("No se encontraron productos en la base de datos para inicializar el recomendador");
            return;
        }

        // Inicializar el recomendador
        recomendador.Inicializar(productos);

        logger.LogInformation("Sistema de recomendaciones inicializado con {Cantidad} productos", productos.Count);

        // Mostrar estadísticas útiles
        var categorias = productos
            .Where(p => !string.IsNullOrEmpty(p.Categoria))
            .GroupBy(p => p.Categoria)
            .Select(g => new { Categoria = g.Key, Cantidad = g.Count() })
            .OrderByDescending(x => x.Cantidad)
            .ToList();

        logger.LogInformation("Categorías disponibles:");
        foreach (var cat in categorias)
        {
            logger.LogInformation("- {Categoria}: {Cantidad} productos", cat.Categoria, cat.Cantidad);
        }

        var sinGluten = productos.Count(p => !ContieneGluten(p.Alergenos));
        var sinAlergenos = productos.Count(p => string.IsNullOrEmpty(p.Alergenos) || p.Alergenos == "NULL");

        logger.LogInformation("Productos sin gluten: {Cantidad}", sinGluten);
        logger.LogInformation("Productos sin alérgenos: {Cantidad}", sinAlergenos);

        // Verificar conexión con OpenAI
        try
        {
            var conexionOpenAI = await openAIService.VerificarConexion();
            if (conexionOpenAI)
            {
                logger.LogInformation("✅ Conexión con OpenAI exitosa");
            }
            else
            {
                logger.LogWarning("⚠️ No se pudo conectar con OpenAI. El sistema funcionará con recomendaciones básicas.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ Error verificando conexión con OpenAI");
        }

        logger.LogInformation("🚀 Sistema de recomendaciones listo para usar");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error inicializando el sistema de recomendaciones");
        // No lanzar excepción para que la app pueda funcionar sin IA
    }
}

static async Task VerificarSistema(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("=== VERIFICACIÓN DEL SISTEMA ===");

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Verificar base de datos
        var totalProductos = await context.Productos.CountAsync();
        var productosDisponibles = await context.Productos.CountAsync(p => p.Cantidad > 0);

        logger.LogInformation("📊 Base de datos:");
        logger.LogInformation("   - Total productos: {Total}", totalProductos);
        logger.LogInformation("   - Productos disponibles: {Disponibles}", productosDisponibles);

        // Verificar tablas del sistema de chat
        try
        {
            var solicitudesChat = await context.SolicitudesChat.CountAsync();
            var historialChat = await context.HistorialChat.CountAsync();
            logger.LogInformation("   - Solicitudes de chat: {Solicitudes}", solicitudesChat);
            logger.LogInformation("   - Historial de chat: {Historial}", historialChat);
        }
        catch (Exception)
        {
            logger.LogWarning("⚠️ Tablas de chat no disponibles. Ejecutar el script SQL proporcionado.");
        }

        logger.LogInformation("=== SISTEMA LISTO ===");
        logger.LogInformation("🌐 Accede al chat en: /Home/Chat");
        logger.LogInformation("🔧 API disponible en: /api/chat/mensaje");
        logger.LogInformation("📈 Métricas en: /api/recomendaciones/verificar");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error en verificación del sistema");
    }
}

static bool ContieneGluten(string? alergenos)
{
    if (string.IsNullOrEmpty(alergenos) || alergenos == "NULL")
        return false;

    return alergenos.Contains("Gluten", StringComparison.OrdinalIgnoreCase);
}