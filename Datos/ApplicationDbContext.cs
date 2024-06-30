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
        //Agregamos los diferentes modelos que necesitamos
        //
        public DbSet<AppUsuario> AppUsuario { get; set; }
        public DbSet<Meta> Metas { get; set; }

        public DbSet<Alineamiento> Alineamientos { get; set; }
        public DbSet<Modelo> Modelos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Alineamiento>()
                .HasMany(a => a.Modelos)
                .WithOne(m => m.Alineamiento)
                .HasForeignKey(m => m.AlineamientoID)
                .OnDelete(DeleteBehavior.Cascade); // Configura la eliminación en cascada
        }
    }
}
