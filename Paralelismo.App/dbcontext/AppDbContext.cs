using Microsoft.EntityFrameworkCore;
using Paralelismo.App.Entities;

namespace Paralelismo.App.Data
{
    public class AppDbContext : DbContext
    {
        private readonly string _connectionString;

        public AppDbContext()
        {
            // Cadena por defecto a LocalDB. Puedes cambiarla en tiempo de ejecuci�n
            _connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=ParalelismoDb;Trusted_Connection=True;";
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Factura> Facturas { get; set; } = null!;
        public DbSet<Marca> Marcas { get; set; } = null!;
        public DbSet<ArticuloVendido> ArticulosVendidos { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // optionsBuilder.UseSqlServer(_connectionString);
                optionsBuilder.UseInMemoryDatabase("MiDbEnMemoria");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cliente>(b =>
            {
                b.HasKey(p => p.Id);
                b.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
                b.Property(p => p.Apellido).HasMaxLength(200).IsRequired(false);
                b.Property(p => p.Email).HasMaxLength(200).IsRequired(false);
                b.Property(p => p.Cedula).HasMaxLength(50).IsRequired(false);
            });

            modelBuilder.Entity<Factura>(b =>
            {
                b.HasKey(p => p.Id);
            });

            modelBuilder.Entity<Marca>(b =>
            {
                b.HasKey(p => p.Id);
            });

            modelBuilder.Entity<ArticuloVendido>(b =>
            {
                b.HasKey(p => p.Id);
            });
        }
    }
}
