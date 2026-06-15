namespace ServicoMapa.Models
{
    public class CorporacaoBombeiro
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Endereco { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
