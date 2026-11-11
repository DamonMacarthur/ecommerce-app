# Agent `codescene` : conception, composants et plan d'action

Périmètre : **ce dépôt uniquement, tout en local**. Rien dans `~/.claude`, `~/.config` ni la configuration utilisateur.
Aucun jeton et aucun fichier d'identifiants n'est prévu.

## 1. Objectif

Brancher l'outil CodeScene local sur Claude Code par un serveur MCP, avec [code-health-rules.json](code-health-rules.json) comme référentiel,
et fournir un agent qui :

1. **scanne** le code modifié de la branche ;
2. **corrige** les problèmes de santé du code (code smells) ;
3. **génère un rapport HTML** ;
4. **boucle** jusqu'à zéro bloqueur, avec une limite qui évite la boucle sans fin.

## 2. Décisions

| Sujet | Décision |
|---|---|
| Accès à CodeScene | Binaire local, lancé par un wrapper. Pas de service distant ajouté par ce dépôt. |
| Bloqueur | Sur les fichiers modifiés de la branche : smell nouveau ou aggravé par rapport à `main`, ou Code Health sous le seuil (**9,0 proposé**, à confirmer). |
| Boucle | Bornée dans l'agent : 5 itérations au plus ; arrêt aussi si deux itérations de suite donnent les mêmes bloqueurs. |
| Référentiel | Reste dans `codescene/`. Repli sur `.codescene/` seulement si le binaire n'accepte pas de chemin explicite, avec accord. |
| Source du référentiel | Le dépôt GitHub a servi **une seule fois**, pour réécrire le référentiel. Ensuite, tout vient des fichiers locaux. |

## 3. Vue d'ensemble

```
/code-health [cible]                      (skill : point d'entrée)
      │
      ▼
agent codescene ──► MCP codescene ──► binaire local ──► codescene/code-health-rules.json
      │                (via .mcp.json + codescene.sh)
      │   boucle (5 itérations au plus) :
      │   1. périmètre   git diff main...HEAD + non commités, hors exclusions du référentiel
      │   2. scan        score Code Health + smells, comparaison avec main
      │   3. bloqueurs ? ── non ──► rapport
      │        │ oui
      │   4. correction  plus petit refactoring, sans changer le comportement
      │   5. vérification dotnet build / test / format  (hooks : formater.sh, stop-tests-verts.sh)
      ▼
resultats.json ──► rapport.sh ──► artifacts/code-health/rapport.html
```

## 4. Composants

| Composant | Fichier | Rôle | État |
|---|---|---|---|
| Référentiel | [code-health-rules.json](code-health-rules.json) | 12 jeux de règles adaptés au dépôt (premier correspondant gagne) | **Fait** |
| Documentation du référentiel | [README.md](README.md) | Raison de chaque jeu de règles | **Fait** |
| Agent | [../.claude/agents/codescene.md](../.claude/agents/codescene.md) | Scan, correction, boucle, rapport ; relit le JSON à chaque exécution ; aucun outil réseau | **Fait** |
| Serveur MCP | `.mcp.json` (racine) + `.claude/mcp/codescene.sh` | Déclare `codescene` ; le wrapper vérifie le binaire (`CODESCENE_MCP_BIN`, défaut `cs-mcp`), exporte `CODESCENE_RULES_FILE`, passe `CODESCENE_MCP_ARGS`, puis `exec` | **Fichiers faits**, connexion en attente du binaire |
| Skill | `.claude/skills/code-health/SKILL.md` | `/code-health [cible]` : vérifie le MCP, lance l'agent, rappelle bloqueur, seuil, limite, restitue le verdict | **Fait** |
| Rapport | `.claude/skills/code-health/rapport.sh` | Lit `artifacts/code-health/resultats.json` avec `jq`, produit `rapport.html` (HTML généré par script, valeurs échappées, thème clair et sombre) | **Fait** |
| Garde | `.claude/hooks/garde-codescene.sh` | Hook PreToolUse : refuse l'écriture du référentiel, de `Data/Migrations/**` et de `*.Designer.cs` | À faire |
| Permissions | `.claude/settings.json` | `allow` `mcp__codescene__*`, `deny` `Edit`/`Write` sur `codescene/**` | À faire (en dernier) |

