# NOVA.AI.Client.Mcp.Test

Application console .NET 9 permettant de tester `NOVA.AI.Client.Mcp` sans MCP Inspector. Le client se connecte au serveur MCP via Streamable HTTP, découvre les outils disponibles, puis permet de les appeler depuis un menu interactif.

## Prérequis

- SDK .NET 9.
- `NOVA.AI.Client.Mcp` démarré sur `http://localhost:5280` (ou endpoint configuré).
- Pour tester Ollama : Ollama démarré et modèle téléchargé sur la machine ou le serveur configuré par le MCP.
- Pour tester ChatGPT : clé OpenAI configurée dans l'environnement du processus `NOVA.AI.Client.Mcp`.
- Pour tester le logging : API centrale de logging accessible depuis le serveur MCP.

## Démarrage

Terminal 1 — démarrer le serveur MCP depuis le dépôt principal :

```powershell
dotnet run --project .\NOVA.AI.Client.Mcp\NOVA.AI.Client.Mcp.csproj
```

Terminal 2 — démarrer le client de test depuis ce dossier :

```powershell
dotnet restore
dotnet build
dotnet run
```

Le client affiche les outils réellement découverts. Il ne peut appeler que les outils présents sur le serveur MCP démarré.

## Menu de tests

| Option | Test |
|---|---|
| 1 | Appeler `ask_chatgpt` |
| 2 | Appeler `ask_ollama` |
| 3 | Appeler `ask_ai` avec le provider par défaut du serveur |
| 4 | Appeler `ask_ai` en forçant `ChatGPT` |
| 5 | Appeler `ask_ai` en forçant `Ollama` |
| 6 | Soumettre un log Information via `submit_log` |
| 7 | Soumettre un log Warning via `submit_log` |
| 8 | Soumettre un log Error via `submit_log` |
| 9 | Exécuter `write_logging_diagnostic` |
| L | Relister les outils disponibles |
| 0 | Quitter |

Pour les tests IA, le client demande la question, le system prompt et un modèle facultatif. Si le modèle est laissé vide, le serveur utilise le modèle défini dans sa configuration.

## Configuration

Le endpoint est modifiable dans `appsettings.json` :

```json
{
  "Mcp": { "Endpoint": "http://localhost:5280/mcp" }
}
```

Ou via PowerShell :

```powershell
$env:Mcp__Endpoint = "http://localhost:5280/mcp"
dotnet run
```

La configuration du fournisseur IA reste dans `NOVA.AI.Client.Mcp/appsettings.json`. Pour OpenAI, ne stockez pas la clé dans un fichier versionné :

```powershell
$env:AiClient__OpenAI__ApiKey = "VOTRE_CLE_API_OPENAI"
```

Pour Ollama, sur la machine qui exécute le serveur MCP :

```powershell
ollama pull llama3.2
ollama list
```

Si le serveur MCP tourne dans Docker, `localhost` désigne le conteneur MCP. Pour joindre Ollama installé sur Windows depuis Docker Desktop, configurez par exemple `AiClient__Ollama__BaseUrl=http://host.docker.internal:11434`.

## Résultats et dépannage

- **Connexion refusée** : vérifiez le serveur MCP, le port et le chemin `/mcp`.
- **Outil absent** : le client affiche les outils réellement exposés ; démarrez la bonne version du serveur MCP.
- **OpenAI échoue** : vérifiez `AiClient__OpenAI__ApiKey`, le modèle et l'accès réseau. L'API OpenAI peut entraîner des coûts.
- **Ollama échoue** : vérifiez l'URL, le modèle et que le serveur Ollama est accessible depuis le processus MCP.
- **Logging échoue** : vérifiez l'endpoint `CentralLoggingForwarding:Endpoint` dans la configuration du serveur MCP et les logs des services.
- **202 Accepted** : indique que l'API a accepté le log, mais ne garantit pas à lui seul sa persistance finale dans MongoDB.

Cette application est un client d'intégration manuel, pas une suite de tests unitaires automatisés. Elle ne remplace pas la compilation et la validation dans l'environnement de développement. Elle permet de tester sans MCP Inspector.


## Test console: PDF to Knowledge Graph

In `NOVA.AI.Client.Mcp.Test`, choose option **10. Add PDF to Knowledge Graph**.
Enter the PDF path as seen by the MCP server (not necessarily the path on the test client's computer),
the graph name, and `Ollama` or `ChatGPT`. Leave the model blank to use the server configuration.

Requirements: the MCP server must expose `add_pdf_to_knowledge_graph`, the PDF must contain selectable text
(scanned PDFs require OCR), Neo4j must be reachable, and the selected AI provider must be configured.
For Docker, mount the PDF into the MCP server container and enter the container path.
