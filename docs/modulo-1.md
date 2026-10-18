# Módulo 1 · El sitio de la tienda

**Rama:** `modulo-1/grupo-de-recursos` · **Viene de:** nada, es la primera · **Sigue en:** `modulo-2/app-service`

## Qué sube a Azure

Un grupo de recursos, `rg-tienda-pruebas` por defecto, en la región que diga `deploy.secrets.ps1` (`westeurope` por defecto). Lleva las cuatro etiquetas de la tienda: `aplicacion`, `entorno`, `responsable` y `centro-de-coste`.

Son las tres decisiones del módulo 1 en un solo formulario: en qué suscripción entra la tienda, cómo se llama su grupo y en qué región va.

## Qué hay en el repositorio

| Fichero | Qué hace |
|---|---|
| `azure/desplegar.ps1` | Comprueba que está la CLI de Azure, lee los valores, inicia sesión, elige la suscripción y crea el grupo con sus etiquetas |
| `azure/borrar.ps1` | Borra el grupo entero. Si no existe, lo dice y termina sin error |
| `azure/deploy.secrets.example.ps1` | La plantilla con los cuatro valores: `$Suscripcion`, `$Ubicacion`, `$GrupoRecursos` y `$Responsable` |

Todavía no hay aplicación.

## Cómo se lanza

```powershell
Copy-Item .\azure\deploy.secrets.example.ps1 .\azure\deploy.secrets.ps1
# Rellena deploy.secrets.ps1 con tu suscripción y tu correo
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

El script puede lanzarse las veces que haga falta: si el grupo ya existe, lo deja como está y actualiza sus etiquetas.

## Qué mirar en el Portal

En **Resource groups**, el grupo de la tienda:

- **Overview**: la suscripción y la región.
- **Tags**: las cuatro etiquetas.

Un matiz: la región del grupo es donde se guardan los datos que lo describen, no una obligación para lo que se meta dentro. La tienda, aun así, pondrá todo en la misma región.

## Cómo sabes que está bien

El script termina con «LISTO» y el grupo aparece en **Resource groups** con su región y sus cuatro etiquetas.

## Lo que todavía no hay

El grupo está vacío: nada se ejecuta todavía. La web llega en el módulo 2.
