using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Collections;
using System.Reflection;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole();

var endpoint = builder.Configuration["Mcp:Endpoint"] ?? "http://localhost:5280/mcp";
var systemPrompt = builder.Configuration["AiTests:SystemPrompt"]
    ?? "You are a .NET technical assistant. Answer clearly and concisely.";
var defaultModel = builder.Configuration["AiTests:Model"] ?? "";
using var host = builder.Build();

Console.WriteLine("====================================================");
Console.WriteLine(" NOVA.AI.Client.Mcp.Test - MCP integration console");
Console.WriteLine("====================================================");
Console.WriteLine($"MCP endpoint: {endpoint}");
Console.WriteLine("This client replaces MCP Inspector for manual testing.");
Console.WriteLine("The NOVA.AI.Client.Mcp server must be started separately.\n");

try
{
    var transport = new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri(endpoint)
    });

    await using var client = await McpClient.CreateAsync(transport);
    var tools = await client.ListToolsAsync();
    var availableTools = tools.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

    Console.WriteLine("Discovered MCP tools:");
    foreach (var tool in tools)
        Console.WriteLine($" - {tool.Name}: {tool.Description}");

    while (true)
    {
        Console.WriteLine("\n-------------------- TEST MENU --------------------");
        Console.WriteLine("1 - Test ChatGPT / OpenAI (ask_chatgpt)");
        Console.WriteLine("2 - Test Ollama (ask_ollama)");
        Console.WriteLine("3 - Test the default provider (ask_ai)");
        Console.WriteLine("4 - Test ask_ai with ChatGPT");
        Console.WriteLine("5 - Test ask_ai with Ollama");
        Console.WriteLine("6 - Send an Information log");
        Console.WriteLine("7 - Send a Warning log");
        Console.WriteLine("8 - Send an Error log");
        Console.WriteLine("9 - Run the logging diagnostic");
        Console.WriteLine("10 - Add PDF to Knowledge Graph");
        Console.WriteLine("L - List available tools again");
        Console.WriteLine("0 - Exit");
        Console.Write("> ");

        var choice = Console.ReadLine()?.Trim();
        if (string.Equals(choice, "0", StringComparison.OrdinalIgnoreCase))
            break;

        try
        {
            switch (choice?.ToUpperInvariant())
            {
                case "1":
                    await AskAiAsync(client, availableTools, "ask_chatgpt", null, defaultModel, systemPrompt);
                    break;
                case "2":
                    await AskAiAsync(client, availableTools, "ask_ollama", null, defaultModel, systemPrompt);
                    break;
                case "3":
                    await AskAiAsync(client, availableTools, "ask_ai", null, defaultModel, systemPrompt);
                    break;
                case "4":
                    await AskAiAsync(client, availableTools, "ask_ai", "ChatGPT", defaultModel, systemPrompt);
                    break;
                case "5":
                    await AskAiAsync(client, availableTools, "ask_ai", "Ollama", defaultModel, systemPrompt);
                    break;
                case "6":
                    await SubmitLogAsync(client, availableTools, "Information",
                        "Test information log submitted by NOVA.AI.Client.Mcp.Test",
                        new Dictionary<string, string>
                        {
                            ["environment"] = "Development",
                            ["source"] = "NOVA.AI.Client.Mcp.Test",
                            ["eventType"] = "InformationTest",
                            ["status"] = "Success"
                        });
                    break;
                case "7":
                    await SubmitLogAsync(client, availableTools, "Warning",
                        "Test warning: external service response time exceeded the expected threshold.",
                        new Dictionary<string, string>
                        {
                            ["environment"] = "Development",
                            ["source"] = "NOVA.AI.Client.Mcp.Test",
                            ["eventType"] = "SlowExternalService",
                            ["responseTimeMs"] = "3500",
                            ["thresholdMs"] = "2000"
                        });
                    break;
                case "8":
                    await SubmitLogAsync(client, availableTools, "Error",
                        "Test error: customer information could not be retrieved from the external service.",
                        new Dictionary<string, string>
                        {
                            ["environment"] = "Development",
                            ["source"] = "NOVA.AI.Client.Mcp.Test",
                            ["eventType"] = "CustomerRetrievalFailure",
                            ["errorCode"] = "CUSTOMER_SERVICE_UNAVAILABLE",
                            ["httpStatusCode"] = "503"
                        });
                    break;
                case "9":
                    await CallNoArgumentToolAsync(client, availableTools, "write_logging_diagnostic");
                    break;
                case "L":
                    foreach (var tool in await client.ListToolsAsync())
                        Console.WriteLine($" - {tool.Name}: {tool.Description}");
                    break;

                case "10":
                    await AddPdfToKnowledgeGraphAsync(client, availableTools);
                    break;
                default:
                    Console.WriteLine("Invalid selection.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\nTEST FAILED: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("Unable to connect to the MCP server.");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Make sure NOVA.AI.Client.Mcp is running and Mcp:Endpoint is correct.");
    Environment.ExitCode = 1;
}

static async Task AskAiAsync(
    McpClient client,
    HashSet<string> availableTools,
    string toolName,
    string? provider,
    string defaultModel,
    string systemPrompt)
{
    EnsureToolExists(availableTools, toolName);

    Console.Write("Question to send to the AI: ");
    var message = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(message))
    {
        Console.WriteLine("The question cannot be empty.");
        return;
    }

    Console.Write($"System prompt [{systemPrompt}] : ");
    var customPrompt = Console.ReadLine();
    var prompt = string.IsNullOrWhiteSpace(customPrompt) ? systemPrompt : customPrompt;

    Console.Write($"Model [{defaultModel} ; leave empty to use the server configuration] : ");
    var model = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(model)) model = defaultModel;

    var args = new Dictionary<string, object?>
    {
        ["message"] = message,
        ["systemPrompt"] = prompt
    };
    if (!string.IsNullOrWhiteSpace(model)) args["model"] = model;
    if (!string.IsNullOrWhiteSpace(provider)) args["provider"] = provider;

    Console.WriteLine($"\nCalling tool {toolName}{(provider is null ? "" : $" (provider={provider})")}...");
    var result = await client.CallToolAsync(toolName, args);
    PrintResult(toolName, result);
}

