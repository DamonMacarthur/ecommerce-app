#!/usr/bin/env bash
# Lanceur de diagnostic du serveur MCP CodeScene : vérifie binaire et référentiel avant de lancer cs-mcp.
# .mcp.json lance cs-mcp directement : sous Windows, « bash » y désigne WSL et non Git Bash,
# ce qui ferme la connexion. Usage manuel depuis Git Bash : bash .claude/mcp/codescene.sh
#
# - Binaire : CODESCENE_MCP_BIN (défaut : cs-mcp), cherché dans le PATH ou donné par son chemin.
# - Référentiel : .codescene/code-health-rules.json, l'emplacement que CodeScene lit de lui-même
#   à la racine du dépôt (cs-mcp n'accepte pas de chemin en paramètre).
# - Aucun jeton ni identifiant n'est géré ici : la connexion se fait par `cs-mcp auth` (OAuth).
#
# Code 2 : binaire ou référentiel introuvable (le message part sur stderr, stdout reste au protocole MCP).
set -euo pipefail

racine="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
regles="$racine/.codescene/code-health-rules.json"
binaire="${CODESCENE_MCP_BIN:-cs-mcp}"
# Emplacement de l'installeur Windows officiel (install.ps1), utile tant que le PATH n'est pas rechargé.
installe="${LOCALAPPDATA:-}/Programs/cs-mcp/cs-mcp.exe"
if [[ -z "${CODESCENE_MCP_BIN:-}" ]] && ! command -v "$binaire" >/dev/null 2>&1 && [[ -x "$installe" ]]; then
  binaire="$installe"
fi

[[ -r "$regles" ]] || { echo "Référentiel introuvable : $regles" >&2; exit 2; }
command -v "$binaire" >/dev/null 2>&1 || {
  echo "Binaire CodeScene introuvable : $binaire. Définissez CODESCENE_MCP_BIN (nom ou chemin)." >&2
  exit 2
}

# Mêmes garanties que .mcp.json : aucun jeton, outils d'analyse locale uniquement.
export CS_ACCESS_TOKEN=""
export CS_ENABLED_TOOLS="code_health_score,code_health_review,analyze_change_set,pre_commit_code_health_safeguard,rules_config_validate,rules_config_list_thresholds,explain_code_health"
cd "$racine"
# CODESCENE_MCP_ARGS est découpé volontairement en arguments séparés.
# shellcheck disable=SC2086
exec "$binaire" ${CODESCENE_MCP_ARGS:-}
