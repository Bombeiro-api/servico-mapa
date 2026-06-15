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
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly DataContext _dataContext;
        private const string BaseUrl = "https://maps.googleapis.com/maps/api";

        public ServMapa(
            HttpClient httpClient,
            IConfiguration configuration,
            DataContext dataContext
        )
        {
            _httpClient = httpClient;
            _dataContext = dataContext;
            _apiKey =
                configuration["GoogleMaps:ApiKey"]
                ?? throw new InvalidOperationException("GoogleMaps:ApiKey não configurada.");
        }

        public async Task<RoteamentoResponseDTO> BuscarRotaMaisProxima(RoteamentoRequestDTO request)
        {
            var corporacoes = await _dataContext
                .CorporacoesBombeiro.Where(c => c.Ativo)
                .ToListAsync();

            if (corporacoes.Count == 0)
                throw new InvalidOperationException("Nenhuma corporação de bombeiro cadastrada.");

            var corporacaoMaisProxima = await EncontrarMaisProxima(
                request.LocalIncendio,
                corporacoes
            );
            return await ObterDirecoes(corporacaoMaisProxima, request.LocalIncendio);
        }

        private async Task<CorporacaoBombeiro> EncontrarMaisProxima(
            LocalizacaoDTO destino,
            List<CorporacaoBombeiro> corporacoes
        )
        {
            var destinoStr = FormatarCoordenada(destino.Latitude, destino.Longitude);
            var origens = string.Join(
                "|",
                corporacoes.Select(c => FormatarCoordenada(c.Latitude, c.Longitude))
            );

            var url =
                $"{BaseUrl}/distancematrix/json"
                + $"?origins={HttpUtility.UrlEncode(origens)}"
                + $"&destinations={HttpUtility.UrlEncode(destinoStr)}"
                + $"&mode=driving"
                + $"&language=pt-BR"
                + $"&key={_apiKey}";

            var json = await _httpClient.GetStringAsync(url);
            var response =
                JsonSerializer.Deserialize<DistanceMatrixResponse>(json)
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

            return corporacoes[indiceMaisProximo];
        }

        private async Task<RoteamentoResponseDTO> ObterDirecoes(
            CorporacaoBombeiro origem,
            LocalizacaoDTO destino
        )
        {
            var origemStr = FormatarCoordenada(origem.Latitude, origem.Longitude);
            var destinoStr = FormatarCoordenada(destino.Latitude, destino.Longitude);

            var url =
                $"{BaseUrl}/directions/json"
                + $"?origin={HttpUtility.UrlEncode(origemStr)}"
                + $"&destination={HttpUtility.UrlEncode(destinoStr)}"
                + $"&mode=driving"
                + $"&language=pt-BR"
                + $"&key={_apiKey}";

            var json = await _httpClient.GetStringAsync(url);
            var response =
                JsonSerializer.Deserialize<DirectionsResponse>(json)
                ?? throw new Exception("Resposta inválida da API Directions.");

            if (response.Status != "OK")
                throw new Exception($"Erro na API Directions: {response.Status}");

            var rota = response.Routes.First();
            var trecho = rota.Legs.First();

            var passos = trecho
                .Steps.Select(s => new PassoRotaDTO
                {
                    Instrucao = RemoverTagsHtml(s.HtmlInstructions),
                    Distancia = s.Distance.Text,
                    Duracao = s.Duration.Text,
                })
                .ToList();

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
                Passos = passos,
                PolylineEncoded = rota.OverviewPolyline.Points,
            };
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
