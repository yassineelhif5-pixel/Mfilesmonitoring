# Modèle de sécurité MFCMP

## Deux identités strictement séparées

### Compte d'accès au portail
- Bootstrap local : admin / admin.
- Uniquement pour authentifier l'administrateur de MFCMP.
- ASP.NET Core Identity et hash de mot de passe.
- Changement obligatoire à la première connexion.
- Création une seule fois, aucune réinitialisation automatique.
- Inscription publique désactivée.
- Cookies sécurisés, protection anti-bruteforce et audit des connexions.

### Identité de supervision M-Files
- Distincte du compte admin du portail.
- Paramétrée par coffre.
- Compte dédié avec permissions minimales de lecture.
- Credentials référencés dans Azure Key Vault.
- Aucun secret en clair en base, fichier de configuration ou logs.

## Politique strictement read-only
Les connecteurs M-Files ne doivent pas exposer les méthodes de création, modification, suppression, check-in/out modifiant le contenu, changement de workflow, permission, utilisateur ou configuration.
Seuls les appels de lecture documentés et les opérations de session nécessaires sont autorisés.
Compléter par des tests automatisés et des permissions M-Files minimales.

## Logs
Ne jamais enregistrer mots de passe, tokens, cookies, secrets, payloads sensibles ou contenu de documents. Expurger les en-têtes Authorization, Cookie, Set-Cookie et données confidentielles.

## Données de monitoring
Toutes les dates en UTC. Séparer les faits observés des déductions. Marquer la source, l'horodatage, la fraîcheur et le niveau de couverture.
