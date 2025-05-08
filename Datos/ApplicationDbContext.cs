using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Models;
namespace ProyectoIdentity.Datos
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<AppUsuario> AppUsuario { get; set; }

        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Tarea> Tareas { get; set; }
        public DbSet<Proyecto> Proyectos { get; set; }

        public DbSet<CollectionPoint> CollectionPoints { get; set; }

        public DbSet<Pedido> Pedidos { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<PedidoProducto> PedidoProductos { get; set; }

        public DbSet<ProductoRecompensa> ProductosRecompensa { get; set; }
        public DbSet<UsuarioPuntos> UsuarioPuntos { get; set; }
        public DbSet<HistorialCanje> HistorialCanjes { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Mueve esta línea al inicio.

            // Relación entre Tarea y Proyecto
            modelBuilder.Entity<Tarea>()
                .HasOne(t => t.Proyecto)
                .WithMany(p => p.Tareas)
                .HasForeignKey(t => t.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade); // Elimina las tareas si se elimina el proyecto.

            // Relación entre Tarea y Empleado
            modelBuilder.Entity<Tarea>()
                .HasOne(t => t.Empleado)
                .WithMany(e => e.Tareas)
                .HasForeignKey(t => t.EmpleadoId)
                .OnDelete(DeleteBehavior.SetNull); // Si se elimina el empleado, se establece como null.
                                                   // Configuración específica para Pedido

            // Configuraciones para las nuevas tablas
            modelBuilder.Entity<ProductoRecompensa>().ToTable("ProductosRecompensa");
            modelBuilder.Entity<UsuarioPuntos>().ToTable("UsuarioPuntos");
            modelBuilder.Entity<HistorialCanje>().ToTable("HistorialCanjes");



        }

    }
}
