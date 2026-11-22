# VENTORY — escritorio Windows

**Rama `app-local` únicamente** — separada de la app web (`main`). No mezclar con Next.js.

Repo: `aeeb-dev/inv-mkt-seguimiento` → branch **`app-local`**.

## Qué compartir

| Archivo | Uso |
|---------|-----|
| **`Ventory.exe`** (tras `.\build-desktop.ps1`) | Un solo `.exe` para enviar |
| **`Ventory/`** | Carpeta completa (arranque más rápido) |

## Build

```powershell
.\build-desktop.ps1
.\launch.ps1
```

## Instalación en el PC del usuario

La primera vez que abren el `.exe`, se instala en `%LocalAppData%\Ventory\`.

## Actualizaciones GitHub

[docs/ACTUALIZACIONES-GITHUB.md](docs/ACTUALIZACIONES-GITHUB.md)

## Sincronizar desde un monorepo local (opcional)

Si tenés una copia con carpeta `AppLocal/` en `main` (ignorada), actualizá la rama con:

```bash
git subtree push --prefix=AppLocal origin app-local
```

Desarrollo diario recomendado: clonar y usar **`git checkout app-local`**.