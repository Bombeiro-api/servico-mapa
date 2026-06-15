namespace ServicoMapa.Models
{
    public class OcorrenciaIncendio
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataOcorrencia { get; set; }

        public int CorporacaoAtendeuId { get; set; }
        public CorporacaoBombeiro? CorporacaoAtendeu { get; set; }

        public string DuracaoEstimada { get; set; } = string.Empty;
        public string DistanciaEstimada { get; set; } = string.Empty;
    }
}
