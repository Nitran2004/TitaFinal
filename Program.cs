using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;

var builder = WebApplication.CreateBuilder(args);

//configuramos la conexion a sql server
builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
   opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSql"))
   );

//Agregar el servicio Identity a la aplicacion
builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();


//esta linea es para la url de retorno al acceder
builder.Services.ConfigureApplicationCookie(options =>
{;
    options.LoginPath = new PathString("/Cuentas/Acceso");
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
//se agrega la autenticacionzzz
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
