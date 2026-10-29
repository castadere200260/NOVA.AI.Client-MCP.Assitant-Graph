namespace NOVA.AI.Client.Mcp.Services;

public interface IPdfKnowledgeGraphService
{
    Task<string> ImportPdfAsync(string pdfPath, string graphName, string? provider, string? model, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> SearchAsync(string question, int limit, CancellationToken cancellationToken);
}

public sealed record KnowledgeChunk(string Document, int ChunkNumber, string Content, IReadOnlyList<string> RelatedEntities);
