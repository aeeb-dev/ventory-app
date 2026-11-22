# Token de solo lectura (actualizaciones del .exe)

## Crear el PAT en GitHub

1. Iniciá sesión con la cuenta **organización o bot** (no tu usuario personal con todos los permisos).
2. **Settings** (del usuario/bot) → **Developer settings** → **Fine-grained personal access tokens** → **Generate new token**.
3. Nombre: `ventory-desktop-update-read`.
4. **Resource owner**: `aeeb-dev` (u owner del repo).
5. **Repository access**: **Only select repositories** → `inv-mkt-seguimiento`.
6. **Permissions** (mínimo):
   - **Contents**: Read-only (descarga de assets del release).
   - **Metadata**: Read-only (suele ser obligatorio).
7. Generar, copiar **una vez** (no se vuelve a mostrar).

No uses “classic” con scope `repo` completo si podés evitarlo.

## Secreto en GitHub Actions (build oficial)

El token **no va en el código**. Solo en el repo:

**Settings → Secrets and variables → Actions → New repository secret**

- Nombre: `VENTORY_EMBED_GITHUB_TOKEN`
- Valor: el PAT de arriba

Al publicar un tag `ventory-v*`, CI ejecuta `build-desktop.ps1`, lee ese secreto y lo **incrusta en el `.exe` del release**. El usuario final no necesita `github.token` si instaló el binario de **GitHub Releases**.

## Prioridad al ejecutar la app

1. Variable `VENTORY_GITHUB_TOKEN` (override / soporte).
2. Archivo `%LocalAppData%\Ventory\github.token` (solo si IT lo configura a mano).
3. Token embebido en el `.exe` (releases construidos en CI con el secreto).

La app **no crea** `github.token` en disco para ocultar el secreto; lo usa desde el ejecutable.

## Lo que sí hace el .exe solo

Al primer arranque copia a `%LocalAppData%\Ventory\`:

- `Ventory.exe`, icono, `update.json` (repo GitHub), etc.

Con eso + token embebido (build de release), basta **enviar solo el `.exe`** del release.

## Seguridad (importante)

| Mitos | Realidad |
|-------|----------|
| “Hashear el token en el .exe” | No sirve: GitHub necesita el PAT en claro en cada petición. |
| “El usuario no lo ve” | No verá un archivo, pero quien analice el `.exe` puede extraer metadata. |
| “Impenetrable” | Cualquier credencial **dentro del cliente** es recuperable. |

Tratá el PAT como **clave de lectura de binarios**, no como secreto de servidor:

- Token dedicado, solo ese repo, solo lectura.
- Rotarlo si filtra o repartís muchas copias del .exe.
- Equipos grandes: valorá publicar el `.exe` en **tu dominio** (`ventory.bferal.cl`) sin PAT en el cliente (evolución futura).

## Build local sin embeber token

```powershell
.\build-desktop.ps1
```

Para probar actualizaciones en dev, usá `github.token` local o `VENTORY_GITHUB_TOKEN`.

```powershell
$env:VENTORY_EMBED_GITHUB_TOKEN = "ghp_..."
.\build-desktop.ps1 -ProductVersion 1.0.0
```

**No commitees** el PAT ni construyas con token embebido para compartir por WhatsApp; solo releases de CI.