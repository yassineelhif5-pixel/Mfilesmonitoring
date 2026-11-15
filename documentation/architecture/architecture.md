# Architecture MFCMP

## Architecture cible

Angular SPA → ASP.NET Core API → Application/Domain → Infrastructure SQL Server + Key Vault.

Les workers .NET sont déployés séparément du frontend et exécutent les collectes planifiées. Un mécanisme de verrou distribué empêche les exécutions concurrentes en double lorsque plusieurs instances sont déployées.

## Projets backend
- MFCMP.Api : HTTP API, authentification, authorization, SignalR, Swagger.
- MFCMP.Application : cas d'usage, DTO, validations et contrats.
- MFCMP.Domain : entités et règles métier.
- MFCMP.Infrastructure : EF Core, SQL Server, Key Vault, repositories.
- MFCMP.Connectors.MFiles : clients gRPC et REST séparés, read-only.
- MFCMP.Connectors.Entra : Microsoft Graph monitoring.
- MFCMP.Monitoring.Workers : scheduling et collecteurs.
- MFCMP.Reporting : génération de rapports.

## Flux de données
1. Le worker sélectionne les coffres activés.
2. Il charge uniquement les références aux credentials.
3. Il ouvre une session isolée par coffre/identité.
4. Il exécute les opérations de lecture supportées.
5. Il normalise les résultats avec la source et la couverture.
6. Il persiste les données en UTC.
7. Le moteur d'alertes évalue les règles.
8. L'API expose des agrégats paginés à Angular.

## Fiabilité
- Timeout par appel.
- Retry avec exponential backoff et jitter uniquement pour erreurs transitoires.
- Circuit breaker par coffre.
- Limitation de concurrence.
- Idempotence et curseurs incrémentaux.
- Isolation des erreurs par coffre.
- Health checks des workers.
- Logs structurés et expurgés.

## Références officielles
- https://developer.m-files.com/
- https://developer.m-files.com/APIs/gRPC-API/
- https://developer.m-files.com/APIs/REST-API/
- https://learn.microsoft.com/en-us/graph/
