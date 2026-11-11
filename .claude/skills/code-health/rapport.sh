#!/usr/bin/env bash
# Génère le rapport HTML de l'agent codescene à partir de son résultat JSON.
#
# Usage (depuis la racine du dépôt) : bash .claude/skills/code-health/rapport.sh [resultats.json] [rapport.html]
# Défauts : artifacts/code-health/resultats.json -> artifacts/code-health/rapport.html (artifacts/ est ignoré par Git).
# Le HTML est produit ici, pas par le modèle : toutes les valeurs du JSON sont échappées.
#
# Code 0 : rapport écrit. Code 2 : jq absent, fichier JSON introuvable ou invalide.
set -euo pipefail

entree="${1:-artifacts/code-health/resultats.json}"
sortie="${2:-artifacts/code-health/rapport.html}"

command -v jq >/dev/null 2>&1 || { echo "jq est requis." >&2; exit 2; }
[[ -r "$entree" ]] || { echo "Résultat introuvable : $entree" >&2; exit 2; }
jq empty "$entree" 2>/dev/null || { echo "JSON invalide : $entree" >&2; exit 2; }
mkdir -p "$(dirname "$sortie")"

jq -r '
  def e: tostring | gsub("&";"&amp;") | gsub("<";"&lt;") | gsub(">";"&gt;") | gsub("\"";"&quot;");
  def liste: if (. // []) | length == 0 then "—" else (map(e) | join("<br>")) end;
  def score: if . == null then "—" else tostring end;
  (.verdict // "INCONNU") as $v
  | ($v == "ZERO_BLOQUEUR") as $ok
  | [
  "<!doctype html><html lang=\"fr\"><head><meta charset=\"utf-8\">",
  "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">",
  "<title>Rapport code health</title><style>",
  ":root{--fond:#fff;--texte:#1f2328;--bord:#d0d7de;--ok:#1a7f37;--ko:#cf222e;--doux:#f6f8fa}",
  "@media (prefers-color-scheme:dark){:root{--fond:#0d1117;--texte:#e6edf3;--bord:#30363d;--ok:#3fb950;--ko:#f85149;--doux:#161b22}}",
  "body{background:var(--fond);color:var(--texte);font:15px/1.5 system-ui,sans-serif;margin:0 auto;max-width:1100px;padding:16px}",
  "table{border-collapse:collapse;width:100%;margin:8px 0 24px;display:block;overflow-x:auto}",
  "th,td{border:1px solid var(--bord);padding:6px 10px;text-align:left;vertical-align:top}th{background:var(--doux)}",
  ".verdict{font-size:1.3em;font-weight:600;color:\(if $ok then "var(--ok)" else "var(--ko)" end)}",
  "code{background:var(--doux);padding:1px 4px;border-radius:4px}</style></head><body>",
  "<h1>Rapport code health</h1>",
  "<p class=\"verdict\">\($v | e)</p>",
  "<p>Branche <code>\(.branche // "?" | e)</code> comparée à <code>\(.base // "main" | e)</code>, seuil \(.seuil // "?" | e), \(.iterations // 0 | e) itération(s).</p>",
  "<h2>Fichiers</h2><table><tr><th>Fichier</th><th>Score avant</th><th>Score après</th><th>Smells avant</th><th>Smells après</th><th>Corrections</th></tr>",
  ((.fichiers // [])[] | "<tr><td><code>\(.chemin | e)</code></td><td>\(.score_avant | score)</td><td>\(.score_apres | score)</td><td>\(.smells_avant | liste)</td><td>\(.smells_apres | liste)</td><td>\(.corrections | liste)</td></tr>"),
  "</table>",
  "<h2>Bloqueurs restants</h2>",
  (if (.bloqueurs_restants // []) | length == 0 then "<p>Aucun.</p>"
   else "<table><tr><th>Fichier</th><th>Smell</th><th>Cause</th></tr>" + ((.bloqueurs_restants | map("<tr><td><code>\(.chemin | e)</code></td><td>\(.smell | e)</td><td>\(.cause | e)</td></tr>")) | join("")) + "</table>" end),
  "<h2>Dette existante (non bloquante)</h2>",
  (if (.dette_existante // []) | length == 0 then "<p>Aucune.</p>"
   else "<table><tr><th>Fichier</th><th>Smell</th></tr>" + ((.dette_existante | map("<tr><td><code>\(.chemin | e)</code></td><td>\(.smell | e)</td></tr>")) | join("")) + "</table>" end),
  "<h2>Anomalies du référentiel</h2>",
  "<p>\(.anomalies_referentiel | liste)</p>",
  "<p>Référentiel : <code>codescene/code-health-rules.json</code> (voir <code>codescene/README.md</code>).</p>",
  "</body></html>"
  ] | join("\n")
' "$entree" >"$sortie"

echo "Rapport écrit : $sortie"
