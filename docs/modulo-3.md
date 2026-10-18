# Módulo 3 · Cada dato en su sitio

**Rama:** `modulo-3/datos` · **Viene de:** `modulo-2/app-service` · **Sigue en:** `modulo-4/identidad-y-secretos`

## Qué sube a Azure

Dentro del mismo grupo, junto a la web del módulo 2:

| Recurso | Nombre | Para qué |
|---|---|---|
| Servidor de SQL | `sql-tienda-<sufijo>` | El punto de conexión, con su usuario administrador. Su cortafuegos solo deja entrar a los servicios de Azure (`AllowAzureServices`) |
| Base de datos | `pedidos` | El catálogo y los pedidos. Nivel Basic |
| Cuenta de almacenamiento | `sttienda<sufijo>` | Las fotos de los productos. StorageV2, redundancia local, sin acceso anónimo |
| Contenedor | `fotos` | Privado: la web sirve las fotos, nadie llega a ellas desde Internet |
| Regla de ciclo de vida | `enfriar-fotos` | Las fotos que nadie cambia pasan a Cool a los 30 días y a Cold a los 90 |

Y la web recibe en su configuración dos cadenas de conexión: la de la base, con usuario y contraseña, y la del almacenamiento, con la clave de la cuenta. Las dos las escribe el Bicep: nadie copia una dirección a mano.

Son las decisiones del módulo 3. Los pedidos van a una relacional porque tienen que cuadrar, y las fotos a un almacenamiento de objetos con su ciclo de vida. Con redundancia local, la regla puede bajar hasta Archive; la tienda se queda en Cold, como pedía el módulo.

## Qué cambia en el repositorio

| Fichero | Qué añade |
|---|---|
| `src/Tienda.Api/CatalogoSql.cs` | El catálogo en la base, con EF Core. La primera vez crea las tablas y carga los seis productos. Pedir resta las existencias y guarda el pedido en la misma transacción: o pasan las dos cosas o ninguna |
| `src/Tienda.Api/Fotos.cs` | Las fotos en Blob Storage. La web sube las de ejemplo la primera vez y las sirve en `/api/productos/{id}/foto` |
| `src/Tienda.Api/Fotos/*.svg` | Las seis fotos de ejemplo, dentro de la propia web |
| `src/Tienda.Api/Comprobaciones.cs` | Las comprobaciones de la base y del almacenamiento, y `/salud` |
| `frontend` | La foto en cada producto y el panel «Estado de la tienda», con una fila por pieza |
| `tests/Tienda.Api.Tests` | 30 tests: se suman el catálogo en SQLite en memoria, las comprobaciones, `/salud` y las fotos |
| `azure/main.bicep` | Servidor y base de SQL, cuenta de almacenamiento, contenedor y ciclo de vida, y las dos cadenas en la web |
| `azure/desplegar.ps1` | Pide la contraseña de la base y, al final, espera a `/salud` |
| `azure/deploy.secrets.example.ps1` | Suma `$SqlPassword` |

Si la web no tiene cadena de conexión, como pasa en local, sigue con el catálogo en memoria del módulo 2 y con las fotos que lleva dentro.

## Cómo se lanza

```powershell
git checkout modulo-3/datos
# Añade $SqlPassword a tu deploy.secrets.ps1
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

Sobre lo que ya hay, el Bicep añade la base y el almacenamiento, y cambia la configuración de la web. Lo demás lo deja como está.

## Qué mirar en el Portal

- En la base `pedidos`: **Overview** (el nivel y el servidor) y **Query editor**, donde se ven las tablas `Productos` y `Pedidos` con sus filas. Para entrar, el editor te pedirá añadir tu IP al cortafuegos del servidor.
- En el servidor de SQL, **Networking**: la excepción para los servicios de Azure.
- En la cuenta de almacenamiento: **Redundancy** (local), **Containers** > `fotos` (las seis fotos, después de abrir la tienda) y **Lifecycle management** (la regla `enfriar-fotos`).
- En la web, **Environment variables** > **App settings**: las dos cadenas. La contraseña y la clave están ahí, a la vista de quien pueda leer la configuración.

## Cómo sabes que está bien

El script termina con «LISTO». En la tienda, cada producto sale con su foto, y al pedir bajan las existencias. Si reinicias la web desde el Portal, los pedidos siguen ahí, porque ya no viven en su memoria. El panel de estado tiene dos filas en verde: la base de datos y el almacenamiento.

## Lo que todavía no hace

La configuración de la web guarda dos secretos: la contraseña de la base y la clave del almacenamiento. Quien la lea, entra. En el módulo 4 la web recibe su propia identidad, la clave del almacenamiento desaparece y llega un Key Vault.
