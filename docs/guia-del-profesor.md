# Guía del profesor: qué script lanzar en cada momento

Esta guía es para quien da el curso en directo. Dice qué rama sacar y qué script lanzar en cada módulo, qué enseñar en pantalla mientras tanto y qué hacer si algo se tuerce. La teoría de cada módulo está en sus bases y sus decks; aquí solo va la mecánica.

## Antes de la primera clase, una sola vez

1. Clona este repositorio.
2. Copia `azure\deploy.secrets.example.ps1` **de la rama `main`** como `azure\deploy.secrets.ps1` y rellena sus seis valores: suscripción, región, grupo, responsable, contraseña de la base e importe del presupuesto. Es la única plantilla que los trae todos. El fichero está en `.gitignore`: no cambia al pasar de rama y no se sube nunca.
3. Comprueba que tienes la CLI de Azure (`az version`), el SDK de .NET 10 (`dotnet --version`) y Node.js (`node --version`), y que tu cuenta puede crear recursos y asignar roles en la suscripción. El rol Owner sobre una suscripción de pruebas da las dos cosas.
4. **Pruébalo la víspera**: lanza las ramas 1, 2 y 3 y después `borrar.ps1`. Nada de esto se ha lanzado antes contra Azure, y es mejor descubrir un fallo la noche anterior que delante de la clase.

Todos los scripts se lanzan desde la raíz del repositorio, así:

```powershell
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

## Cómo funcionan las ramas

- **Son acumulativas.** La rama de cada módulo lleva todo lo de los anteriores, y su `desplegar.ps1` crea el grupo si no existe. Por eso cualquier rama se monta desde cero.
- **Hacia delante, no hace falta borrar.** Si vienes del módulo 2 y sacas la rama del 3, su `desplegar.ps1` añade lo nuevo y cambia lo que ha cambiado; los pedidos y las fotos se conservan.
- **Hacia atrás, o para empezar de cero, se borra antes.** Y siempre con el `borrar.ps1` de la rama que esté montada, o con el de `main`. El de las ramas 1 a 3 no purga el Key Vault, y el de las ramas 1 a 4 no borra el presupuesto.
- **Al terminar cada sesión, `borrar.ps1`.** Así no se queda nada montado hasta la clase siguiente.

## Lunes 19: módulos 1 a 3

| Módulo | Qué lanzas | Qué enseñas |
|---|---|---|
| 1 | `git checkout modulo-1/grupo-de-recursos` y `desplegar.ps1` | En el Portal, **Resource groups**: el grupo, su suscripción, su región (**Location**) y sus cuatro etiquetas |
| 2 | `git checkout modulo-2/app-service` y `desplegar.ps1` | La URL que escribe el script: la tienda con su catálogo en memoria. En el Portal, la web y su plan **Free (F1)**, sin slots |
| 3 | `git checkout modulo-3/datos` y `desplegar.ps1` | La tienda con fotos, y los pedidos ya se guardan en la base. En el Portal, la base (oferta gratuita), la cuenta de almacenamiento con su contenedor `fotos` y su regla de ciclo de vida, y en la web las dos cadenas de conexión con contraseña y clave |
| Al acabar | `borrar.ps1`, con la rama 3 | El grupo desaparece |

## Lunes 26: módulos 4 a 6

| Módulo | Qué lanzas | Qué enseñas |
|---|---|---|
| 4 | `git checkout modulo-4/identidad-y-secretos` y `desplegar.ps1`, que monta del 1 al 4 en un grupo vacío | El mensaje del Key Vault en la tienda y el panel de estado con sus tres filas en verde. En el Portal, la identidad de la web, sus dos permisos y la configuración ya sin la clave del almacenamiento. En el Key Vault, **Objects** > **Secrets**: ni siendo Owner puedes ver el secreto, y esa es la lección |
| 5 | `git checkout modulo-5/costes`, `ver-cambios.ps1` y después `desplegar.ps1` | El what-if: solo se crea el presupuesto. Después, en el Portal, **Cost Management** > **Budgets** del grupo. Las dos estimaciones se hacen aparte, en la calculadora |
| 6 | `git checkout modulo-6/un-comando`, `borrar.ps1`, `ver-cambios.ps1` y `desplegar.ps1` | La tienda entera desde cero, con una orden: en el what-if todo sale como nuevo |
| 6, el fallo | `provocar-fallo.ps1`, y al cabo de unos minutos, recargar la tienda | La fila del Key Vault en rojo y el mensaje desaparece, mientras la base y el almacenamiento siguen en verde. Otro `desplegar.ps1` lo devuelve |
| Al acabar | `borrar.ps1`, con la rama 6 | Desaparecen el presupuesto y el grupo, y se purga el Key Vault |

## Los laboratorios del alumno

Están en `labs/ARQCLOUD-<n>-lab.md` del curso, uno por módulo. Siguen la misma secuencia con una diferencia: **cada lab empieza de cero y termina con `borrar.ps1`**. El alumno no encadena ramas. Quien no tenga suscripción sigue la demo en pantalla.

## Las esperas, para no ponerse nervioso

- El primer `desplegar.ps1` de las ramas 3 y 4 tarda unos minutos, porque crea la base de datos.
- El plan gratuito duerme la web si nadie la usa: la primera visita después de un rato tarda unos segundos.
- La base gratuita se pausa sola: la primera página que la toca tarda alrededor de un minuto en responder, y la aplicación reintenta mientras tanto.
- Los permisos tardan unos minutos en aplicarse. Si justo después de desplegar la fila del Key Vault sale en rojo, espera y recarga.

## Si algo falla

Los mensajes son los que escribe el propio script:

| Mensaje | Qué ha pasado | Qué hacer |
|---|---|---|
| «Falta Azure CLI (az)» | No está instalada | Instalarla y abrir otra terminal |
| «Falta Node.js (npm)» | No está instalado | Instalarlo y abrir otra terminal |
| «Falta azure\deploy.secrets.ps1» | No se ha copiado la plantilla | Copiar la de `main` con ese nombre y rellenarla |
| «deploy.secrets.ps1 no define $…» | Falta un valor | Añadir el que dice el mensaje |
| «Fallo en: az deployment group create» | Azure ha rechazado el Bicep | Leer el error de Azure que sale justo encima |
| «Desplegado, pero /salud aún no responde bien» | Una pieza falla | Abrir la URL y mirar qué fila del panel de estado está en rojo |

Si el despliegue choca con un Key Vault que ya existe en estado borrado, es que el grupo se borró con el `borrar.ps1` de una rama anterior a la 4, y el Key Vault se quedó en la papelera con su nombre reservado. El `borrar.ps1` de `main` ya no lo encuentra, porque lo busca dentro del grupo. Es la única orden que hay que teclear a mano en todo el curso: púrgalo con `az keyvault purge --name <el nombre que da el error>` y vuelve a desplegar.

## Lo que cuesta

La tienda va al nivel gratuito donde lo hay: App Service Free (F1) y la oferta gratuita de Azure SQL Database. El almacenamiento y el Key Vault no tienen nivel gratuito; están en su nivel más barato y se pagan por uso, que aquí es mínimo. Aun así, borra al acabar cada sesión.