### 4.1 Référentiel

| # | Chemin | Réglage | Pourquoi |
|---|---|---|---|
| 1 | `**/Migrations/**` | 4 règles à 0 | Migrations EF Core générées |
| 2 | `**/*.Designer.cs` | 4 règles à 0 | Fichiers générés |
| 3 | `**/wwwroot/lib/**` | 4 règles à 0 | Bootstrap versionné (code tiers) |
| 4 | `**/*.razor.js` | 4 règles à 0 | JavaScript de gabarit Blazor |
| 5 | `tests/**` | Duplication, Large Method à 0,5 ; alerte à 100 lignes | Arrange/Act/Assert |
| 6 | `**/Data/*DbContext*.cs` | Large Method à 0,5 | `OnModelCreating` déclaratif, grossit avec `HasData` |
| 7 | `**/Endpoints/**` | Complexité 9, imbrication 3 | Gestionnaires d'API minimale. **Seuils provisoires** |
| 8 | `**/Models/**` | Duplication à 0,5 | DTO dupliqués par conception |
| 9 | `**/Services/**` | Duplication à 0,5 | Clients HTTP typés, DTO copiés |
| 10 | `**/Program.cs` | Large Method à 0,5 | Racines de composition |
| 11 | `src/ECommerce.AppHost/**` | Large Method à 0,5 | Orchestration Aspire |
| 12 | `src/ECommerce.ServiceDefaults/**` | Large Method à 0,5 | Gabarit Aspire |

Retirés : `*.Domain` et `*.Application` (aucun projet ne correspond). Seuls les noms de seuils déjà présents dans le fichier sont utilisés.

### 4.2 Agent

- Au début de **chaque** exécution, il lit le JSON et le README du référentiel au lieu d'en recopier les valeurs.
- Il ne corrige jamais les fichiers exclus (Migrations, Designer, `wwwroot/lib`, `*.razor.js`), ne « dédoublonne » pas les DTO dupliqués par conception,
  et signale « seuil à confirmer » pour un bloqueur dans `Endpoints/` plutôt que de forcer un refactoring large.
- Il ne modifie jamais le référentiel, ne baisse aucun seuil, ne modifie aucun test pour le faire passer, ne change pas le comportement visible d'une API,
  ne commite ni ne pousse.
- Il produit toujours un rapport, même en cas d'arrêt anticipé.

### 4.3 Format de `resultats.json` (proposé)

```json
{
  "branche": "feat/...", "base": "main", "seuil": 9.0, "iterations": 3, "verdict": "ZERO_BLOQUEUR",
  "fichiers": [
    { "chemin": "src/.../OrderingEndpoints.cs", "score_avant": 8.1, "score_apres": 9.3,
      "smells_avant": ["Complex Method"], "smells_apres": [], "corrections": ["Extraction de la validation"] }
  ],
  "bloqueurs_restants": [ { "chemin": "...", "smell": "...", "cause": "..." } ],
  "dette_existante": [ { "chemin": "...", "smell": "..." } ],
  "anomalies_referentiel": ["..."]
}
```

Sections du rapport HTML : verdict et nombre d'itérations, tableau avant/après par fichier, corrections appliquées, bloqueurs restants et leur cause,
dette existante, rappel du référentiel utilisé.

### 4.4 Hooks

| Hook | Rôle | Statut |
|---|---|---|
| PostToolUse `Edit\|Write` : `formater.sh` | Formate après chaque modification | Existant |
| Stop : `stop-tests-verts.sh` | Empêche de conclure avec des tests rouges (3 blocages au plus) | Existant |
| PreToolUse `Bash` : `garde-git.sh` | Bloque les commandes Git qui réécrivent l'historique | Existant |
| PreToolUse `Edit\|Write\|MultiEdit` : `garde-codescene.sh` | Protège le référentiel et le code généré, avec un message explicite | Nouveau |

