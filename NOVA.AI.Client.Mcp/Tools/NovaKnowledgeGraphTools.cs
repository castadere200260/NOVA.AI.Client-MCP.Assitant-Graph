using System.ComponentModel;
using NOVA.AI.Client.Mcp.Services;
using ModelContextProtocol.Server;

namespace NOVA.AI.Client.Mcp.Tools;

/// <summary>MCP tools for importing PDF documents into the Neo4j knowledge graph.</summary>
[McpServerToolType]
public sealed class NovaKnowledgeGraphTools(IPdfKnowledgeGraphService knowledgeGraphService)
{
    [McpServerTool(Name = "add_pdf_to_knowledge_graph")]
    [Description("Imports a PDF into Neo4j by storing every text chunk and extracting entities and relationships where possible. The indexed chunks are automatically searched before ask_ai, ask_ollama, and ask_chatgpt answer questions. Requires a fully qualified server-side PDF path and configured Neo4j credentials.")]
    public Task<string> AddPdfToKnowledgeGraphAsync(
        [Description("Absolute path to the PDF file on the machine running the MCP server. Scanned PDFs require OCR and are not supported by this tool yet.")] string pdfPath,
        [Description("Logical graph name used to group the imported entities.")] string graphName,
        [Description("AI provider to use for graph extraction: ChatGPT or Ollama. Defaults to Ollama.")] string provider = "Ollama",
        [Description("Optional model override for the selected provider.")] string? model = null,
        CancellationToken cancellationToken = default)
        => knowledgeGraphService.ImportPdfAsync(pdfPath, graphName, provider, model, cancellationToken);
}
