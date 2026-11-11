# codescene/

Référentiel local de santé du code (CodeScene) pour ce dépôt. Le JSON ne peut pas contenir de commentaires :
ce fichier explique **pourquoi** chaque jeu de règles existe.

- Fichier de règles : [code-health-rules.json](code-health-rules.json)
- Agent qui s'en sert : [.claude/agents/codescene.md](../.claude/agents/codescene.md) (il relit le JSON à chaque exécution)
- Conception de l'agent, composants et plan d'action : [conception-agent.md](conception-agent.md)

## Fonctionnement

Les jeux (`rule_sets`) sont évalués **de haut en bas : le premier dont `matching_content_path` correspond s'applique**, les suivants
sont ignorés pour ce fichier. L'ordre compte : les exclusions (poids 0) viennent d'abord.

Un poids de `0.0` désactive la règle pour les fichiers concernés ; `0.5` la tolère à moitié. Les seuils (`thresholds`) utilisent uniquement
les noms déjà présents dans le fichier (`function_lines_of_code_warning`, `function_cyclomatic_complexity_warning`,
`function_nesting_depth_warning`).

## Jeux de règles

| # | Chemin | Réglage | Pourquoi |
|---|---|---|---|
| 1 | `**/Migrations/**` | Large Method, Complex Method, Brain Method, Code Duplication à 0 | Migrations EF Core générées, non maintenues à la main |
| 2 | `**/*.Designer.cs` | idem | Fichiers générés par les designers |
| 3 | `**/wwwroot/lib/**` | idem | Bootstrap est versionné dans `ECommerce.Web` : code tiers, il fausserait tous les scores |
| 4 | `**/*.razor.js` | idem | JavaScript de gabarit Blazor (`ReconnectModal.razor.js`) |
| 5 | `tests/**` | Duplication et Large Method à 0,5 ; alerte à 100 lignes | Arrange/Act/Assert, fabriques de test |
| 6 | `**/Data/*DbContext*.cs` | Large Method à 0,5 | `OnModelCreating` est déclaratif et grossit avec les données `HasData` |
| 7 | `**/Endpoints/**` | Complexité cyclomatique 9, imbrication 3 | Gestionnaires d'API minimale. **Seuils provisoires**, voir ci-dessous |
| 8 | `**/Models/**` | Code Duplication à 0,5 | DTO dupliqués par conception (pas de projet de contrats partagé) |
| 9 | `**/Services/**` | Code Duplication à 0,5 | Clients HTTP typés et DTO copiés des autres services |
| 10 | `**/Program.cs` | Large Method à 0,5 | Racines de composition : câblage linéaire |
| 11 | `src/ECommerce.AppHost/**` | Large Method à 0,5 | Orchestration Aspire, déclaration linéaire des ressources |
| 12 | `src/ECommerce.ServiceDefaults/**` | Large Method à 0,5 | Gabarit Aspire fourni par le modèle de projet |

## Seuils provisoires des `Endpoints/`

Les valeurs 9 et 3 sont des **valeurs de départ, pas une mesure**. `CreateOrder` (Ordering.Api) est le plus gros gestionnaire du dépôt : validation,
appel à Catalog, enregistrement. Elles seront calibrées par un scan de référence une fois le serveur MCP CodeScene branché. Tant que ce n'est
pas fait, ne pas les assouplir pour faire disparaître un constat.

## Ce qui a été retiré

Les jeux `**/*.Domain/**` et `**/*.Application/**` ne correspondaient à aucun projet de ce dépôt (les projets sont `Catalog.Api`,
`Ordering.Api`, `Gateway`, `Web`, `AppHost`, `ServiceDefaults`). Aucune règle n'est prévue sur `bin/` et `obj/` : ils ne sont pas
versionnés, et CodeScene analyse les fichiers suivis par Git.

## Limites connues

- La prise en charge des fichiers Razor (`*.razor`) par l'outil n'est pas vérifiée : aucune règle dédiée.
- Un chemin sans jeu correspondant est évalué avec les règles par défaut de l'outil.

## Modifier ce référentiel

Une personne relit le diff avant toute modification. L'agent `codescene` n'y touche jamais (ni pour atteindre zéro bloqueur, ni autrement) ;
s'il juge une règle inadaptée, il le signale dans son rapport. Après un changement, mettre à jour ce fichier pour que le tableau
ci-dessus reste identique au JSON.
