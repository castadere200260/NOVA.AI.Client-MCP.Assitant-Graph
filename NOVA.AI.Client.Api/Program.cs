using NOVA.AI.Client.Adapter.Api.Controllers;
using NOVA.AI.Client.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(HealthController).Assembly);
builder.Services.AddNovaAiClientInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapControllers();
app.Run();

public partial class Program { }
