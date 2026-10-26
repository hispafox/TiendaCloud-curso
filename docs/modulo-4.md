# Módulo 4 · La web entra con su identidad

**Rama:** `modulo-4/identidad-y-secretos` · **Viene de:** `modulo-3/datos` · **Sigue en:** `modulo-5/costes`

## Qué sube a Azure

| Recurso | Nombre | Para qué |
|---|---|---|
| Identidad administrada de la web | (la de `app-tienda-<sufijo>`) | La llave de la web: Azure la crea con el recurso, sin contraseña que guardar ni rotar |
| Key Vault | `kv-tienda-<sufijo>` | El almacén de secretos, con permisos por roles de Azure (RBAC) |
| Secreto | `mensaje` | Un único secreto inofensivo: un mensaje que la web lee y enseña |
| Permiso **Storage Blob Data Contributor** | Sobre la cuenta de almacenamiento | Leer las fotos y subir las de ejemplo |
| Permiso **Key Vault Secrets User** | Sobre el Key Vault | Leer el mensaje |

Cada permiso va sobre un solo recurso, no sobre el grupo: el mínimo privilegio del módulo 4, escrito en el Bicep.

Y en la configuración de la web, **la clave del almacenamiento desaparece**. En su lugar van dos direcciones, la del almacenamiento y la del Key Vault, que no dan acceso a nada por sí solas.

## Qué cambia en el repositorio

| Fichero | Qué cambia |
|---|---|
| `src/Tienda.Api/Program.cs` | Las fotos se leen con `DefaultAzureCredential`, que en Azure es la identidad de la web. Nuevo `/api/mensaje` |
| `src/Tienda.Api/Comprobaciones.cs` | La comprobación del almacenamiento entra con la identidad, y llega la del Key Vault |
| `frontend` | El mensaje del Key Vault, debajo del título. El panel de estado tiene una fila más |
| `tests/Tienda.Api.Tests` | 35 tests: se suman el Key Vault sin configurar y `/api/mensaje` |
| `azure/main.bicep` | La identidad de la web, el Key Vault con su secreto y los dos permisos. Fuera `ConnectionStrings__Fotos` |
| `azure/borrar.ps1` | Purga el Key Vault después de borrar el grupo. Si no, se queda unos días en la papelera con su nombre reservado y el siguiente despliegue choca con él |

## Lo que se queda como estaba, y por qué

La web sigue entrando a la base de datos con usuario y contraseña. Para que entrara con su identidad haría falta un administrador de Microsoft Entra en el servidor de SQL y una orden dentro de la base (`CREATE USER … FROM EXTERNAL PROVIDER`), y eso el Bicep no lo puede declarar. Es el único secreto que queda en la configuración, y la versión de producción lo quita.

Tampoco hay red privada: la base y el almacenamiento siguen teniendo dirección pública. La tienda de producción los sacaría de Internet con una red virtual y endpoints privados, que aquí se explican sin montarlos.

## Cómo se lanza

```powershell
git checkout modulo-4/identidad-y-secretos
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

Hace falta poder asignar roles en la suscripción: el rol Owner sobre una suscripción de pruebas lo da.

## Qué mirar en el Portal

- En la web, **Identity**: la identidad asignada por el sistema, en **On**.
- En la web, **Environment variables**: ya no está la cadena del almacenamiento. Quedan dos direcciones y la cadena de la base.
- En el Key Vault, **Access control (IAM)** > **Role assignments**: la web, con **Key Vault Secrets User**. Si abres **Objects** > **Secrets**, el Portal te dirá que no tienes permiso para ver el contenido, aunque seas Owner: con permisos por roles, gestionar el Key Vault no da acceso a sus secretos. Solo la web, con su rol, lee el mensaje, y lo ves en la tienda.
- En la cuenta de almacenamiento, **Access control (IAM)**: la web, con **Storage Blob Data Contributor**.

## Cómo sabes que está bien

El script termina con «LISTO». Debajo del título de la tienda aparece «Mensaje del Key Vault: «Hola desde el Key Vault de la tienda»», y los productos siguen con sus fotos. El panel de estado tiene sus tres filas en verde, y `/salud` responde 200.

Si la fila del Key Vault sale en rojo justo después de desplegar, espera un rato y recarga: los permisos tardan unos minutos en aplicarse.

## La revisión con el pilar

Con la tienda ya montada, recorre las preguntas del pilar de seguridad sobre lo que existe: qué secretos quedan en la configuración (uno, la contraseña de la base), qué tiene dirección pública (la web, la base y el almacenamiento) y si cada identidad tiene solo lo que necesita (sí: dos permisos sobre dos recursos). Lo que quede sin resolver es tu lista de pendientes.

## Lo que todavía no hace

Nadie avisa de lo que cuesta. En el módulo 5 el grupo recibe su presupuesto, con avisos al responsable.
