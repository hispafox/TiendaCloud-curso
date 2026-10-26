# TiendaCloud-curso

La tienda online del curso de arquitectura cloud, montada en **Azure** paso a paso. Cada módulo del curso sube una parte más de la tienda, y cada parte tiene su rama en este repositorio.

> **Estás en la tienda terminada.** `main` y `modulo-6/un-comando` tienen la misma tienda: entera, se monta desde cero con una orden y se borra con otra. Este README, además, explica cómo pasar de una rama a otra.

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

Lo que hace cada rama, cómo se lanza, qué mirar en el Portal y cómo se comprueba está en `docs/`, un fichero por módulo. La lista de comprobación, en `docs/lista-comprobacion.md`. Y la secuencia de scripts para dar el curso, clase a clase, en `docs/guia-del-profesor.md`.

## Qué hay dentro

| Carpeta | Qué es |
|---|---|
| `azure/main.bicep` | La infraestructura entera: plan y web de App Service con su identidad, base de datos SQL, almacenamiento con su contenedor de fotos y su ciclo de vida, Key Vault con un mensaje, los dos permisos de la identidad, el presupuesto del grupo y etiquetas en todo |
| `azure/desplegar.ps1` | Monta la tienda: crea el grupo, despliega el Bicep, compila el front, publica la API con el front dentro y espera a que `/salud` responda |
| `azure/borrar.ps1` | Borra el presupuesto y el grupo de recursos entero, y purga el Key Vault, para que no siga costando dinero |
| `azure/ver-cambios.ps1` | Enseña qué cambiaría el despliegue (el what-if), sin desplegar nada |
| `azure/provocar-fallo.ps1` | Quita a la web su permiso sobre el Key Vault, para el fallo provocado de la demo del módulo 6 |
| `azure/deploy.secrets.example.ps1` | La plantilla de valores: suscripción, región, grupo, responsable, contraseña de la base e importe del presupuesto |
| `src/Tienda.Api` | La API .NET 10: catálogo, pedidos, fotos, el mensaje del Key Vault y `/salud`. En Azure sirve también el front |
| `frontend` | El front: React, Vite y TypeScript |
| `tests/Tienda.Api.Tests` | Los tests de la API (xUnit) |
| `docs/` | Un fichero por módulo y la lista de comprobación |
| `docs/arquitectura.md` | Lo que hay montado en Azure en esta rama, con diagramas |

## Lo que necesitas

- La CLI de Azure (`az version`).
- El SDK de .NET 10 (`dotnet --version`).
- Node.js con npm (`node --version`).
- Permisos para crear recursos y asignar roles en una suscripción de pruebas (el rol Owner los da los dos).

No hacen falta contenedores ni ninguna otra herramienta.

## Montarla

1. Copia `azure/deploy.secrets.example.ps1` como `azure/deploy.secrets.ps1` y rellena tus valores. Ese fichero está en `.gitignore`: no se sube nunca.
2. Desde la raíz del repositorio:

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

Al terminar escribe la URL de la tienda. `/salud` devuelve en JSON el estado de cada pieza, con un 200 si todo va bien y un 503 si algo falla, y el front lo enseña en su panel de estado.

Antes de desplegar puedes ver qué se va a crear, sin desplegar nada:

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\ver-cambios.ps1
```

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

El plan de App Service es el gratuito (F1) y la base usa la oferta gratuita de Azure SQL Database, que se pausa sola cuando nadie la usa. El almacenamiento y el Key Vault no tienen plan gratuito: se pagan por uso, en su nivel más barato, y con lo que mueve la tienda el uso es mínimo. Aun así, bórrala con `borrar.ps1` al acabar cada sesión.

## Borrarla

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\borrar.ps1
```

## Pasar de una rama a otra

Cada rama monta su etapa desde un grupo vacío, así que puedes empezar por la que quieras. Dos cosas para que salga bien:

1. **Rellena `azure/deploy.secrets.ps1` una sola vez, con los seis valores de la plantilla de `main`.** Ese fichero está en `.gitignore`, así que no cambia al hacer `git checkout`, y las ramas que no usan un valor lo ignoran. La plantilla de cada rama solo trae los valores que usa esa rama.
2. **Borra con el `borrar.ps1` de la rama que desplegaste, o con el de `main`.** El de las ramas 1 a 3 no purga el Key Vault, y el de las ramas 1 a 4 no borra el presupuesto. Si montas la 4 y borras con la 3, el Key Vault se queda en la papelera con su nombre reservado, y el siguiente despliegue choca con él. El de `main` lo cubre todo.

Por ejemplo, para empezar por la rama 2 con otra ya montada: `borrar.ps1` desde la rama montada o desde `main`, después `git checkout modulo-2/app-service` y `desplegar.ps1`.

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\borrar.ps1
git checkout modulo-2/app-service
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

## Un aviso

Es una versión pensada para enseñarla en clase, no para producción. La web entra a la base con usuario y contraseña, y no hay red privada, borde con cortafuegos, escalado ni cola. Lo que añadiría la versión de producción, y por qué, se explica en el módulo 6 del curso y está resumido en `docs/lista-comprobacion.md`.
