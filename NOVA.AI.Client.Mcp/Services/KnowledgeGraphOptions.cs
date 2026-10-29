namespace NOVA.AI.Client.Mcp.Services;

/// <summary>Configuration for PDF ingestion and Neo4j persistence.</summary>
public sealed class KnowledgeGraphOptions
{
    public const string SectionName = "KnowledgeGraph";

    public string Neo4jUri { get; set; } = "bolt://localhost:7687";
    public string Username { get; set; } = "neo4j";
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = "neo4j";
    public int MaxPdfCharacters { get; set; } = 80_000;
    public int MaxChunkCharacters { get; set; } = 8_000;
}
