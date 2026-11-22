# Actualizaciones automáticas (GitHub privado)

## Cadena completa

1. **Mantenedor** en rama `app-local` → tag `ventory-v1.0.1` → push del tag.
2. **GitHub Actions** (`ventory-desktop-release.yml`) valida `build-desktop.ps1`, compila con esa versión y publica **Release** con `Ventory.exe`.
3. **Cliente** con `github.token` consulta `releases/latest`, compara versión y ofrece descargar `Ventory.exe`.

Si el tag se crea sobre `main`, el workflow **falla** (no existe `build-desktop.ps1` en la raíz).

## Publicar versión (mantenedor)

```powershell
git checkout app-local
git pull origin app-local
.\scripts\publish-release.ps1 -Version 1.0.1
git push origin app-local
git push origin ventory-v1.0.1
```

Solo el **push del tag** dispara el build en CI. El nombre debe ser `ventory-vMAJOR.MINOR.PATCH`.

## En cada PC con VENTORY

1. PAT de GitHub con **Contents: Read** en el repo privado.
2. Archivo de una línea:

   `%LocalAppData%\Ventory\github.token`

   Alternativa: variable `VENTORY_GITHUB_TOKEN`.

3. `update.json` por defecto apunta a `aeeb-dev/inv-mkt-seguimiento` (se copia al instalar).

La app avisa en bandeja o con **Buscar actualizaciones…**.

## Primera release

Sin ningún Release en GitHub, el cliente muestra “No hay releases publicados”. El primer `git push origin ventory-v1.0.0` lo resuelve.

## Comprobar versión instalada

Propiedades de `Ventory.exe` → Detalles → **Versión de archivo**. Debe ser menor que la del último tag para que ofrezca actualizar.