using Exemplo;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ServicoMapa.Servicos;
using Template.Infra;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Serviço de Mapa", Version = "v1" });
});

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddHttpClient("googlemaps");
builder.Services.AddHttpClient("veiculos", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServicoVeiculos:BaseUrl"]
        ?? throw new InvalidOperationException("ServicoVeiculos:BaseUrl não configurada."));
});

builder.Services.AddScoped<IServMapa, ServMapa>();

GeradorDeServicos.ServiceProvider = builder.Services.BuildServiceProvider();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
