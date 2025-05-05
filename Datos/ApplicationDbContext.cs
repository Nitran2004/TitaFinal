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
        }

    }
}
