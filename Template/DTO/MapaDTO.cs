namespace ServicoMapa.DTO
{
    public class LocalizacaoDTO
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CorporacaoBombeiroDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class RoteamentoRequestDTO
    {
        public LocalizacaoDTO LocalIncendio { get; set; } = new();
    }

    public class PassoRotaDTO
    {
        public string Instrucao { get; set; } = string.Empty;
        public string Distancia { get; set; } = string.Empty;
        public string Duracao { get; set; } = string.Empty;
    }

    public class RoteamentoResponseDTO
    {
        public CorporacaoBombeiroDTO CorporacaoMaisProxima { get; set; } = new();
        public int ViaturaId { get; set; }
        public string DuracaoEstimada { get; set; } = string.Empty;
        public string DistanciaEstimada { get; set; } = string.Empty;
        public List<PassoRotaDTO> Passos { get; set; } = new();
        public string PolylineEncoded { get; set; } = string.Empty;
    }
}