À signaler : `settings.json` référence `.claude/hooks/bloquer-secrets.sh`, qui n'existe pas. Hors périmètre ; à corriger ou retirer sur décision.

### 4.5 Commentaires

- Scripts : en-tête en français (rôle, codes de retour), comme les scripts existants.
- `.mcp.json` n'accepte pas de commentaires : le fonctionnement sera documenté dans l'en-tête du wrapper et dans une courte section de `CLAUDE.md`.
- Référentiel : champs `*_doc` pour chaque jeu et chaque seuil ; le détail vit dans [README.md](README.md).
- Code C# corrigé : commentaires en anglais, seulement quand le pourquoi n'est pas évident, jamais pour décrire le refactoring.

## 5. Plan d'action

| # | Action | Qui | État | Vérification |
|---|---|---|---|---|
| 1 | Réécrire `code-health-rules.json` d'après le dépôt GitHub (dernier usage du dépôt distant) | Claude | **Fait** | JSON valide ; chaque chemin correspond à des fichiers locaux |
| 2 | Créer `.claude/agents/codescene.md` en tenant compte du référentiel | Claude | **Fait** | En-tête valide, aucun outil réseau |
| 2b | Créer `codescene/README.md` | Claude | **Fait** | Les 12 jeux du JSON y figurent |
| 3 | Relire le diff des fichiers CodeScene | Vous | **À faire** | Accord explicite |
| 4 | Fournir le nom ou le chemin du binaire CodeScene local | Vous | À faire | Le binaire se lance depuis un terminal |
| 5 | Créer `codescene.sh` et `.mcp.json` | Claude | **Fait** (connexion non vérifiée : aucun binaire installé) | `bash -n` OK ; sans binaire, le wrapper sort en code 2 avec un message clair ; `claude mcp list` à vérifier après l'étape 4 |
| 6 | Relever les outils MCP, confirmer le chemin du référentiel, scan de référence, calibrer les seuils d'`Endpoints/` | Claude | À faire | Un appel renvoie un score pour `OrderingEndpoints.cs` ; diff à valider ; seuil 9,0 confirmé |
| 7 | Créer le skill `/code-health` et `rapport.sh` | Claude | **Fait** | `bash -n` OK ; rapport généré depuis un exemple, HTML injecté bien échappé ; JSON absent : code 2 |
| 8 | Ajouter permissions, `deny` sur `codescene/**` et `garde-codescene.sh` (en dernier, sinon le calibrage de l'étape 6 serait bloqué) | Claude | À faire | Un `Edit` sur `codescene/` est refusé |
| 9 | Essai sur une branche jetable avec un smell volontaire | Claude | À faire | La boucle converge à zéro bloqueur |
| 10 | Essai avec un smell impossible à lever | Claude | À faire | Arrêt à 5 itérations ou sans progrès, le rapport l'indique |
| 11 | Documenter dans `CLAUDE.md` | Claude | À faire | Section courte et relue |

Vérifications transversales : `dotnet build ECommerce.slnx` et `dotnet test ECommerce.slnx` restent verts ; le référentiel n'évalue pas `Data/Migrations/` ;
aucun jeton ni fichier d'identifiants n'est créé dans le dépôt.

## 6. Risques et inconnues

- Le binaire local peut ne pas accepter un chemin de référentiel explicite : repli sur `.codescene/` avec accord.
- Les noms des outils MCP et du binaire sont des hypothèses tant que le binaire n'est pas branché.
- La prise en charge de Razor par l'outil n'est pas confirmée.
- Seuils mal calibrés : trop stricts, l'agent refactore trop ; trop souples, ils masquent des défauts. D'où le scan de référence et la relecture humaine.
- Un refactoring peut changer le comportement : le filet est de 9 tests, sans test Web ni Gateway.
- Boucle sans fin : bornée par 5 itérations et par la détection d'absence de progrès.

## 7. Hors périmètre

Catégories, publication du rapport en Artifact, correction du hook `bloquer-secrets.sh`, toute configuration hors du dépôt.
