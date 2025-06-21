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

        // ✅ DBSETS
        public DbSet<AppUsuario> AppUsuario { get; set; }
        public DbSet<CollectionPoint> CollectionPoints { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<PedidoProducto> PedidoProductos { get; set; }
        public DbSet<PedidoDetalle> PedidoDetalles { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<Cupon> Cupones { get; set; }
        public DbSet<CuponCanjeado> CuponesCanjeados { get; set; }

        // Sistema de fidelización
        public DbSet<ProductoRecompensa> ProductosRecompensa { get; set; }
        public DbSet<UsuarioPuntos> UsuarioPuntos { get; set; }
        public DbSet<HistorialCanje> HistorialCanjes { get; set; }
        public DbSet<TransaccionPuntos> TransaccionesPuntos { get; set; }
        public DbSet<Valoracion> Valoraciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ CONFIGURACIÓN CORREGIDA PARA PEDIDO
            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Fecha).IsRequired();
                entity.Property(e => e.Estado).HasMaxLength(50).HasDefaultValue("Preparándose");
                entity.Property(e => e.TipoServicio).HasMaxLength(50);
                entity.Property(e => e.Comentario).HasMaxLength(500);

                // ✅ Relación con Usuario (SIN propiedad de navegación)
                entity.HasOne<AppUsuario>()
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                // ✅ Relación con Sucursal
                entity.HasOne(e => e.Sucursal)
                      .WithMany()
                      .HasForeignKey(e => e.SucursalId)
                      .IsRequired(true)
                      .OnDelete(DeleteBehavior.Restrict);

                // ✅ Relación con PuntoRecoleccion (opcional)
                entity.HasOne(e => e.PuntoRecoleccion)
                      .WithMany()
                      .HasForeignKey(e => e.PuntoRecoleccionId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                // ✅ Relación con PedidoProductos
                entity.HasMany(e => e.PedidoProductos)
                      .WithOne(pp => pp.Pedido)
                      .HasForeignKey(pp => pp.PedidoId)
                      .OnDelete(DeleteBehavior.Cascade);

                // ✅ Relación con Detalles
                entity.HasMany(e => e.Detalles)
                      .WithOne(d => d.Pedido)
                      .HasForeignKey(d => d.PedidoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ✅ CONFIGURACIÓN PARA PEDIDOPRODUCTO
            modelBuilder.Entity<PedidoProducto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Precio).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Cantidad).IsRequired();

                entity.HasOne(e => e.Pedido)
                      .WithMany(p => p.PedidoProductos)
                      .HasForeignKey(e => e.PedidoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Producto)
                      .WithMany()
                      .HasForeignKey(e => e.ProductoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ✅ CONFIGURACIÓN PARA PEDIDODETALLE
            modelBuilder.Entity<PedidoDetalle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("PedidoDetalle");
                entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Cantidad).IsRequired();
                entity.Property(e => e.IngredientesRemovidos).HasMaxLength(1000);
                entity.Property(e => e.NotasEspeciales).HasMaxLength(500);

                entity.HasOne(e => e.Pedido)
                      .WithMany(p => p.Detalles)
                      .HasForeignKey(e => e.PedidoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Producto)
                      .WithMany()
                      .HasForeignKey(e => e.ProductoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ✅ CONFIGURACIÓN PARA PRODUCTO
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Precio).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Categoria).HasMaxLength(50);
                entity.Property(e => e.Descripcion).HasMaxLength(500);
                entity.Property(e => e.InfoNutricional).HasMaxLength(1000);
                entity.Property(e => e.Alergenos).HasMaxLength(500);
                entity.Property(e => e.Ingredientes).HasColumnType("nvarchar(max)");
            });

            // ✅ CONFIGURACIÓN PARA APPUSUARIO
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

                entity.HasIndex(e => e.PuntosFidelidad);
            });

            // ✅ CONFIGURACIÓN PARA SUCURSAL
            modelBuilder.Entity<Sucursal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Direccion).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Latitud).HasColumnType("decimal(10,8)");
                entity.Property(e => e.Longitud).HasColumnType("decimal(11,8)");
            });

            // ✅ CONFIGURACIÓN PARA COLLECTIONPOINT
            modelBuilder.Entity<CollectionPoint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(200);

                entity.HasOne(e => e.Sucursal)
                      .WithMany()
                      .HasForeignKey(e => e.SucursalId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ✅ CONFIGURACIÓN PARA TRANSACCIONPUNTOS
            modelBuilder.Entity<TransaccionPuntos>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("TransaccionesPuntos");
                entity.Property(e => e.Puntos).IsRequired();
                entity.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Descripcion).HasMaxLength(200);
                entity.Property(e => e.Fecha).IsRequired().HasDefaultValueSql("GETDATE()");

                entity.HasOne<AppUsuario>()
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UsuarioId, e.Fecha });
            });

            // ✅ CONFIGURACIÓN PARA CUPON
            modelBuilder.Entity<Cupon>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CodigoQR).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TipoDescuento).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ValorDescuento).HasColumnType("decimal(10,2)");
                entity.Property(e => e.MontoMinimo).HasColumnType("decimal(10,2)");

                entity.HasIndex(e => e.CodigoQR).IsUnique();
            });

            // ✅ CONFIGURACIÓN PARA CUPONCANJEADO
            modelBuilder.Entity<CuponCanjeado>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CodigoQR).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalOriginal).HasColumnType("decimal(10,2)");
                entity.Property(e => e.DescuentoAplicado).HasColumnType("decimal(10,2)");
                entity.Property(e => e.TotalConDescuento).HasColumnType("decimal(10,2)");

                entity.HasOne(e => e.Cupon)
                      .WithMany()
                      .HasForeignKey(e => e.CuponId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Usuario)
                      .WithMany()
                      .HasForeignKey(e => e.UsuarioId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Pedido)
                      .WithMany()
                      .HasForeignKey(e => e.PedidoId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ✅ CONFIGURACIONES PARA SISTEMA DE RECOMPENSAS
            modelBuilder.Entity<ProductoRecompensa>(entity =>
            {
                entity.ToTable("ProductosRecompensa");
                entity.HasKey(e => e.Id);

                entity.HasOne(pr => pr.Producto)
                      .WithMany()
                      .HasForeignKey(pr => pr.ProductoId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UsuarioPuntos>(entity =>
            {
                entity.ToTable("UsuarioPuntos");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<HistorialCanje>(entity =>
            {
                entity.ToTable("HistorialCanjes");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Valoracion>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }
}