static async Task SubmitLogAsync(
    McpClient client,
    HashSet<string> availableTools,
    string level,
    string message,
    Dictionary<string, string> properties)
{
    EnsureToolExists(availableTools, "submit_log");
    var request = new
    {
        level,
        message,
        serviceName = "NOVA.AI.Client.Mcp.Test",
        timestamp = DateTimeOffset.UtcNow,
        properties
    };

    var args = new Dictionary<string, object?>
    {
        ["request"] = JsonSerializer.SerializeToElement(request)
    };
    var result = await client.CallToolAsync("submit_log", args);
    PrintResult("submit_log", result);
}

static async Task CallNoArgumentToolAsync(McpClient client, HashSet<string> availableTools, string toolName)
{
    EnsureToolExists(availableTools, toolName);
    var result = await client.CallToolAsync(toolName, new Dictionary<string, object?>());
    PrintResult(toolName, result);
}

static void EnsureToolExists(HashSet<string> availableTools, string toolName)
{
    if (!availableTools.Contains(toolName))
        throw new InvalidOperationException(
            $"The MCP tool '{toolName}' is not exposed by the current server. " +
            "Make sure you started a version of NOVA.AI.Client.Mcp that exposes this tool.");
}

static void PrintResult(string toolName, object result)
{
    Console.WriteLine($"\n================ RESULT: {toolName} ================");

    // CallToolResult exposes content blocks. Reflection helps keep this output compatible
    // across SDK versions and prints the actual text instead of only the object's type name.
    var contentProperty = result.GetType().GetProperty("Content", BindingFlags.Public | BindingFlags.Instance);
    if (contentProperty?.GetValue(result) is IEnumerable contentBlocks)
    {
        var printed = false;
        foreach (var block in contentBlocks)
        {
            if (block is null) continue;
            var textProperty = block.GetType().GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
            var text = textProperty?.GetValue(block)?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine(text);
                printed = true;
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(block, new JsonSerializerOptions { WriteIndented = true }));
                printed = true;
            }
        }
        if (!printed) Console.WriteLine("The tool returned no text content.");
    }
    else
    {
        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }

    var isError = result.GetType().GetProperty("IsError")?.GetValue(result);
    if (isError is true) Console.WriteLine("\nWARNING: The MCP server reported an error for this call.");
}


