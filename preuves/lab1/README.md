# Preuves — lab 1

Sorties copiées telles quelles depuis la session du 2026-11-10, contre l'application déjà en
cours d'exécution (passerelle `http://localhost:5067`, bases en mémoire).

## 1. Test de fumée (première exécution)

Commande : `bash .claude/skills/smoke-test/smoke.sh`

```
L'application tourne déjà : le test l'utilise et la laissera tourner.

Vérification                                  Attendu  Obtenu   Statut
Catalogue : GET /catalog/api/products          200      200      OK (4 produits)
Commande valide : POST /ordering/api/orders    201      201      OK (total 239.98)
Produit inconnu : POST /ordering/api/orders    400      400      OK ("Product 99 does not exist.")

SMOKE OK
```

## 2. Test de fumée (deuxième exécution)

Même commande, même sortie :

```
L'application tourne déjà : le test l'utilise et la laissera tourner.

Vérification                                  Attendu  Obtenu   Statut
Catalogue : GET /catalog/api/products          200      200      OK (4 produits)
Commande valide : POST /ordering/api/orders    201      201      OK (total 239.98)
Produit inconnu : POST /ordering/api/orders    400      400      OK ("Product 99 does not exist.")

SMOKE OK
```

## 3. Commande avec un client vide

```bash
curl -i -X POST http://localhost:5067/ordering/api/orders -H 'Content-Type: application/json' \
  -d '{"customer":"","items":[{"productId":1,"quantity":2}]}'
```

Sortie (la barre de progression de curl est omise) :

```
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Date: Tue, 10 Nov 2026 11:19:06 GMT
Server: Kestrel
Location: https://localhost:7082/api/orders/4
Transfer-Encoding: chunked

{"id":4,"customer":"","createdAt":"2026-11-10T11:19:06.7051602+00:00","status":"Confirmed","items":[{"id":4,"productId":1,"productName":"Mechanical Keyboard","unitPrice":119.99,"quantity":2}],"total":239.98}
```

## 4. Commande avec une quantité négative

```bash
curl -s -X POST http://localhost:5067/ordering/api/orders -H 'Content-Type: application/json' \
  -d '{"customer":"Ada","items":[{"productId":1,"quantity":-3}]}' | jq .total
```

Sortie (seul le total est affiché ; le code HTTP n'a pas été relevé) :

```
-359.97
```
