# Serviço de Mapa

## Sobre o Projeto

O Serviço de Mapa é responsável por calcular a rota mais próxima entre as corporações de bombeiros disponíveis e o local de um incêndio. Ele atua como intermediário entre o serviço de ocorrências, o serviço de veículos e a API do Google Maps.

## Tecnologias Utilizadas

* .NET 8
* ASP.NET Core Web API
* Entity Framework Core
* SQLite
* Google Maps Distance Matrix API
* Google Maps Directions API
* Scalar
* REST API

## Fluxo de Funcionamento

Ao receber uma requisição com as coordenadas do incêndio, o serviço executa os seguintes passos:

```
POST /api/mapa/rota-mais-proxima
  ├─► servico-veiculos GET /api/corporacao
  │       filtra corporações ativas com ao menos uma viatura DisponivelNaBase
  ├─► Google Maps Distance Matrix API
  │       calcula o tempo de deslocamento de cada corporação até o local
  │       seleciona a de menor duração
  ├─► servico-veiculos PATCH /api/viatura/{id}/status
  │       marca a viatura como EmDeslocamento
  ├─► Google Maps Directions API
  │       obtém a rota completa com passos e polyline
  ├─► salva OcorrenciaIncendio no banco local
  └─► retorna resultado para o serviço de ocorrências
```

## Endpoints

### Buscar rota mais próxima

```http
POST /api/mapa/rota-mais-proxima
```

**Body:**
```json
{
  "localIncendio": {
    "latitude": -28.7283,
    "longitude": -49.3015
  }
}
```

**Resposta:**
```json
{
  "corporacaoMaisProxima": {
    "id": 1,
    "nome": "1º Batalhão de Bombeiros Militar",
    "latitude": -28.7283,
    "longitude": -49.3015
  },
  "viaturaId": 1,
  "duracaoEstimada": "12 min",
  "distanciaEstimada": "5,2 km",
  "passos": [
    {
      "instrucao": "Siga em frente na Rua X",
      "distancia": "200 m",
      "duracao": "1 min"
    }
  ],
  "polylineEncoded": "..."
}
```

## Estrutura da Entidade Local

### OcorrenciaIncendio

Registra um histórico de cada despacho realizado.

| Campo | Tipo | Descrição |
|---|---|---|
| Id | Inteiro | Identificador único |
| Latitude | Double | Coordenada do incêndio |
| Longitude | Double | Coordenada do incêndio |
| Descricao | Texto | Descrição opcional |
| DataOcorrencia | DateTime | Momento do despacho |
| CorporacaoId | Inteiro | ID da corporação despachada |
| ViaturaId | Inteiro | ID da viatura despachada |
| DuracaoEstimada | Texto | Tempo estimado retornado pelo Google Maps |
| DistanciaEstimada | Texto | Distância estimada retornada pelo Google Maps |

## Configuração

### Variáveis necessárias

Configure via user secrets para não expor dados sensíveis no repositório:

```powershell
dotnet user-secrets set "GoogleMaps:ApiKey" "SUA_CHAVE_AQUI"
dotnet user-secrets set "ServicoVeiculos:BaseUrl" "http://localhost:5091"
```

### Google Maps — APIs necessárias

Habilitar no Google Cloud Console:
* Distance Matrix API
* Directions API

## Arquitetura

* **Controllers** — exposição dos endpoints da API
* **Servicos** — lógica de roteamento e integração com Google Maps e servico-veiculos
* **DTO** — contratos com serviços externos (MapaDTO, VeiculosDTO)
* **Models** — entidade local OcorrenciaIncendio
* **DataContext** — comunicação com o banco de dados
* **Migrations** — controle de versão da estrutura do banco

## Portas

| Serviço | Porta |
|---|---|
| ServicoMapa | 5089 |
| servico-veiculos | 5091 |
| servico-ocorrencia | 5090 |