//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using ModelContextProtocol.Client;
//using System.Collections;
//using System.Reflection;
//using System.Text.Json;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Logging.AddConsole();

//var endpoint = builder.Configuration["Mcp:Endpoint"] ?? "http://localhost:5280/mcp";
//var systemPrompt = builder.Configuration["AiTests:SystemPrompt"]
//    ?? "Tu es un assistant technique .NET. Réponds clairement et de façon concise.";
//var defaultModel = builder.Configuration["AiTests:Model"] ?? "";
//using var host = builder.Build();

//Console.WriteLine("====================================================");
//Console.WriteLine(" NOVA.AI.Client.Mcp.Test - Console d'intégration MCP");
//Console.WriteLine("====================================================");
//Console.WriteLine($"Endpoint MCP : {endpoint}");
//Console.WriteLine("Ce client remplace MCP Inspector pour les tests manuels.");
//Console.WriteLine("Le serveur NOVA.AI.Client.Mcp doit être démarré séparément.\n");

//try
//{
//    var transport = new HttpClientTransport(new HttpClientTransportOptions
//    {
//        Endpoint = new Uri(endpoint)
//    });

//    await using var client = await McpClient.CreateAsync(transport);
//    var tools = await client.ListToolsAsync();
//    var availableTools = tools.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

//    Console.WriteLine("Outils MCP détectés :");
//    foreach (var tool in tools)
//        Console.WriteLine($" - {tool.Name}: {tool.Description}");

//    while (true)
//    {
//        Console.WriteLine("\n-------------------- MENU DE TEST --------------------");
//        Console.WriteLine("1 - Tester ChatGPT / OpenAI (ask_chatgpt)");
//        Console.WriteLine("2 - Tester Ollama (ask_ollama)");
//        Console.WriteLine("3 - Tester le provider par défaut (ask_ai)");
//        Console.WriteLine("4 - Tester ask_ai avec ChatGPT");
//        Console.WriteLine("5 - Tester ask_ai avec Ollama");
//        Console.WriteLine("6 - Envoyer un log Information");
//        Console.WriteLine("7 - Envoyer un log Warning");
//        Console.WriteLine("8 - Envoyer un log Error");
//        Console.WriteLine("9 - Exécuter le diagnostic de logging");
//        Console.WriteLine("L - Relister les outils disponibles");
//        Console.WriteLine("0 - Quitter");
//        Console.Write("> ");

//        var choice = Console.ReadLine()?.Trim();
//        if (string.Equals(choice, "0", StringComparison.OrdinalIgnoreCase))
//            break;

