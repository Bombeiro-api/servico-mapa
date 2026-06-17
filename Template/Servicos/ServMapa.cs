using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Exemplo;
using Microsoft.EntityFrameworkCore;
using ServicoMapa.DTO;
using ServicoMapa.Models;

namespace ServicoMapa.Servicos
{
    public interface IServMapa
    {
        Task<RoteamentoResponseDTO> BuscarRotaMaisProxima(RoteamentoRequestDTO request);
    }

    public class ServMapa : IServMapa
    {
        private readonly HttpClient _googleMapsClient;
        private readonly HttpClient _veiculosClient;
        private readonly DataContext _dataContext;
        private readonly string _apiKey;
        private const string GoogleMapsBaseUrl = "https://maps.googleapis.com/maps/api";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ServMapa(IHttpClientFactory httpClientFactory, IConfiguration configuration, DataContext dataContext)
        {
            _googleMapsClient = httpClientFactory.CreateClient("googlemaps");
            _veiculosClient = httpClientFactory.CreateClient("veiculos");
            _dataContext = dataContext;
            _apiKey = configuration["GoogleMaps:ApiKey"]
                ?? throw new InvalidOperationException("GoogleMaps:ApiKey não configurada.");
        }

        public async Task<RoteamentoResponseDTO> BuscarRotaMaisProxima(RoteamentoRequestDTO request)
        {
            var corporacoes = await ObterCorporacoesDisponiveisAsync();

            if (corporacoes.Count == 0)
                throw new InvalidOperationException("Nenhuma corporação com viatura disponível.");

            var (corporacao, viatura) = await EncontrarMaisProximaAsync(request.LocalIncendio, corporacoes);

            await DespacharViaturaAsync(viatura.Id);

            var rota = await ObterDirecoesAsync(corporacao, request.LocalIncendio);
            rota.CorporacaoMaisProxima.Id = corporacao.Id;
            rota.ViaturaId = viatura.Id;

            await RegistrarOcorrenciaAsync(request.LocalIncendio, corporacao, viatura, rota);

            return rota;
        }

        private async Task<List<CorporacaoDTO>> ObterCorporacoesDisponiveisAsync()
        {
            var json = await _veiculosClient.GetStringAsync("/api/corporacao");
            var todas = JsonSerializer.Deserialize<List<CorporacaoDTO>>(json, JsonOptions) ?? [];

            return todas
                .Where(c => c.Ativo && c.Viaturas.Any(v => v.Status == "DisponivelNaBase"))
                .ToList();
        }

        private async Task<(CorporacaoDTO corporacao, ViaturaDTO viatura)> EncontrarMaisProximaAsync(
            LocalizacaoDTO destino,
            List<CorporacaoDTO> corporacoes)
        {
            var destinoStr = FormatarCoordenada(destino.Latitude, destino.Longitude);
            var origens = string.Join("|", corporacoes.Select(c => FormatarCoordenada(c.Latitude, c.Longitude)));

            var url = $"{GoogleMapsBaseUrl}/distancematrix/json"
                + $"?origins={HttpUtility.UrlEncode(origens)}"
                + $"&destinations={HttpUtility.UrlEncode(destinoStr)}"
                + $"&mode=driving"
                + $"&language=pt-BR"
                + $"&key={_apiKey}";

            var json = await _googleMapsClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<DistanceMatrixResponse>(json)
                ?? throw new Exception("Resposta inválida da API Distance Matrix.");

            if (response.Status != "OK")
                throw new Exception($"Erro na API Distance Matrix: {response.Status}");

            int indiceMaisProximo = -1;
            long menorDuracao = long.MaxValue;

            for (int i = 0; i < response.Rows.Count; i++)
            {
                var element = response.Rows[i].Elements.FirstOrDefault();
                if (element?.Status == "OK" && element.Duration.Value < menorDuracao)
                {
                    menorDuracao = element.Duration.Value;
                    indiceMaisProximo = i;
                }
            }

            if (indiceMaisProximo == -1)
                throw new Exception("Não foi possível calcular rota para nenhuma corporação.");

            var corporacao = corporacoes[indiceMaisProximo];
            var viatura = corporacao.Viaturas.First(v => v.Status == "DisponivelNaBase");

            return (corporacao, viatura);
        }

        private async Task DespacharViaturaAsync(int viaturaId)
        {
            var body = new StringContent("1", Encoding.UTF8, "application/json"); // 1 = EmDeslocamento
            var response = await _veiculosClient.PatchAsync($"/api/viatura/{viaturaId}/status", body);
            response.EnsureSuccessStatusCode();
        }

