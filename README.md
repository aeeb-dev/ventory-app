# VENTORY — escritorio Windows

## Qué compartir

| Archivo | Uso |
|---------|-----|
| **`AppLocal/Ventory.exe`** | Un solo `.exe` para enviar; en el **primer uso** se instala solo en `%LocalAppData%\Ventory\` |
| **`AppLocal/Ventory/`** | Carpeta completa (arranque más rápido si copias todo) |

## Instalación automática

La primera vez que alguien abre el `.exe` (desde Descargas, Escritorio, etc.):

1. Crea la carpeta **`%LocalAppData%\Ventory\`** (p. ej. `C:\Users\tu_usuario\AppData\Local\Ventory\`)
2. Copia ahí el programa y lo que necesita
3. Vuelve a abrir desde esa carpeta (como otros programas de usuario)

Inicio con Windows y datos del navegador usan esa ruta instalada.

## Desarrollo / regenerar

```powershell
AppLocal\build-desktop.ps1
```

## Uso

```powershell
AppLocal\launch.ps1
```

- Cierras la ventana → sigue en bandeja  
- **Salir** solo en menú de bandeja (clic derecho)  
- Primera ejecución: **Iniciar con Windows** activado (en bandeja)

## Actualizaciones (repo privado GitHub)

Ver [docs/ACTUALIZACIONES-GITHUB.md](docs/ACTUALIZACIONES-GITHUB.md): token en `%LocalAppData%\Ventory\github.token`, releases con tag `ventory-v*`.