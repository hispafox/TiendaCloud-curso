# TiendaCloud-curso

La tienda online del curso de arquitectura cloud, montada en **Azure** paso a paso. Cada módulo del curso sube una parte más de la tienda, y cada parte tiene su rama en este repositorio.

> **Estás en `modulo-3/datos`.** El catálogo y los pedidos ya viven en Azure SQL Database, y las fotos de los productos en Blob Storage. La web entra a los dos con contraseña y con clave: el módulo 4 lo cambia.

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
| `azure/main.bicep` | La infraestructura: plan y web de App Service, base de datos SQL, almacenamiento con su contenedor de fotos y su ciclo de vida, y etiquetas en todo |
| `azure/desplegar.ps1` | Crea el grupo, despliega el Bicep, compila el front, publica la API con el front dentro y espera a que `/salud` responda |
| `azure/borrar.ps1` | Borra el grupo de recursos entero |
| `azure/deploy.secrets.example.ps1` | La plantilla de valores: suscripción, región, grupo, responsable y contraseña de la base |
| `src/Tienda.Api` | La API .NET 10: catálogo, pedidos, fotos y `/salud`. En Azure sirve también el front |
| `frontend` | El front: React, Vite y TypeScript |
| `tests/Tienda.Api.Tests` | Los tests de la API (xUnit) |
| `docs/` | Un fichero por módulo: qué hace cada rama y cómo se comprueba |
| `docs/arquitectura.md` | Lo que hay montado en Azure en esta rama, con diagramas |

## Lo que necesitas

- La CLI de Azure (`az version`).
- El SDK de .NET 10 (`dotnet --version`).
- Node.js con npm (`node --version`).
- Permisos para crear recursos en una suscripción de pruebas.

## Montarla

1. Copia `azure/deploy.secrets.example.ps1` como `azure/deploy.secrets.ps1` y rellena tus valores. Ese fichero está en `.gitignore`: no se sube nunca.
2. Desde la raíz del repositorio:

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

Al terminar escribe la URL de la tienda. `/salud` devuelve en JSON el estado de cada pieza, con un 200 si todo va bien y un 503 si algo falla, y el front lo enseña en su panel de estado.

## Probarla en local

La API en `https://localhost:5001` (certificado de desarrollo de .NET) y el front en `http://localhost:5173`, que pasa `/api` y `/salud` a la API:

```powershell
dotnet run --project src/Tienda.Api
```

```powershell
cd frontend
npm install
npm run dev
```

En local, sin Azure, la API usa el catálogo en memoria y las fotos que lleva dentro, y el panel de estado dice qué falta por configurar. Los tests: `dotnet test` en la raíz y `npm test` en `frontend`.

## Lo que cuesta

El plan de App Service es el gratuito (F1) y la base usa la oferta gratuita de Azure SQL Database, que se pausa sola cuando nadie la usa. El almacenamiento no tiene plan gratuito: se pagan por uso, en su nivel más barato, y con lo que mueve la tienda el uso es mínimo. Aun así, bórrala con `borrar.ps1` al acabar cada sesión.

## Borrarla

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\borrar.ps1
```