        private async Task<RoteamentoResponseDTO> ObterDirecoesAsync(CorporacaoDTO origem, LocalizacaoDTO destino)
        {
            var origemStr = FormatarCoordenada(origem.Latitude, origem.Longitude);
            var destinoStr = FormatarCoordenada(destino.Latitude, destino.Longitude);

            var url = $"{GoogleMapsBaseUrl}/directions/json"
                + $"?origin={HttpUtility.UrlEncode(origemStr)}"
                + $"&destination={HttpUtility.UrlEncode(destinoStr)}"
                + $"&mode=driving"
                + $"&language=pt-BR"
                + $"&key={_apiKey}";

            var json = await _googleMapsClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<DirectionsResponse>(json)
                ?? throw new Exception("Resposta inválida da API Directions.");

            if (response.Status != "OK")
                throw new Exception($"Erro na API Directions: {response.Status}");

            var rota = response.Routes.First();
            var trecho = rota.Legs.First();

            return new RoteamentoResponseDTO
            {
                CorporacaoMaisProxima = new CorporacaoBombeiroDTO
                {
                    Nome = origem.Nome,
                    Latitude = origem.Latitude,
                    Longitude = origem.Longitude,
                },
                DuracaoEstimada = trecho.Duration.Text,
                DistanciaEstimada = trecho.Distance.Text,
                Passos = trecho.Steps.Select(s => new PassoRotaDTO
                {
                    Instrucao = RemoverTagsHtml(s.HtmlInstructions),
                    Distancia = s.Distance.Text,
                    Duracao = s.Duration.Text,
                }).ToList(),
                PolylineEncoded = rota.OverviewPolyline.Points,
            };
        }

        private async Task RegistrarOcorrenciaAsync(
            LocalizacaoDTO local,
            CorporacaoDTO corporacao,
            ViaturaDTO viatura,
            RoteamentoResponseDTO rota)
        {
            _dataContext.OcorrenciasIncendio.Add(new OcorrenciaIncendio
            {
                Latitude = local.Latitude,
                Longitude = local.Longitude,
                DataOcorrencia = DateTime.UtcNow,
                CorporacaoId = corporacao.Id,
                ViaturaId = viatura.Id,
                DuracaoEstimada = rota.DuracaoEstimada,
                DistanciaEstimada = rota.DistanciaEstimada,
            });

            await _dataContext.SaveChangesAsync();
        }

        private static string FormatarCoordenada(double lat, double lng) =>
            $"{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        private static string RemoverTagsHtml(string html) =>
            System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);

        // ── Google Maps API response models ──────────────────────────────────

        private class DistanceMatrixResponse
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("rows")]
            public List<MatrixRow> Rows { get; set; } = new();
        }

        private class MatrixRow
        {
            [JsonPropertyName("elements")]
            public List<MatrixElement> Elements { get; set; } = new();
        }

        private class MatrixElement
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("distance")]
            public TextValue Distance { get; set; } = new();

            [JsonPropertyName("duration")]
            public TextValue Duration { get; set; } = new();
        }

        private class DirectionsResponse
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("routes")]
            public List<DirectionsRoute> Routes { get; set; } = new();
        }

        private class DirectionsRoute
        {
            [JsonPropertyName("legs")]
            public List<DirectionsLeg> Legs { get; set; } = new();

            [JsonPropertyName("overview_polyline")]
            public Polyline OverviewPolyline { get; set; } = new();
        }

        private class DirectionsLeg
        {
            [JsonPropertyName("distance")]
            public TextValue Distance { get; set; } = new();

            [JsonPropertyName("duration")]
            public TextValue Duration { get; set; } = new();

            [JsonPropertyName("steps")]
            public List<DirectionsStep> Steps { get; set; } = new();
        }

        private class DirectionsStep
        {
            [JsonPropertyName("html_instructions")]
            public string HtmlInstructions { get; set; } = string.Empty;

            [JsonPropertyName("distance")]
            public TextValue Distance { get; set; } = new();

            [JsonPropertyName("duration")]
            public TextValue Duration { get; set; } = new();
        }

        private class TextValue
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;

            [JsonPropertyName("value")]
            public long Value { get; set; }
        }

        private class Polyline
        {
            [JsonPropertyName("points")]
            public string Points { get; set; } = string.Empty;
        }
    }
}
