using System.Text.Json.Serialization;

namespace ServicoMapa.DTO
{
    public class CorporacaoDTO
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("endereco")]
        public string Endereco { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; }

        [JsonPropertyName("viaturas")]
        public List<ViaturaDTO> Viaturas { get; set; } = new();
    }

    public class ViaturaDTO
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; } // 0 = DisponivelNaBase, 1 = EmDeslocamento, 2 = NoLocalDaOcorrencia, 3 = EmManutencao

        [JsonPropertyName("corporacaoId")]
        public int CorporacaoId { get; set; }
    }
}
