# Módulo 6 · La tienda entera, con una orden

**Rama:** `modulo-6/un-comando` (igual que `main`) · **Viene de:** `modulo-5/costes` · **Es la última**

## Qué sube a Azure

Todo lo de los módulos 1 a 5, de una vez y **en un grupo vacío**: el grupo con sus etiquetas, el plan y la web con su identidad, la base de datos, el almacenamiento con su ciclo de vida, el Key Vault con su mensaje, los dos permisos de la identidad y el presupuesto. El orden lo calcula el Bicep a partir de lo que cada recurso necesita: la web espera a la base, al almacenamiento y al secreto, y los permisos esperan a la identidad de la web.

## Qué cambia en el repositorio

| Fichero | Qué añade |
|---|---|
| `docs/lista-comprobacion.md` | La lista de comprobación del módulo 6: una pregunta por pilar, con la versión básica de este repositorio y la de producción |
| `README.md` | El de la tienda terminada, con la tabla de ramas |

La aplicación y los scripts no cambian: `desplegar.ps1` ya monta la tienda entera desde cero. El paso de este módulo es hacerlo de una vez, en un grupo que no existe, y revisarlo con la lista.

## Cómo se lanza

Si el grupo de los módulos anteriores sigue montado, bórralo primero, para ver la tienda nacer entera:

```powershell
git checkout modulo-6/un-comando
pwsh -ExecutionPolicy Bypass -File .\azure\borrar.ps1
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

Antes de desplegar, la lista de lo que se va a crear, sin tocar nada (el grupo tiene que existir: créalo con `az group create` o lanza antes el script del módulo 1):

```powershell
az deployment group what-if -g rg-tienda-pruebas --template-file azure/main.bicep -p responsable=tu.nombre@tuempresa.com sqlAdminPassword=<la de tu fichero> presupuestoMensual=<el tuyo>
```

## El recorrido de la demo

1. `azure/main.bicep`, bloque a bloque: cada recurso, su módulo y lo que necesita.
2. `az deployment group what-if`: la lista de lo que se va a crear, antes de crearlo.
3. `desplegar.ps1` en marcha, paso a paso, hasta que escribe la URL de la tienda.
4. El grupo de recursos en el Portal: los recursos creados y sus etiquetas.
5. La tienda: el catálogo con sus fotos, el mensaje del Key Vault y el panel de estado con sus tres filas en verde.
6. Un fallo provocado: en el Key Vault, **Access control (IAM)**, se quita el rol **Key Vault Secrets User** a la web. Al recargar, la fila del Key Vault sale en rojo y el mensaje desaparece, mientras la base y el almacenamiento siguen en verde. Se vuelve a poner con otro `desplegar.ps1`: el Bicep repone lo que falta.
7. `borrar.ps1`: el presupuesto y el grupo desaparecen, y el Key Vault se purga.

El paso 6 es el que más enseña: un permiso que falta es de los fallos más frecuentes al montar una arquitectura, y verlo aislado en una sola fila dice exactamente qué decisión del diseño está fallando. Los permisos tardan unos minutos en aplicarse, así que tanto el rojo como la vuelta al verde pueden pedir un par de recargas.

## Cómo sabes que está bien

El script termina con «LISTO». La tienda enseña el catálogo con sus fotos y el mensaje del Key Vault, el panel de estado tiene sus tres filas en verde y `/salud` responde 200. Después de `borrar.ps1`, el grupo ya no aparece en **Resource groups**.

## Y después, la lista

Con la tienda montada, recorre `docs/lista-comprobacion.md` un pilar cada vez. Cada «no» de la versión básica tiene su razón; lo que tu empresa no pueda justificar igual es su lista de pendientes.
