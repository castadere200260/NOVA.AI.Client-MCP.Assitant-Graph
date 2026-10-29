using System.Security.Cryptography;
using System.Text;
using NOVA.AI.Client.Infrastructure;
using NOVA.AI.Client.Mcp.Services;
using NOVA.AI.Client.Mcp.Tools;
using Y10.AI.Client;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNovaAiClientInfrastructure(builder.Configuration);
builder.Services.AddY10AiClient(builder.Configuration);
builder.Services.Configure<KnowledgeGraphOptions>(builder.Configuration.GetSection(KnowledgeGraphOptions.SectionName));
builder.Services.AddSingleton<IPdfKnowledgeGraphService, PdfKnowledgeGraphService>();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<NovaLoggingTools>()
    .WithTools<NovaAiTools>()
    .WithTools<NovaKnowledgeGraphTools>();

var mcpUrl = builder.Configuration["Mcp:Url"] ?? "http://127.0.0.1:5280";
builder.WebHost.UseUrls(mcpUrl);
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    var configuredApiKey = app.Configuration["Mcp:ApiKey"];
    if (string.IsNullOrWhiteSpace(configuredApiKey))
        throw new InvalidOperationException("Configure Mcp:ApiKey using a secret/environment variable before running outside Development.");
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase)) { await next(context); return; }
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var supplied) || !FixedTimeEquals(supplied.ToString(), configuredApiKey))
        { context.Response.StatusCode = StatusCodes.Status401Unauthorized; await context.Response.WriteAsync("Unauthorized."); return; }
        await next(context);
    });
}
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "NOVA.AI.Client.Mcp" }));
app.MapMcp("/mcp");
app.Run();

static bool FixedTimeEquals(string supplied, string expected)
{
    var a = Encoding.UTF8.GetBytes(supplied); var b = Encoding.UTF8.GetBytes(expected);
    return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
}
public partial class Program { }
