# MFCMP — Cahier des charges fonctionnel et technique

Version 1.0 — 15 novembre 2026

## 1. Objectif
Créer un portail centralisé de supervision de plusieurs coffres M-Files Cloud pour les équipes Support N1/N2/N3 et administrateurs GED. Le système collecte, historise et présente les informations accessibles par les interfaces officielles et sources autorisées. Il ne modifie jamais les coffres surveillés.

## 2. Périmètre
- M-Files Cloud exclusivement.
- Multi-organisations, multi-environnements et multi-coffres.
- Supervision en lecture seule.
- Authentification locale pour le portail.
- Identités techniques M-Files séparées et configurées par coffre.
- Microsoft Graph en source de monitoring SSO/SCIM uniquement.

## 3. Stack
- .NET 10 / ASP.NET Core Web API.
- Angular / TypeScript.
- SQL Server / Azure SQL et EF Core.
- M-Files gRPC prioritaire, REST complémentaire.
- .NET Worker Services et Quartz.NET ou Hangfire.
- Serilog, SignalR, Swagger/OpenAPI.
- Azure Key Vault, App Service ou Container Apps, Application Insights.
- GitHub Actions et Docker.

## 4. Gestion des coffres
Chaque coffre comprend : nom, GUID, host Cloud, organisation, pays, environnement, fuseau horaire, description, état de surveillance, modules activés, fréquences de collecte, paramètres d'authentification et référence Key Vault.

Le test de connexion distingue :
1. Validation du format host/GUID.
2. Résolution DNS et connectivité TLS.
3. Négociation HTTP/2/gRPC.
4. Découverte des capacités/version.
5. Authentification réelle si nécessaire.
6. Accès au coffre avec l'identité configurée.
7. Durées, messages et diagnostics par étape.

Un test réseau anonyme ne doit pas être présenté comme un test d'authentification complet.

## 5. Dashboard global
KPI : coffres surveillés, HEALTHY/WARNING/DEGRADED/CRITICAL/UNKNOWN, incidents, erreurs, appels API observés, créations/modifications documentaires, activité des identités techniques, SSO/SCIM, licences et Aino lorsque les sources sont disponibles.

Graphiques : disponibilité, temps de réponse, créations par jour, erreurs par coffre et par catégorie, opérations API instrumentées, activité par utilisateur/compte technique.

Filtres : période, organisation, pays, environnement, coffre, application et catégorie.

## 6. Health monitoring
Contrôles périodiques indépendants par coffre, fréquence initiale configurable (valeur proposée : 1 minute). Historiser chaque contrôle, durée, résultat, source, timestamp UTC et erreur expurgée. Des contrôles périmés deviennent UNKNOWN. Une panne d'un coffre ne doit pas arrêter les collectes des autres.

## 7. Incidents et erreurs
Catégories : CONNECTIVITY, AUTHENTICATION, API, OBJECT, METADATA, WORKFLOW, IDENTITY, SCIM, SSO, LICENSE, AINO, COLLECTOR, PERFORMANCE.
Sévérités P1–P4. États NEW, ACKNOWLEDGED, IN_PROGRESS, RESOLVED, CLOSED.
Déduplication par empreinte et fenêtre configurable. Affectation, commentaires, chronologie, cause, résolution et export.

## 8. API Activity Monitoring
Tracer les opérations disponibles selon trois sources explicitement identifiées :
- instrumentation des applications clientes contrôlées ;
- passerelle/proxy approuvé lorsque le flux peut être routé ainsi ;
- événements d'audit M-Files officiellement accessibles.

Enregistrer timestamp, coffre, application/identité, protocole, endpoint/RPC, opération, Object Type/ID/version si connus, résultat, durée, correlation/activity ID, source et niveau de complétude.

Ne jamais prétendre que tous les appels directs M-Files Cloud sont capturés sans mécanisme exhaustif. Ne jamais enregistrer par défaut les mots de passe, tokens, cookies ou contenu documentaire.

