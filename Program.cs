using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using ProyectoIdentity.Servicios;
using static ProyectoIdentity.Controllers.UsuariosController;

var builder = WebApplication.CreateBuilder(args);

// Configuramos la conexión a SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSql"))
);

// ① Añade CORS
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ② Asegúrate de incluir AddControllers (necesario para MapControllers)
builder.Services.AddControllers();       // <-- para tus api/[controller]
builder.Services.AddControllersWithViews(); // <-- para MVC normal


// Agregar el servicio de MySQL en el contexto de la aplicación.
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
//        new MySqlServerVersion(new Version(8, 0, 25))));

// Agregar el servicio Identity a la aplicación
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddPasswordValidator<CustomPasswordValidator>() // Añadir el validador personalizado
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<CustomIdentityErrorDescriber>() // Aquí se agrega el describidor personalizado
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IPasswordHasher<IdentityUser>, PlainTextPasswordHasher>();


// Configuración de las opciones de Identity
builder.Services.Configure<IdentityOptions>(options =>
{
    // Configuración de bloqueo
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 10;

    // Desactivar validadores predeterminados
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 0;
});

// Configuración de la URL de retorno al acceder
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = new PathString("/Cuentas/Acceso");
    options.AccessDeniedPath = new PathString("/Cuentas/Denegado");
});

// Configuramos localización
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("es-ES") }; // Cultura soportada (español)
    options.DefaultRequestCulture = new RequestCulture("es-ES"); // Cultura predeterminada
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Register IHttpContextAccessor for session access
builder.Services.AddHttpContextAccessor();

// Se agrega IEmailSender
builder.Services.AddTransient<IEmailSender, MailJetEmailSender>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<CartService>();


// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache(); // Almacén en memoria para la sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiración de la sesión
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configurar el middleware de localización
app.UseRequestLocalization();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();
app.UseSession(); // Habilitar uso de sesiones

// Se agrega la autenticación
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
