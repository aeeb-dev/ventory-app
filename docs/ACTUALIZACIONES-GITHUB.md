# Actualizaciones desde GitHub (repo privado)

## En cada PC con VENTORY

1. Crear un **Personal Access Token** (fine-grained o classic) con permiso **Contents: Read** del repo privado.
2. Guardar **una línea** con el token en:

   ```
   %LocalAppData%\Ventory\github.token
   ```

   (Tras la primera ejecución de `Ventory.exe`, esa carpeta ya existe.)

   Alternativa: variable de entorno de usuario `VENTORY_GITHUB_TOKEN`.

3. Opcional: copiar `update.json` a `%LocalAppData%\Ventory\` si el repo u owner no son `aeeb-dev/inv-mkt-seguimiento`.

La app comprueba releases al inicio (aviso en bandeja) y desde **Bandeja → Buscar actualizaciones…**.

## Rama `app-local`

El cliente de escritorio vive en la rama **`app-local`** (solo esta carpeta/proyecto, sin la web Next.js).

```bash
git push -u origin app-local
# Release:
git tag ventory-v1.0.1
git push origin ventory-v1.0.1
```

## Publicar una versión nueva (mantenedor)

1. Subir versión en `ventory-tray/VentoryTray.csproj` (`Version`, `FileVersion`) en rama `app-local`.
2. Commit y tag:

   ```bash
   git tag ventory-v1.0.1
   git push origin ventory-v1.0.1
   ```

3. GitHub Actions construye y adjunta a **Releases**:
   - `Ventory.exe`
   - `Ventory-desktop.zip`

4. Los clientes con `github.token` instalado recibirán la oferta de actualización.

## Seguridad

- No subir `github.token` al repo (está en `.gitignore` vía rutas locales).
- Usar un PAT con mínimo privilegio (solo lectura del repo o solo releases).