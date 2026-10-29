# Intégration Y10.AI.Client dans NOVA.AI.Client.Mcp

Le nouveau projet `Y10.AI.Client` est référencé par `NOVA.AI.Client.Mcp`.
Il centralise les appels vers ChatGPT (API OpenAI) et Ollama, tandis que `NovaAiTools`
publie les fonctions correspondantes sous forme d'outils MCP.

Consulter `NOVA.AI.Client.Mcp/README.md` pour la configuration, le démarrage et les tests.
Le code de logging existant a été conservé.


L'outil MCP `add_pdf_to_knowledge_graph` permet également d'extraire des entités et relations depuis un PDF et de les enregistrer dans Neo4j. Voir `NOVA.AI.Client.Mcp/README.md` pour la configuration et les limites.