//        try
//        {
//            switch (choice?.ToUpperInvariant())
//            {
//                case "1":
//                    await AskAiAsync(client, availableTools, "ask_chatgpt", null, defaultModel, systemPrompt);
//                    break;
//                case "2":
//                    await AskAiAsync(client, availableTools, "ask_ollama", null, defaultModel, systemPrompt);
//                    break;
//                case "3":
//                    await AskAiAsync(client, availableTools, "ask_ai", null, defaultModel, systemPrompt);
//                    break;
//                case "4":
//                    await AskAiAsync(client, availableTools, "ask_ai", "ChatGPT", defaultModel, systemPrompt);
//                    break;
//                case "5":
//                    await AskAiAsync(client, availableTools, "ask_ai", "Ollama", defaultModel, systemPrompt);
//                    break;
//                case "6":
//                    await SubmitLogAsync(client, availableTools, "Information",
//                        "Test information log submitted by NOVA.AI.Client.Mcp.Test",
//                        new Dictionary<string, string>
//                        {
//                            ["environment"] = "Development",
//                            ["source"] = "NOVA.AI.Client.Mcp.Test",
//                            ["eventType"] = "InformationTest",
//                            ["status"] = "Success"
//                        });
//                    break;
//                case "7":
//                    await SubmitLogAsync(client, availableTools, "Warning",
//                        "Test warning: external service response time exceeded the expected threshold.",
//                        new Dictionary<string, string>
//                        {
//                            ["environment"] = "Development",
//                            ["source"] = "NOVA.AI.Client.Mcp.Test",
//                            ["eventType"] = "SlowExternalService",
//                            ["responseTimeMs"] = "3500",
//                            ["thresholdMs"] = "2000"
//                        });
//                    break;
//                case "8":
//                    await SubmitLogAsync(client, availableTools, "Error",
//                        "Test error: customer information could not be retrieved from the external service.",
//                        new Dictionary<string, string>
//                        {
//                            ["environment"] = "Development",
//                            ["source"] = "NOVA.AI.Client.Mcp.Test",
//                            ["eventType"] = "CustomerRetrievalFailure",
//                            ["errorCode"] = "CUSTOMER_SERVICE_UNAVAILABLE",
//                            ["httpStatusCode"] = "503"
//                        });
//                    break;
//                case "9":
//                    await CallNoArgumentToolAsync(client, availableTools, "write_logging_diagnostic");
//                    break;
//                case "L":
//                    foreach (var tool in await client.ListToolsAsync())
//                        Console.WriteLine($" - {tool.Name}: {tool.Description}");
//                    break;
//                default:
//                    Console.WriteLine("Choix invalide.");
//                    break;
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.Error.WriteLine($"\nÉCHEC DU TEST : {ex.Message}");
//        }
//    }
//}
//catch (Exception ex)
//{
//    Console.Error.WriteLine("Impossible de se connecter au serveur MCP.");
//    Console.Error.WriteLine(ex.Message);
//    Console.Error.WriteLine("Vérifiez que NOVA.AI.Client.Mcp fonctionne et que Mcp:Endpoint est correct.");
//    Environment.ExitCode = 1;
//}

//static async Task AskAiAsync(
//    McpClient client,
//    HashSet<string> availableTools,
//    string toolName,
//    string? provider,
//    string defaultModel,
//    string systemPrompt)
//{
//    EnsureToolExists(availableTools, toolName);

//    Console.Write("Question à envoyer à l'IA : ");
//    var message = Console.ReadLine();
//    if (string.IsNullOrWhiteSpace(message))
//    {
//        Console.WriteLine("La question ne peut pas être vide.");
//        return;
//    }

//    Console.Write($"System prompt [{systemPrompt}] : ");
//    var customPrompt = Console.ReadLine();
//    var prompt = string.IsNullOrWhiteSpace(customPrompt) ? systemPrompt : customPrompt;

//    Console.Write($"Modèle [{defaultModel} ; laisser vide pour la configuration serveur] : ");
//    var model = Console.ReadLine();
//    if (string.IsNullOrWhiteSpace(model)) model = defaultModel;

//    var args = new Dictionary<string, object?>
//    {
//        ["message"] = message,
//        ["systemPrompt"] = prompt
//    };
//    if (!string.IsNullOrWhiteSpace(model)) args["model"] = model;
//    if (!string.IsNullOrWhiteSpace(provider)) args["provider"] = provider;

//    Console.WriteLine($"\nAppel de l'outil {toolName}{(provider is null ? "" : $" (provider={provider})")}...");
//    var result = await client.CallToolAsync(toolName, args);
//    PrintResult(toolName, result);
//}

//static async Task SubmitLogAsync(
//    McpClient client,
//    HashSet<string> availableTools,
//    string level,
//    string message,
//    Dictionary<string, string> properties)
//{
//    EnsureToolExists(availableTools, "submit_log");
//    var request = new
//    {
//        level,
//        message,
//        serviceName = "NOVA.AI.Client.Mcp.Test",
//        timestamp = DateTimeOffset.UtcNow,
//        properties
//    };

