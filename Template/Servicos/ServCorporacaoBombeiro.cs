using Exemplo;
using Microsoft.EntityFrameworkCore;
using ServicoMapa.Models;

namespace ServicoMapa.Servicos
{
    public interface IServCorporacaoBombeiro
    {
        Task<List<CorporacaoBombeiro>> ListarAsync();
        Task<CorporacaoBombeiro?> ObterPorIdAsync(int id);
        Task<CorporacaoBombeiro> CriarAsync(CorporacaoBombeiro corporacao);
        Task<CorporacaoBombeiro?> AtualizarAsync(int id, CorporacaoBombeiro corporacao);
        Task<bool> RemoverAsync(int id);
    }

    public class ServCorporacaoBombeiro : IServCorporacaoBombeiro
    {
        private readonly DataContext _dataContext;

        public ServCorporacaoBombeiro(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public Task<List<CorporacaoBombeiro>> ListarAsync() =>
            _dataContext.CorporacoesBombeiro.OrderBy(c => c.Nome).ToListAsync();

        public Task<CorporacaoBombeiro?> ObterPorIdAsync(int id) =>
            _dataContext.CorporacoesBombeiro.FirstOrDefaultAsync(c => c.Id == id);

        public async Task<CorporacaoBombeiro> CriarAsync(CorporacaoBombeiro corporacao)
        {
            _dataContext.CorporacoesBombeiro.Add(corporacao);
            await _dataContext.SaveChangesAsync();
            return corporacao;
        }

        public async Task<CorporacaoBombeiro?> AtualizarAsync(int id, CorporacaoBombeiro corporacao)
        {
            var existente = await _dataContext.CorporacoesBombeiro.FindAsync(id);
            if (existente is null)
                return null;

            existente.Nome = corporacao.Nome;
            existente.Endereco = corporacao.Endereco;
            existente.Latitude = corporacao.Latitude;
            existente.Longitude = corporacao.Longitude;
            existente.Ativo = corporacao.Ativo;

            await _dataContext.SaveChangesAsync();
            return existente;
        }

        public async Task<bool> RemoverAsync(int id)
        {
            var existente = await _dataContext.CorporacoesBombeiro.FindAsync(id);
            if (existente is null)
                return false;

            _dataContext.CorporacoesBombeiro.Remove(existente);
            await _dataContext.SaveChangesAsync();
            return true;
        }
    }
}
