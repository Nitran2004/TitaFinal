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


        // Método para actualizar DbContext (agregar a ApplicationDbContext.cs)
        public DbSet<Cupon> Cupones { get; set; }
        public DbSet<CuponCanjeado> CuponesCanjeados { get; set; }

        // Entidades existentes
        public DbSet<AppUsuario> AppUsuario { get; set; }
        public DbSet<CollectionPoint> CollectionPoints { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<PedidoProducto> PedidoProductos { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }

        // Entidades del sistema de fidelización - existentes (mantenemos tu estructura)
        public DbSet<ProductoRecompensa> ProductosRecompensa { get; set; }
        public DbSet<UsuarioPuntos> UsuarioPuntos { get; set; }
        public DbSet<HistorialCanje> HistorialCanjes { get; set; }

        // Solo agregamos TransaccionPuntos para el historial
        public DbSet<TransaccionPuntos> TransaccionesPuntos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Importante: esto debe ir al inicio

            // Configuraciones para las tablas existentes (mantenemos tu estructura)
            modelBuilder.Entity<ProductoRecompensa>().ToTable("ProductosRecompensa");
            modelBuilder.Entity<UsuarioPuntos>().ToTable("UsuarioPuntos");
            modelBuilder.Entity<HistorialCanje>().ToTable("HistorialCanjes");

            // Solo agregamos la nueva tabla de transacciones
            modelBuilder.Entity<TransaccionPuntos>().ToTable("TransaccionesPuntos");

            // Configuraciones específicas para las relaciones
            // Configurar relación ProductoRecompensa -> Producto
            modelBuilder.Entity<ProductoRecompensa>()
            .HasOne(pr => pr.Producto)
            .WithMany()
            .HasForeignKey(pr => pr.ProductoId)
            .OnDelete(DeleteBehavior.SetNull);

            // Configuración para Cupon
            modelBuilder.Entity<Cupon>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CodigoQR).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TipoDescuento).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ValorDescuento).HasColumnType("decimal(10,2)");
                entity.Property(e => e.MontoMinimo).HasColumnType("decimal(10,2)");

                // Índice único para el código QR
                entity.HasIndex(e => e.CodigoQR).IsUnique();
            });

            // Configuración para CuponCanjeado
            modelBuilder.Entity<CuponCanjeado>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CodigoQR).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalOriginal).HasColumnType("decimal(10,2)");
                entity.Property(e => e.DescuentoAplicado).HasColumnType("decimal(10,2)");
                entity.Property(e => e.TotalConDescuento).HasColumnType("decimal(10,2)");

                // Relación con Cupon
                entity.HasOne(e => e.Cupon)
                      .WithMany()
                      .HasForeignKey(e => e.CuponId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relación con Usuario (opcional)
                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Relación con Pedido (opcional)
                entity.HasOne(e => e.Pedido)
                      .WithMany()
                      .HasForeignKey(e => e.PedidoId)
                      .OnDelete(DeleteBehavior.SetNull);

            });  // <-- AGREGAR esta llave que falta

            // Configuración de Pedido
            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Fecha).IsRequired();
                entity.Property(e => e.Estado).HasMaxLength(50).HasDefaultValue("Pendiente");

                // Relación con Sucursal
                entity.HasOne(e => e.Sucursal)
                      .WithMany()
                      .HasForeignKey(e => e.SucursalId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relación con Usuario (opcional)
                entity.HasOne<AppUsuario>()
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración de PedidoProducto
            modelBuilder.Entity<PedidoProducto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Precio).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Cantidad).IsRequired();

                // Relación con Pedido
                entity.HasOne(e => e.Pedido)
                      .WithMany(p => p.PedidoProductos)
                      .HasForeignKey(e => e.PedidoId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relación con Producto
                entity.HasOne(e => e.Producto)
                      .WithMany()
                      .HasForeignKey(e => e.ProductoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Producto
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Precio).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Categoria).HasMaxLength(50);
                entity.Property(e => e.Descripcion).HasMaxLength(500);
            });

            // Configuración de TransaccionPuntos
            modelBuilder.Entity<TransaccionPuntos>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Puntos).IsRequired();
                entity.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Descripcion).HasMaxLength(200);
                entity.Property(e => e.Fecha).IsRequired().HasDefaultValueSql("GETDATE()");

                // Relación con Usuario
                entity.HasOne<AppUsuario>()
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índice para consultas eficientes
                entity.HasIndex(e => new { e.UsuarioId, e.Fecha });
            });

            // Configuración de AppUsuario (extendiendo IdentityUser)
            modelBuilder.Entity<AppUsuario>(entity =>
            {
                entity.Property(e => e.Nombre).HasMaxLength(100);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.Pais).HasMaxLength(50);
                entity.Property(e => e.Ciudad).HasMaxLength(50);
                entity.Property(e => e.Direccion).HasMaxLength(200);
                entity.Property(e => e.CodigoPais).HasMaxLength(10);
                entity.Property(e => e.Estado).HasMaxLength(50);
                entity.Property(e => e.Url).HasMaxLength(200);
                entity.Property(e => e.PuntosFidelidad).HasDefaultValue(0);

                // Índice para búsquedas por puntos
                entity.HasIndex(e => e.PuntosFidelidad);
            });

            // Configuración de Sucursal
            modelBuilder.Entity<Sucursal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Direccion).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Latitud).HasColumnType("decimal(10,8)");
                entity.Property(e => e.Longitud).HasColumnType("decimal(11,8)");
            });

            // Configuración de CollectionPoint
            modelBuilder.Entity<CollectionPoint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(200);

                // Relación con Sucursal
                entity.HasOne(e => e.Sucursal)
                      .WithMany()
                      .HasForeignKey(e => e.SucursalId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuraciones adicionales para las entidades existentes del sistema de recompensas

            // ProductoRecompensa
            modelBuilder.Entity<ProductoRecompensa>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Agregar configuraciones adicionales si es necesario
            });

            // UsuarioPuntos
            modelBuilder.Entity<UsuarioPuntos>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Agregar configuraciones adicionales si es necesario
            });

            // HistorialCanje
            modelBuilder.Entity<HistorialCanje>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Agregar configuraciones adicionales si es necesario
            });
        }


    }
}