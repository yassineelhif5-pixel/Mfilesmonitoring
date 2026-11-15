# Data Source Coverage

Chaque métrique affichée doit fournir : status, source, lastCollectedAtUtc, freshness, completeness et message de diagnostic.

Statuts autorisés :
- AVAILABLE : donnée récupérée depuis une source configurée.
- PARTIAL : couverture partielle explicitement expliquée.
- NOT_CONFIGURED : connecteur ou credential non configuré.
- NOT_AVAILABLE : source officielle ou permission indisponible.
- STALE : donnée trop ancienne.
- ERROR : dernière collecte en erreur.

Règles :
- Une donnée absente n'est jamais affichée comme zéro si la collecte a échoué.
- Les appels API instrumentés ne représentent pas automatiquement tous les appels directs vers M-Files Cloud.
- Les statistiques de suppression nécessitent un audit/event source adapté.
- Les journaux internes des serveurs M-Files Cloud ne sont pas présumés accessibles.
- Les quotas Aino et données de licences doivent provenir de sources officielles confirmées.
