#!/usr/bin/env bash
# Serveur MCP CodeScene (local, propre à ce dépôt). Déclaré dans .mcp.json à la racine.
#
# - Binaire : CODESCENE_MCP_BIN (défaut : cs-mcp), cherché dans le PATH ou donné par son chemin.
# - Référentiel : codescene/code-health-rules.json, exporté dans CODESCENE_RULES_FILE.
#   CODESCENE_RULES_FILE est une variable de ce dépôt, pas une option connue du binaire :
#   si celui-ci attend une option de ligne de commande, la mettre dans CODESCENE_MCP_ARGS.
# - Aucun jeton ni identifiant n'est géré ici.
#
# Code 2 : binaire ou référentiel introuvable (le message part sur stderr, stdout reste au protocole MCP).
set -euo pipefail

racine="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
regles="$racine/codescene/code-health-rules.json"
binaire="${CODESCENE_MCP_BIN:-cs-mcp}"

[[ -r "$regles" ]] || { echo "Référentiel introuvable : $regles" >&2; exit 2; }
command -v "$binaire" >/dev/null 2>&1 || {
  echo "Binaire CodeScene introuvable : $binaire. Définissez CODESCENE_MCP_BIN (nom ou chemin)." >&2
  exit 2
}

export CODESCENE_RULES_FILE="$regles"
cd "$racine"
# CODESCENE_MCP_ARGS est découpé volontairement en arguments séparés.
# shellcheck disable=SC2086
exec "$binaire" ${CODESCENE_MCP_ARGS:-}