## 9. Logs et événements
Centraliser uniquement les événements accessibles : audit M-Files, logs des intégrations instrumentées, logs des collecteurs et sources Microsoft Graph autorisées. Ne pas supposer l'accès aux fichiers de logs internes des serveurs M-Files Cloud.

## 10. Analytics documentaires
Collecter les propriétés système et recherches documentées via pagination et curseurs incrémentaux. Ventiler créations et modifications par jour, classe, type, workflow, utilisateur et compte technique lorsque l'identité est observable. Les suppressions nécessitent une source d'audit adaptée. Éviter les scans complets répétés.

## 11. Identités, licences et Aino
Microsoft Graph : provisioning logs, sign-in logs, audit logs, applications et service principals selon permissions minimales.
Licences et Aino : développer des providers isolés. Afficher NOT_AVAILABLE/NOT_CONFIGURED en l'absence de source officielle confirmée. Ne pas estimer les quotas ou licences à partir de métriques indirectes.

## 12. Alertes et rapports
Alertes configurables : indisponibilité, dégradation, hausse d'erreurs, lenteur, échec de collecteur, erreurs SCIM/SSO, expiration credential, seuil de licence/Aino confirmé. Prévoir déduplication, maintenance windows et notifications.
Rapports : health, disponibilité, incidents, API, documents, utilisateurs, comptes techniques, licences, Aino, SSO et SCIM. Exports CSV/XLSX/PDF et planification.

## 13. Administration et authentification du portail
Compte local bootstrap initial : username `admin`, mot de passe initial `admin`. Créé une seule fois si aucun administrateur n'existe. Hash sécurisé, changement obligatoire au premier login, pas de réinitialisation au redémarrage, inscription publique désactivée. Ce compte ne sert qu'au portail et n'est jamais utilisé pour M-Files.
Microsoft Entra ID n'est pas utilisé pour le login du portail. Microsoft Graph reste un connecteur de monitoring.
Les secrets M-Files et Graph sont conservés dans Azure Key Vault ; SQL ne stocke que des références.

## 14. Sécurité et lecture seule
Tous les connecteurs M-Files doivent exposer des abstractions de lecture seule. N'implémenter ni appeler aucune opération M-Files de création, modification, suppression, check-in/out modifiant le contenu, workflow, permissions, utilisateurs ou administration.
Les opérations de session strictement nécessaires à l'authentification, keepalive et logout sont autorisées. Compléter la contrainte par des permissions minimales au niveau du compte M-Files.
Le portail peut gérer sa propre configuration locale, ses incidents, ses alertes et ses rapports.

## 15. Modèle de données initial
Organizations, Environments, Vaults, VaultConnections, VaultModules, MonitoringConfigurations, CredentialReferences, HealthChecks, HealthHistory, MonitoringMetrics, CollectionJobs, CollectionCursors, CollectionErrors, ApiRequests, ApiErrors, ObjectStatistics, UserActivity, ServiceAccounts, ServerEvents, IdentityApplications, IdentityLogs, ScimJobs, ScimEvents, LicenseSnapshots, AinoUsageSnapshots, Incidents, IncidentHistory, IncidentComments, Alerts, AlertRules, AlertNotifications, Reports, ReportSchedules, ReportExecutions, PortalUsers, Roles, Permissions, UserRoleAssignments, AuditLogs, SystemSettings.

## 16. Fréquences initiales proposées
Health 1 min ; erreurs et événements 1–5 min selon source ; statistiques documentaires/utilisateurs 15 min ; SCIM/SSO 5 min selon limites Microsoft ; licences 6 h ; Aino selon source et API. Configurables et soumises aux quotas/limites fournisseurs.

## 17. Critères d'acceptation
Ajouter un coffre Cloud, tester de façon distincte connectivité et authentification, persister la configuration, exécuter un health worker en arrière-plan, historiser les résultats, afficher le dashboard Angular, supporter plusieurs coffres indépendamment, fournir erreurs et diagnostics, ne jamais modifier les coffres, et marquer clairement les sources non disponibles.

## 18. Livrables
Code backend et frontend, migrations SQL, connecteurs gRPC/REST/Graph, workers, tests, documentation installation/sécurité/API, configuration Azure et CI/CD.
