using Microsoft.EntityFrameworkCore;
using ServicoMapa.Models;

namespace Exemplo
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<OcorrenciaIncendio> OcorrenciasIncendio { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OcorrenciaIncendio>(e =>
            {
                e.HasKey(o => o.Id);
                e.Property(o => o.Descricao).HasMaxLength(500);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