//    var args = new Dictionary<string, object?>
//    {
//        ["request"] = JsonSerializer.SerializeToElement(request)
//    };
//    var result = await client.CallToolAsync("submit_log", args);
//    PrintResult("submit_log", result);
//}

//static async Task CallNoArgumentToolAsync(McpClient client, HashSet<string> availableTools, string toolName)
//{
//    EnsureToolExists(availableTools, toolName);
//    var result = await client.CallToolAsync(toolName, new Dictionary<string, object?>());
//    PrintResult(toolName, result);
//}

//static void EnsureToolExists(HashSet<string> availableTools, string toolName)
//{
//    if (!availableTools.Contains(toolName))
//        throw new InvalidOperationException(
//            $"L'outil MCP '{toolName}' n'est pas exposé par le serveur actuel. " +
//            "Vérifiez que vous avez démarré la version de NOVA.AI.Client.Mcp qui contient cet outil.");
//}

//static void PrintResult(string toolName, object result)
//{
//    Console.WriteLine($"\n================ RÉSULTAT : {toolName} ================");

//    // CallToolResult exposes Content blocks. Reflection keeps output compatible with
//    // SDK versions while printing actual text instead of only the object's type name.
//    var contentProperty = result.GetType().GetProperty("Content", BindingFlags.Public | BindingFlags.Instance);
//    if (contentProperty?.GetValue(result) is IEnumerable contentBlocks)
//    {
//        var printed = false;
//        foreach (var block in contentBlocks)
//        {
//            if (block is null) continue;
//            var textProperty = block.GetType().GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
//            var text = textProperty?.GetValue(block)?.ToString();
//            if (!string.IsNullOrWhiteSpace(text))
//            {
//                Console.WriteLine(text);
//                printed = true;
//            }
//            else
//            {
//                Console.WriteLine(JsonSerializer.Serialize(block, new JsonSerializerOptions { WriteIndented = true }));
//                printed = true;
//            }
//        }
//        if (!printed) Console.WriteLine("L'outil n'a retourné aucun contenu textuel.");
//    }
//    else
//    {
//        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
//    }

//    var isError = result.GetType().GetProperty("IsError")?.GetValue(result);
//    if (isError is true) Console.WriteLine("\nATTENTION : le serveur MCP signale une erreur pour cet appel.");
//}


static async Task AddPdfToKnowledgeGraphAsync(McpClient client, HashSet<string> availableTools)
{
    const string toolName = "add_pdf_to_knowledge_graph";
    if (!availableTools.Contains(toolName))
    {
        Console.WriteLine($"The MCP server does not expose the tool '{toolName}'.");
        return;
    }

    Console.Write("Absolute PDF path on the MCP server: ");
    var pdfPath = Console.ReadLine()?.Trim().Trim('"');
    if (string.IsNullOrWhiteSpace(pdfPath))
    {
        Console.WriteLine("PDF path is required.");
        return;
    }

    Console.Write("Knowledge graph name [default: pdf-knowledge-graph]: ");
    var graphName = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(graphName))
        graphName = "pdf-knowledge-graph";

    Console.Write("AI provider (Ollama/ChatGPT) [default: Ollama]: ");
    var provider = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(provider))
        provider = "Ollama";

    if (!provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) &&
        !provider.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase) &&
        !provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Unsupported provider. Use Ollama or ChatGPT.");
        return;
    }

    Console.Write("Model (leave empty to use server configuration): ");
    var model = Console.ReadLine()?.Trim();

    var arguments = new Dictionary<string, object?>
    {
        ["pdfPath"] = pdfPath,
        ["graphName"] = graphName,
        ["provider"] = provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) ? "ChatGPT" : provider
    };
    if (!string.IsNullOrWhiteSpace(model))
        arguments["model"] = model;

    try
    {
        Console.WriteLine("Calling add_pdf_to_knowledge_graph...");
        var result = await client.CallToolAsync(toolName, arguments);
        PrintResult(toolName, result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"PDF import failed: {ex.Message}");
    }
}
