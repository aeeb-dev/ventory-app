# Guía de publicación (mantenedores)

Documento para quien sube versiones de **VENTORY Desktop**. El repositorio es público: lo que commitees, taggees y escribas en el **Release** lo ve cualquiera.

## Cuándo hay release

| Acción | ¿Genera Release con `.exe`? |
|--------|----------------------------|
| `git push` a `main` sin tag | No |
| `git push` del tag **`VENTORY - vX.Y.Z`** | Sí (GitHub Actions) |

## Formato de tag (obligatorio)

```
VENTORY - v1.0.0
```

- Texto exacto: `VENTORY - v` + versión **MAJOR.MINOR.PATCH**.
- Espacios como en el ejemplo (antes y después del guión).
- Ejemplos válidos: `VENTORY - v1.0.1`, `VENTORY - v1.1.0`.

Ejemplos inválidos: `ventory-v1.0.0`, `v1.0.0`, `VENTORY-v1.0.0`.

### Publicar desde tu máquina

```powershell
cd e:\MIS-CODIGOS\inv-mkt-app-local-wt
.\scripts\publish-release.ps1 -Version 1.0.1
git push ventory-app HEAD:main
git push ventory-app "VENTORY - v1.0.1"
```

Usá comillas al pushear el tag por los espacios.

## Qué subir a `main` (commits)

- Cambios de producto: bandeja, actualizaciones, WebView, instalación, rendimiento.
- Mensajes de commit claros para el equipo; sin rutas internas, nombres de servidores ni detalles de infraestructura.

## Notas del Release (texto público)

Al crear o editar el release en GitHub, escribí para **quien usa la app**, no para desarrolladores:

**Sí incluir**

- Correcciones visibles (“Se arregló el aviso al cerrar la ventana”).
- Mejoras de uso (“Notificaciones más claras en la bandeja”).
- Requisitos si cambian (p. ej. Windows actualizado).

**No incluir**

- Tokens, PAT, repos privados, Coolify, CI, nombres de ramas internas.
- Explicaciones de vulnerabilidades, vectores de ataque o “parches de seguridad” técnicos.
- Arquitectura interna, rutas de disco de desarrollo, scripts de mantenimiento.

Si el cambio es solo de seguridad, un mensaje genérico basta: *“Actualización recomendada con mejoras de estabilidad.”*

## Versión semántica (orientación)

| Tipo | Cuándo | Ejemplo |
|------|--------|---------|
| **PATCH** | Corrección sin función nueva | `… v1.0.1` |
| **MINOR** | Función nueva para el usuario | `… v1.1.0` |
| **MAJOR** | Cambio grande de comportamiento | `… v2.0.0` |

La versión del `Ventory.exe` (propiedades → versión de archivo) debe coincidir con el tag.

## Checklist antes del push del tag

- [ ] Probaste el `.exe` local (`.\build-desktop.ps1`).
- [ ] El tag sigue el formato `VENTORY - vX.Y.Z`.
- [ ] Las notas del release hablan al usuario final, sin detalle interno.
- [ ] No hay secretos ni archivos `github.token` en el commit.

## Assets del Release

El workflow sube **solo `Ventory.exe`**. GitHub puede mostrar además *Source code (zip/tar.gz)*: es un snapshot automático del código público del tag, no lo generamos nosotros ni incluye secretos si el repo está limpio.

## Referencia

- Usuarios y descargas: [README](../README.md) y [Releases](https://github.com/aeeb-dev/ventory-app/releases).
- Web oficial: [ventory.bferal.cl](https://ventory.bferal.cl).