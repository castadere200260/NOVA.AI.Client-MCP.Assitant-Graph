# NOVA.AI.Client.Mcp + Y10.AI.Client

## Fonctionnement

`NOVA.AI.Client.Mcp` expose des outils MCP qui appellent le nouveau projet `Y10.AI.Client`.
Le projet client contient une abstraction commune et deux providers HTTP :

- **ChatGPT / OpenAI API** : `POST /v1/chat/completions`
- **Ollama** : `POST /api/chat`

Les outils de logging existants (`submit_log`, `write_logging_diagnostic`) sont conservés.

## Outils MCP exposés

| Outil | Rôle |
|---|---|
| `ask_chatgpt` | Interroge ChatGPT via l'API OpenAI |
| `ask_ollama` | Interroge un modèle hébergé par Ollama |
| `ask_ai` | Interroge le provider configuré par défaut ou celui passé en paramètre |
| `submit_log` | Soumet un log Y10 existant |
| `write_logging_diagnostic` | Déclenche le diagnostic de logging existant |

Les outils d'IA acceptent `message`, un `systemPrompt` facultatif et un `model` facultatif. `ask_ai` accepte aussi `provider` (`ChatGPT` ou `Ollama`).

## Configuration

Le fichier `NOVA.AI.Client.Mcp/appsettings.json` contient la configuration non secrète :

```json
"AiClient": {
  "DefaultProvider": "Ollama",
  "OpenAI": {
    "BaseUrl": "https://api.openai.com/v1",
    "ApiKey": "",
    "Model": "gpt-4o-mini",
    "TimeoutSeconds": 60
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.2",
    "TimeoutSeconds": 120
  }
}
```

Ne mettez pas la clé OpenAI dans le fichier de configuration versionné. Sous PowerShell, configurez-la dans l'environnement du processus :

```powershell
$env:AiClient__OpenAI__ApiKey = "<votre-cle-api-openai>"
```

Pour utiliser un autre modèle ou une autre URL :

```powershell
$env:AiClient__DefaultProvider = "Ollama"
$env:AiClient__Ollama__BaseUrl = "http://localhost:11434"
$env:AiClient__Ollama__Model = "llama3.2"
```

Si le MCP tourne dans Docker, `localhost` désigne le conteneur MCP lui-même. Dans ce cas, utilisez par exemple `http://host.docker.internal:11434` pour Ollama hébergé sur la machine Windows, ou le nom du service Docker Compose.

## Démarrage Ollama

Ollama doit être installé et démarré. Puis téléchargez le modèle configuré :

```powershell
ollama pull llama3.2
ollama list
```

Vérifiez l'API locale :

```powershell
Invoke-RestMethod http://localhost:11434/api/tags
```

## Compiler et démarrer

Depuis le dossier racine extrait de l'archive :

```powershell
dotnet restore .\NOVA.AI.Client.Mcp\NOVA.AI.Client.Mcp.csproj
dotnet build .\NOVA.AI.Client.Mcp\NOVA.AI.Client.Mcp.csproj --no-restore
dotnet run --project .\NOVA.AI.Client.Mcp\NOVA.AI.Client.Mcp.csproj
```

Endpoints par défaut :

- MCP : `http://127.0.0.1:5280/mcp`
- Santé : `http://127.0.0.1:5280/health`

## Test depuis MCP Inspector

Connectez MCP Inspector à `http://127.0.0.1:5280/mcp`, puis appelez :

```json
{
  "message": "Explique en deux phrases ce qu'est un Knowledge Graph.",
  "systemPrompt": "Tu es un assistant technique .NET, concis et précis.",
  "model": "llama3.2"
}
```

Utilisez ce payload avec `ask_ollama`. Pour `ask_chatgpt`, configurez `AiClient__OpenAI__ApiKey` puis utilisez éventuellement `gpt-4o-mini` comme modèle.

## Notes

- L'API OpenAI est un service externe payant selon le compte et l'utilisation ; une clé API est nécessaire.
- Ollama est local si vous exécutez le modèle sur votre machine ; le téléchargement du modèle consomme de l'espace disque.
- Les exceptions des providers sont converties en messages d'erreur concis dans les outils MCP, sans retourner la clé API.
- Cette génération n'a pas été compilée ici : il faut exécuter les commandes de validation ci-dessus avec le SDK .NET 9 installé.


## Importer un PDF dans un Knowledge Graph Neo4j

Le nouvel outil MCP `add_pdf_to_knowledge_graph` :
1. lit le texte d'un PDF accessible sur la machine du serveur MCP ;
2. découpe le texte en segments ;
3. demande au provider IA sélectionné d'extraire des entités et relations en JSON ;
4. enregistre les nœuds et relations dans Neo4j.

### Configuration

Ajoutez/complétez cette section dans `appsettings.json` :

```json
"KnowledgeGraph": {
  "Neo4jUri": "bolt://localhost:7687",
  "Username": "neo4j",
  "Password": "",
  "Database": "neo4j",
  "MaxPdfCharacters": 80000,
  "MaxChunkCharacters": 8000
}
```

Configurez le mot de passe sans l'enregistrer dans Git :

```powershell
$env:KnowledgeGraph__Password = "VOTRE_MOT_DE_PASSE_NEO4J"
```

Configurez aussi la clé OpenAI avec une variable d'environnement si vous choisissez ChatGPT :

```powershell
$env:AiClient__OpenAI__ApiKey = "VOTRE_CLE_API_OPENAI"
```

### Exemple d'appel MCP

```json
{
  "pdfPath": "C:\\Documents\\architecture.pdf",
  "graphName": "Architecture",
  "provider": "Ollama",
  "model": "llama3.2"
}
```

`pdfPath` est un chemin local vu par le processus MCP, pas par le client MCP. Avec Docker, le PDF doit être monté dans le conteneur et le chemin du conteneur doit être fourni.

L'outil prend en charge les PDF contenant du texte sélectionnable. Les PDF scannés nécessitent une étape OCR, non incluse dans cette version. Le texte est envoyé au fournisseur IA configuré ; n'importez pas de documents confidentiels vers un service externe sans autorisation.

### Visualiser le graphe

Dans Neo4j Browser, vous pouvez inspecter les documents, entités et relations avec :

```cypher
MATCH (d:SourceDocument)-[:CONTAINS_ENTITY]->(e:KGEntity)
RETURN d, e
LIMIT 100;
```

Et les relations entre entités :

```cypher
MATCH (s:KGEntity)-[r:RELATED_TO]->(t:KGEntity)
RETURN s, r, t
LIMIT 100;
```


## Test console: PDF to Knowledge Graph

In `NOVA.AI.Client.Mcp.Test`, choose option **10. Add PDF to Knowledge Graph**.
Enter the PDF path as seen by the MCP server (not necessarily the path on the test client's computer),
the graph name, and `Ollama` or `ChatGPT`. Leave the model blank to use the server configuration.

Requirements: the MCP server must expose `add_pdf_to_knowledge_graph`, the PDF must contain selectable text
(scanned PDFs require OCR), Neo4j must be reachable, and the selected AI provider must be configured.
For Docker, mount the PDF into the MCP server container and enter the container path.
