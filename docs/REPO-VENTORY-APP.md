# Repo público `aeeb-dev/ventory-app`

Escritorio Windows **separado** de la web (`inv-mkt-seguimiento` / Coolify en `main`).

## ¿Migrar releases o empezar aquí?

| Opción | Recomendación |
|--------|----------------|
| Mover tags/releases del repo privado | **No**. Los releases no se “trasladan”; los clientes ya instalados apuntan a otro repo. |
| **Copiar código + publicar solo en `ventory-app`** | **Sí**. Un único salto (reinstalar o un release puente) y luego todo público, sin PAT. |
| Seguir publicando en `inv-mkt-seguimiento` | **No** a medio plazo (privado, workflow en la web, confusión). |

## Impacto en la web

**Ninguno** si:

1. En `inv-mkt-seguimiento` solo quitás o desactivás el workflow `ventory-desktop-release.yml` (no afecta Coolify).
2. No volvés a commitear `AppLocal` en `main`.
3. Todo el desarrollo desktop vive en `https://github.com/aeeb-dev/ventory-app`.

La web sigue en `main` → Coolify; el exe en repo público aparte.

## Publicar el código en el repo nuevo (una vez)

Repo vacío en GitHub, público, default `main`.

Desde el árbol actual del escritorio (`inv-mkt-app-local-wt`):

```powershell
cd e:\MIS-CODIGOS\inv-mkt-app-local-wt
git remote add ventory-app https://github.com/aeeb-dev/ventory-app.git
git push ventory-app HEAD:main
```

Si el remoto ya tiene commit inicial (README), puede hacer falta `--force` solo en el primer push del árbol completo (decisión tuya).

## Releases en `ventory-app`

Misma convención:

```powershell
.\scripts\publish-release.ps1 -Version 1.0.0
git push ventory-app main
git push ventory-app ventory-v1.0.0
```

Workflow en `.github/workflows/` del **mismo repo** (default `main`). Sin secreto de token. Releases y descargas son públicas.

## Clientes ya instalados (repo viejo)

Quien tiene `update.json` con `inv-mkt-seguimiento`:

1. **Una vez:** repartir `Ventory.exe` del primer release en `ventory-app`, **o**
2. **Puente (opcional):** un último release en el repo privado con un exe que ya trae `update.json` → `ventory-app` (solo si aún podés publicar ahí).

Después, todas las actualizaciones salen de `ventory-app` sin configuración extra.

## Repo privado `inv-mkt-seguimiento`

- Rama `app-local`: archivar o dejar de usar.
- Eliminar workflow de release desktop en `main` cuando `ventory-app` esté activo.