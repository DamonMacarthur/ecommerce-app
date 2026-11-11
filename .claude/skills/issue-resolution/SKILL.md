---
name: issue-resolution
description: Traite le backlog GitHub du dépôt. Lit les issues ouvertes, crée une branche feature/<nom>-<numéro> par ticket, confie chaque ticket à l'agent adéquat (codescene, test-integration...), fait relire le résultat et ouvre une pull request. À utiliser quand on demande de résoudre, traiter ou dépiler des issues ou des tickets.
argument-hint: "[numéros d'issues] [--base <branche>]"
disable-model-invocation: true
---

# Résolution des issues

Tickets demandés : $ARGUMENTS (sans argument : toutes les issues ouvertes).

État actuel :
- Branche : !`git branch --show-current`
- Modifications non commitées : !`git status --short`

Lis `.claude/agents/issue-resolution.md` et applique-le **toi-même, dans cette session**. Ne le lance pas comme sous-agent :
un sous-agent ne peut pas en lancer d'autres, et l'orchestrateur doit pouvoir appeler `codescene`, `general-purpose` et les skills.

Si des modifications non commitées apparaissent ci-dessus, arrête-toi avant toute action et demande à la personne de les commiter ou de les mettre de côté.
