# Arquitectura en Azure · Módulo 1

**Rama:** `modulo-1/grupo-de-recursos` · Lo que hay en Azure después de lanzar `desplegar.ps1` en esta rama. Cada rama trae su propia versión de este fichero, y dibuja solo lo que se monta en ella.

En el módulo 1 todavía no hay tienda: solo el sitio donde va a vivir. Parece poca cosa, pero aquí se decide lo que más cuesta cambiar después. En qué suscripción entra, cómo se llama su grupo y en qué región va.

## Lo que hay en Azure

En verde, lo que añade este módulo.

```mermaid
flowchart TB
    suscripcion["Suscripción<br/>la de $Suscripcion"]
    subgraph rg["Grupo de recursos rg-tienda-pruebas · westeurope"]
        vacio["Vacío: la tienda llega en el módulo 2"]
    end
    etiquetas[/"Etiquetas del grupo<br/>aplicacion = tienda<br/>entorno = pruebas<br/>responsable = $Responsable<br/>centro-de-coste = curso-arquitectura-cloud"/]
    suscripcion --> rg
    etiquetas -.- rg
    classDef nuevo fill:#e3f2e5,stroke:#2e7d32,stroke-width:2px,color:#1b3d1f
    class etiquetas nuevo
    style rg fill:#f1f8f2,stroke:#2e7d32,stroke-width:2px
```

| Recurso | Nombre | Región | Lo que cuesta |
|---|---|---|---|
| Grupo de recursos | `rg-tienda-pruebas` (el de `$GrupoRecursos`) | `westeurope` (la de `$Ubicacion`) | Nada |

Los valores salen de `azure/deploy.secrets.ps1`; los de la tabla son los de la plantilla. La región del grupo es donde Azure guarda los datos que lo describen, no una obligación para lo que se meta dentro, aunque la tienda pondrá ahí casi todo.

## La red

No hay nada que exponer. Un grupo vacío no tiene direcciones, ni puertos, ni nada a lo que llamar.

## Cómo se monta y cómo se borra

```mermaid
flowchart LR
    secretos[/"azure/deploy.secrets.ps1<br/>suscripción, región, grupo y responsable"/] --> desplegar
    subgraph desplegar["desplegar.ps1"]
        direction TB
        d1["1. az login, si hace falta<br/>az account set"] --> d2["2. az group create<br/>con las cuatro etiquetas"]
    end
    subgraph borrar["borrar.ps1"]
        direction TB
        b0{"¿Existe el grupo?"} -- sí --> b1["az group delete"]
        b0 -- no --> b2["Lo dice y termina sin error"]
    end
```

`desplegar.ps1` se puede lanzar las veces que haga falta: si el grupo ya existe, lo deja como está y actualiza sus etiquetas.

## Lo que llega después

El módulo 2 mete en este grupo un plan de App Service y la web con la tienda.
