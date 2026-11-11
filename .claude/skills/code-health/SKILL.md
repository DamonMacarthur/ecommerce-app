---
name: code-health
description: Scanne le code modifié de la branche avec CodeScene (serveur MCP local), corrige les bloqueurs de santé du code, boucle jusqu'à zéro bloqueur (5 itérations maximum) et génère un rapport HTML. À utiliser avant de commiter ou d'ouvrir une pull request, ou quand on demande un scan CodeScene.
argument-hint: "[fichier ou dossier]"
---

# Santé du code (CodeScene)

Cible : $ARGUMENTS (sans argument : tous les fichiers modifiés de la branche).

## 1. Préalable

Vérifie que les outils MCP `codescene` sont disponibles. S'ils ne le sont pas, **arrête-toi** et dis :
« Serveur MCP codescene non connecté : vérifiez `claude mcp list` et la variable `CODESCENE_MCP_BIN` (voir `.claude/mcp/codescene.sh`). »
Ne simule pas de scan et n'invente aucun score.

## 2. Lancer l'agent

Délègue à l'agent `codescene` (`.claude/agents/codescene.md`) avec la cible ci-dessus. Il relit le référentiel
`codescene/code-health-rules.json`, applique la définition de bloqueur, boucle au plus 5 fois et écrit
`artifacts/code-health/resultats.json` puis le rapport HTML avec `bash .claude/skills/code-health/rapport.sh`.

Rappels :
- **Bloqueur** : smell nouveau ou aggravé par rapport à `main`, ou fichier modifié sous le seuil de **9,0** (valeur proposée, à confirmer).
- **Arrêt** : zéro bloqueur, 5 itérations, ou mêmes bloqueurs deux itérations de suite.
- L'agent ne modifie ni le référentiel, ni les tests pour les faire passer, ni le code généré.

## 3. Restitution

Recopie le verdict tel quel :

```
Verdict : ZÉRO BLOQUEUR | N BLOQUEUR(S) RESTANT(S)
Itérations : n / 5
Fichiers corrigés : liste
Rapport : artifacts/code-health/rapport.html
```

S'il reste des bloqueurs, donne pour chacun le fichier, le smell et la cause. Signale toute « anomalie du référentiel » relevée par l'agent :
c'est une personne qui décide de modifier `codescene/code-health-rules.json`, jamais l'agent.
