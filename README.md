# TiendaCloud-curso

La tienda online del curso de arquitectura cloud, montada en **Azure** en su versión básica: una web .NET en App Service, una base de pedidos en Azure SQL Database, las fotos en Blob Storage y un Key Vault con un único mensaje que la web lee con su propia identidad.

Todo se crea con una orden y se borra con otra.

## Qué hay dentro

| Carpeta | Qué es |
|---|---|
| `azure/main.bicep` | La infraestructura entera: plan y web de App Service, SQL, almacenamiento, Key Vault, permisos de la identidad de la web y etiquetas en todo |
| `azure/desplegar.ps1` | Monta la tienda: crea el grupo de recursos, despliega el Bicep, publica la aplicación y comprueba que responde |
| `azure/borrar.ps1` | Borra el grupo de recursos entero y purga el Key Vault, para que no siga costando dinero |
| `azure/deploy.secrets.example.ps1` | La plantilla de valores (suscripción, región, grupo, responsable y contraseña de la base) |
| `src/Tienda.Web` | La aplicación: una página que comprueba el Key Vault, el almacenamiento y la base de datos |
| `tests/Tienda.Web.Tests` | Los tests (xUnit) |

## Lo que necesitas

- La CLI de Azure (`az version`).
- El SDK de .NET 10 (`dotnet --version`).
- Permisos para crear recursos y asignar roles en una suscripción de pruebas.

No hacen falta contenedores ni ninguna otra herramienta.

## Montarla

1. Copia `azure/deploy.secrets.example.ps1` como `azure/deploy.secrets.ps1` y rellena tus valores. Ese fichero está en `.gitignore`: no se sube nunca.
2. Desde la raíz del repositorio:

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

Al terminar escribe la URL de la tienda. La página enseña las tres comprobaciones; `/salud` devuelve lo mismo en JSON, con un 200 si todo va bien y un 503 si algo falla.

Antes de desplegar puedes ver qué se va a crear, sin tocar nada:

```powershell
az deployment group what-if -g rg-tienda-pruebas --template-file azure/main.bicep -p responsable=tu.nombre@tuempresa.com sqlAdminPassword=<la de tu fichero>
```

## Borrarla

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\borrar.ps1
```

## Un aviso

Es una versión pensada para enseñarla en clase, no para producción. La web entra a la base con usuario y contraseña, y no hay red privada, borde con cortafuegos ni escalado. Lo que añadiría la versión de producción, y por qué, se explica en el módulo 6 del curso.
