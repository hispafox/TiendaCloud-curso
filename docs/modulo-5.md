# Módulo 5 · Cuánto cuesta y quién se entera

**Rama:** `modulo-5/costes` · **Viene de:** `modulo-4/identidad-y-secretos` · **Sigue en:** `modulo-6/un-comando`

## Qué sube a Azure

| Recurso | Nombre | Para qué |
|---|---|---|
| Presupuesto de Cost Management | `presupuesto-tienda` | Un importe mensual sobre el grupo de la tienda, con tres avisos al responsable: cuando el gasto **previsto** del mes pasa del 50, el 90 y el 100 % |

Son las decisiones de la sección 7 del módulo 5: el ámbito (el grupo de la tienda), el periodo (mensual), los umbrales (sobre el gasto previsto, que es el aviso que llega antes) y a quién avisar (`$Responsable`, el mismo correo de la etiqueta `responsable`).

Un presupuesto avisa y no corta nada: si se pasa, la tienda sigue funcionando.

El resto del módulo 5 ya estaba en la tienda desde el principio: las cuatro etiquetas en cada recurso, un Bicep que se puede relanzar sin romper nada y un script que lo borra todo.

## Qué cambia en el repositorio

| Fichero | Qué cambia |
|---|---|
| `azure/main.bicep` | El presupuesto, con su importe, su fecha de inicio (el día 1 del mes en curso) y sus tres avisos |
| `azure/desplegar.ps1` | Pasa el importe al Bicep |
| `azure/deploy.secrets.example.ps1` | Suma `$PresupuestoMensual`, el importe en la moneda de tu suscripción |
| `azure/borrar.ps1` | Borra el presupuesto antes que el grupo: es un objeto de Cost Management, y se quita aparte para que no se quede huérfano |

La aplicación no cambia.

## Cómo se lanza

```powershell
git checkout modulo-5/costes
# Añade $PresupuestoMensual a tu deploy.secrets.ps1, con la cifra de tu estimación
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

Antes de lanzarlo, mira qué va a cambiar sin tocar nada. Es el plan antes de aplicar del módulo 5:

```powershell
az deployment group what-if -g rg-tienda-pruebas --template-file azure/main.bicep -p responsable=tu.nombre@tuempresa.com sqlAdminPassword=<la de tu fichero> presupuestoMensual=<el tuyo>
```

Con la tienda del módulo 4 ya montada, el presupuesto sale como nuevo (**Create**). En el resto de la lista, lo que se mira con cuidado es lo que se borra o se reemplaza.

## Las dos estimaciones, en la calculadora

La estimación no la hace el script: se hace en la **Azure Pricing Calculator**, servicio a servicio. La plataforma de aplicaciones con su plan y su número de instancias, la base de datos, el almacenamiento de las fotos con sus niveles y la salida de datos. Dos estimaciones, la de un mes normal y la del mes de campaña, y apunta qué partidas cambian. La del mes normal es el importe del presupuesto.

## Qué mirar en el Portal

- En el grupo de la tienda, **Cost Management** > **Budgets**: `presupuesto-tienda`, con su importe y sus tres avisos.
- En el grupo, **Cost analysis**: el gasto agrupado por la etiqueta `centro-de-coste`. Los datos de coste tardan horas en aparecer, así que en un grupo recién creado puede salir vacío.

## Cómo sabes que está bien

El script termina con «LISTO» y el presupuesto aparece en **Budgets** del grupo, con sus tres umbrales sobre el gasto previsto y el correo del responsable.

## Lo que todavía no hace

La tienda se ha ido montando por partes, una rama detrás de otra. En el módulo 6 se monta entera desde cero, con una sola orden, y se revisa con la lista de comprobación del pilar.
