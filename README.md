# M-Files Cloud Monitoring Portal (MFCMP)

Portail de supervision **M-Files Cloud uniquement**, en lecture seule vis-à-vis des coffres.

## Architecture retenue

- Backend : .NET 10 / ASP.NET Core Web API
- Frontend : Angular + TypeScript
- Base de données : SQL Server / Azure SQL
- Connecteurs M-Files : gRPC prioritaire + REST complémentaire
- Identité du portail : compte local bootstrap `admin/admin` (changement obligatoire au premier démarrage)
- Identité M-Files : compte(s) technique(s) distinct(s), à configurer par coffre, en lecture seule
- Microsoft Graph : source de données pour SSO, audit et SCIM uniquement
- Hébergement cible : Azure
- Workers : collectes indépendantes du frontend

## Principes de sécurité

1. Le compte `admin/admin` sert uniquement à accéder au portail. Il n'est jamais utilisé pour se connecter à M-Files.
2. Le mot de passe bootstrap est hashé, le changement au premier login est obligatoire et le compte n'est créé qu'une seule fois.
3. Tous les connecteurs M-Files appliquent une politique stricte de lecture seule.
4. Aucun secret, jeton, cookie ou contenu documentaire sensible ne doit être écrit dans les logs.
5. Une fonctionnalité sans source API/audit officiellement disponible doit indiquer clairement `NOT_AVAILABLE` ou `NOT_CONFIGURED`.
6. Le portail ne prétend pas tracer exhaustivement les appels directs aux API M-Files Cloud sans source de capture exhaustive.

## Structure cible

```text
backend/
  MFCMP.sln
  src/
    MFCMP.Api/
    MFCMP.Application/
    MFCMP.Domain/
    MFCMP.Infrastructure/
    MFCMP.Connectors.MFiles/
    MFCMP.Connectors.Entra/
    MFCMP.Monitoring.Workers/
    MFCMP.Reporting/
  tests/
frontend/
  mfcmp-angular/
database/
  migrations/
  scripts/
deployment/
  docker/
  azure/
  github-actions/
documentation/
  architecture/
  api/
  installation/
  operations/
  security/
```

## Démarrage du développement

Consulter :
- [Cahier des charges](documentation/architecture/functional-technical-specification.md)
- [Architecture](documentation/architecture/architecture.md)
- [Sécurité et lecture seule](documentation/security/security-model.md)
- [Couverture des sources de données](documentation/architecture/data-source-coverage.md)
- [État du développement](DEVELOPMENT_STATUS.md)

## État actuel

Le dépôt contient les documents de cadrage et la structure de démarrage. Les connecteurs réels, l'application et les workers restent à implémenter et à valider avec un coffre M-Files Cloud de test.
