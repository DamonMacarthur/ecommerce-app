# Critères de l'issue #11 « frontend » : habillage CSS moderne du Web ECommerce

Page : https://localhost:7218/
Pages concernées : `/` (Home), `/products` (Catalog), `/orders` (Orders), le menu latéral et la barre du haut.
Captures : preuves/ui/captures/

Périmètre (fichiers modifiables) :
- `src/ECommerce.Web/wwwroot/app.css`
- `src/ECommerce.Web/Components/Layout/MainLayout.razor`, `MainLayout.razor.css`
- `src/ECommerce.Web/Components/Layout/NavMenu.razor`, `NavMenu.razor.css`
- `src/ECommerce.Web/Components/Pages/Home.razor`, `Products.razor`, `Orders.razor` et leurs `.razor.css`, à créer si besoin
- `src/ECommerce.Web/README.md`

Hors périmètre :
- `App.razor`, `Routes.razor`, `ReconnectModal.*`, `Error.razor`, `NotFound.razor` ;
- `Services/**`, `Program.cs`, `appsettings*.json` ;
- `wwwroot/lib/**` : Bootstrap reste chargé tel quel ;
- toute API, tout autre projet que `ECommerce.Web`.

Aucun CDN, aucune police téléchargée, aucun JavaScript ajouté.

Contrainte de non-régression : seuls le balisage de présentation et le CSS changent. La logique C# et les textes visibles restent identiques : titres, libellés, boutons, messages, contenu des tableaux et des cartes. Les liens et les formulaires existants restent atteignables au clavier, dans le même ordre.

## Phase 1 · Habillage

Avant : rien (l'application tourne).

| Id | Critère |
|---|---|
| M1 | **Jetons de design** : couleurs, rayons, ombres, espacements et typographie sont définis une seule fois, en propriétés personnalisées CSS (`--…`) sur `:root` dans `app.css`, et réutilisés par les autres feuilles. Aucune couleur n'est codée en dur hors de ces jetons, sauf `transparent` et `currentColor`. |
| M2 | **Thème sombre** : avec `prefers-color-scheme: dark`, le fond, le texte, les cartes, le menu et les champs passent en sombre, en redéfinissant les jetons. Le contraste texte/fond reste d'au moins 4,5:1 dans les deux thèmes. |
| M3 | **Typographie** : une pile de polices système (`system-ui, -apple-system, "Segoe UI", Roboto, …`), des tailles de titres fluides avec `clamp()`, et un interlignage confortable, d'environ 1,5 pour le texte. |
| M4 | **Menu latéral** : une surface sobre, issue des jetons, remplace le dégradé violet du gabarit. Le lien actif se distingue nettement, par un fond ou une barre d'accent. Les icônes restent alignées. |
| M5 | **Mise en page** : le contenu est centré, avec une largeur de lecture maximale. Les cartes de Home, la grille des produits et la liste des commandes utilisent CSS Grid (`repeat(auto-fill, minmax(…))`) au lieu de colonnes fixes. |
| M6 | **Cartes** : coins arrondis, ombre légère et bordure discrète ; au survol de la souris, une élévation douce. Les badges de statut de commande ont une couleur qui reflète le statut, lisible dans les deux thèmes. |
| M7 | **Interaction** : des transitions courtes (200 ms au plus) au survol et au focus, désactivées sous `prefers-reduced-motion: reduce`. `:focus-visible` affiche un anneau de focus net sur les liens, les boutons et les champs, dans les deux thèmes. |
| M8 | **Responsive** : à 320 px de large, aucun défilement horizontal. Le menu se replie comme avant, et les boutons restent atteignables. |
| M9 | **Non-régression** : aucun texte visible n'a changé. `dotnet build ECommerce.slnx` et `dotnet test ECommerce.slnx` passent. |
