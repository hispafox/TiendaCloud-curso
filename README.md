# TiendaCloud-curso

La tienda online del curso de arquitectura cloud, montada en **Azure** paso a paso. Cada módulo del curso sube una parte más de la tienda, y cada parte tiene su rama en este repositorio.

> **Estás en `modulo-1/grupo-de-recursos`.** Todavía no hay aplicación: solo el sitio donde irá la tienda, un grupo de recursos con sus etiquetas, y los dos scripts que la montan y la borran.

## Las ramas del curso

Cada rama añade un paso a la anterior. Para ver una, `git checkout <rama>`; para montarla, el mismo `desplegar.ps1` en todas.

| Módulo | Rama | Qué añade |
|---|---|---|
| 1 | `modulo-1/grupo-de-recursos` | El grupo de recursos con sus etiquetas, y los scripts que montan y borran la tienda |
| 2 | `modulo-2/app-service` | La tienda: una API .NET y un front React, publicados en App Service, con el catálogo en memoria |
| 3 | `modulo-3/datos` | El catálogo y los pedidos en Azure SQL Database, y las fotos en Blob Storage con su ciclo de vida |
| 4 | `modulo-4/identidad-y-secretos` | La identidad de la web, un Key Vault con su mensaje, y las fotos sin clave |
| 5 | `modulo-5/costes` | El presupuesto del grupo, con avisos al responsable |
| 6 | `modulo-6/un-comando` | La tienda entera desde cero con una orden, y la lista de comprobación del pilar |

`main` es la tienda terminada, igual que la rama del módulo 6. Lo que hace cada rama, cómo se lanza y cómo se comprueba está en `docs/`, un fichero por módulo.

## Qué hay en esta rama

| Carpeta | Qué es |
|---|---|
| `azure/desplegar.ps1` | Crea el grupo de recursos con sus cuatro etiquetas |
| `azure/borrar.ps1` | Borra el grupo de recursos entero |
| `azure/deploy.secrets.example.ps1` | La plantilla de valores: suscripción, región, grupo y responsable |
| `docs/modulo-1.md` | Qué hace esta rama, cómo se lanza y cómo se comprueba |
| `docs/arquitectura.md` | Lo que hay montado en Azure en esta rama, con diagramas |

## Lo que necesitas

- La CLI de Azure (`az version`).
- Permisos para crear grupos de recursos en una suscripción de pruebas.

## Montarla

1. Copia `azure/deploy.secrets.example.ps1` como `azure/deploy.secrets.ps1` y rellena tus valores. Ese fichero está en `.gitignore`: no se sube nunca.
2. Desde la raíz del repositorio:

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

## Borrarla

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\borrar.ps1
```
