using Microsoft.EntityFrameworkCore;
using ServicoMapa.Models;

namespace Exemplo
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<CorporacaoBombeiro> CorporacoesBombeiro { get; set; }
        public DbSet<OcorrenciaIncendio> OcorrenciasIncendio { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CorporacaoBombeiro>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Nome).IsRequired().HasMaxLength(200);
                e.Property(c => c.Endereco).HasMaxLength(400);
            });

            modelBuilder.Entity<OcorrenciaIncendio>(e =>
            {
                e.HasKey(o => o.Id);
                e.Property(o => o.Descricao).HasMaxLength(500);
                e.HasOne(o => o.CorporacaoAtendeu)
                 .WithMany()
                 .HasForeignKey(o => o.CorporacaoAtendeuId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
