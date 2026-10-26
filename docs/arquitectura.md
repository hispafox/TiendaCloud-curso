# Arquitectura en Azure · Módulo 6

**Rama:** `modulo-6/un-comando` (igual que `main`) · Lo que hay en Azure después de lanzar `desplegar.ps1` en esta rama. Cada rama trae su propia versión de este fichero, y dibuja solo lo que se monta en ella.

Es la tienda entera, y se monta desde un grupo vacío con una sola orden. El módulo 6 no añade recursos. Añade tres cosas para mirar la arquitectura desde fuera: el orden en que Bicep crea las piezas, un fallo provocado para ver qué se rompe y qué no, y la distancia hasta una versión de producción.

## Lo que hay en Azure

```mermaid
flowchart TB
    usuario(["Navegador"])
    entra(["Microsoft Entra ID"])
    subgraph rg["Grupo de recursos rg-tienda-pruebas · las cuatro etiquetas en todo"]
        subgraph plan["Plan de App Service plan-tienda-pruebas · Linux · Free F1"]
            web["Web app-tienda-#lt;sufijo#gt;<br/>API .NET 10 y front React<br/>solo HTTPS · TLS 1.2"]
        end
        id{{"Identidad administrada<br/>de la web"}}
        subgraph sql["Servidor SQL sql-tienda-#lt;sufijo#gt;"]
            fw{{"Cortafuegos<br/>AllowAzureServices"}}
            db[("Base pedidos<br/>serverless · oferta gratuita<br/>copias locales")]
        end
        subgraph st["Almacenamiento sttienda#lt;sufijo#gt; · StorageV2 · LRS"]
            fotos[["Contenedor fotos · privado"]]
            ciclo["Regla enfriar-fotos<br/>Cool a 30 días, Cold a 90"]
        end
        subgraph kv["Key Vault kv-tienda-#lt;sufijo#gt; · standard · RBAC"]
            mensaje["Secreto mensaje"]
        end
    end
    subgraph cm["Cost Management"]
        presupuesto["Presupuesto presupuesto-tienda"]
    end
    correo(["Correo del responsable"])
    usuario -- HTTPS --> web
    web -.- id
    id -. "pide sus tokens" .-> entra
    web -- "usuario y contraseña" --> fw --> db
    id -- "Storage Blob Data Contributor" --> fotos
    id -- "Key Vault Secrets User" --> mensaje
    ciclo -.- fotos
    presupuesto -. "mide el gasto" .-> rg
    presupuesto -- "previsto > 50, 90 y 100 %" --> correo
```

| Recurso | Nombre | Nivel | Módulo |
|---|---|---|---|
| Grupo de recursos | `rg-tienda-pruebas` | Las cuatro etiquetas | 1 |
| Plan de App Service | `plan-tienda-pruebas` | Linux, Free (F1) | 2 |
| Web | `app-tienda-<sufijo>` | .NET 10, solo HTTPS, TLS 1.2 | 2 |
| Servidor SQL y cortafuegos | `sql-tienda-<sufijo>`, `AllowAzureServices` | TLS 1.2; solo los servicios de Azure | 3 |
| Base de datos | `pedidos` | General Purpose serverless, 2 vCores, oferta gratuita, copias locales | 3 |
| Cuenta de almacenamiento | `sttienda<sufijo>` | StorageV2, Standard LRS, Hot, sin acceso anónimo | 3 |
| Contenedor y ciclo de vida | `fotos`, `enfriar-fotos` | Privado; Cool a 30 días, Cold a 90 | 3 |
| Identidad administrada | La de la web | Asignada por el sistema | 4 |
| Key Vault y secreto | `kv-tienda-<sufijo>`, `mensaje` | Standard, RBAC, borrado temporal de 7 días | 4 |
| Permisos | Storage Blob Data Contributor, Key Vault Secrets User | Cada uno sobre un solo recurso | 4 |
| Presupuesto | `presupuesto-tienda` | Mensual, avisos al 50, 90 y 100 % del previsto | 5 |

Todo va en la región de `$Ubicacion`, menos el servidor SQL y su base, que van en la de `$UbicacionSql` si la pones: la oferta gratuita pone todas las bases gratuitas de una suscripción en la misma región.

## En qué orden lo crea Bicep

Nadie escribe el orden: Bicep lo calcula a partir de lo que cada recurso necesita. Una flecha de A a B quiere decir que B no empieza hasta que A existe. Lo que no tiene flechas entre sí se crea a la vez.

```mermaid
flowchart LR
    plan["Plan"] --> web["Web y su identidad"]
    srv["Servidor SQL"] --> db["Base pedidos"]
    srv --> fw["Cortafuegos"]
    cuenta["Cuenta de almacenamiento"] --> blobs["Servicio de blobs"] --> fotos["Contenedor fotos"]
    cuenta --> ciclo["Regla enfriar-fotos"]
    kv["Key Vault"] --> mensaje["Secreto mensaje"]
    db --> web
    fotos --> web
    mensaje --> web
    web --> rolSt["Permiso sobre el almacenamiento"]
    cuenta --> rolSt
    web --> rolKv["Permiso sobre el Key Vault"]
    kv --> rolKv
    presupuesto["Presupuesto"]
```

La web es la pieza que más espera: su configuración lleva la dirección de la base, la del almacenamiento y la del Key Vault. Y los permisos esperan a la web, porque se dan a su identidad, que nace con ella. El presupuesto no depende de nada y se crea a la vez que el resto.

## El fallo provocado

```mermaid
sequenceDiagram
    actor P as Profesor
    participant S as provocar-fallo.ps1
    participant A as Azure RBAC
    participant W as Web
    participant K as Key Vault
    P->>S: lanza el script
    S->>A: borra la asignación Key Vault Secrets User de la identidad de la web
    Note over W,K: Al cabo de unos minutos
    W->>K: lee el secreto mensaje
    K-->>W: 403, sin permiso
    W-->>P: /salud responde 503: la fila del Key Vault en rojo,<br/>la base y el almacenamiento siguen en verde
    P->>A: desplegar.ps1: el Bicep repone la asignación que falta
```

Lo interesante no es que falle el Key Vault, sino que **solo** falle el Key Vault. Cada pieza tiene su propia comprobación en `/salud`, así que el panel señala la pieza rota y deja en verde el resto.

## La red

| Pieza | Acceso público | Quién entra |
|---|---|---|
| Web | Sí, por HTTPS | Cualquiera con la dirección |
| Servidor SQL | Sí, cerrado por el cortafuegos | Los servicios de Azure, con usuario y contraseña |
| Almacenamiento | Sí, sin acceso anónimo | Una identidad con rol de datos, como la de la web |
| Key Vault | Sí | Una identidad con rol, como la de la web |

No hay red virtual, ni endpoints privados, ni borde con cortafuegos de aplicaciones web. Es una decisión, no un olvido: la tienda se monta y se borra en una clase, y la identidad ya decide quién entra a cada recurso. La lista de comprobación lo anota como pendiente para producción.

## La versión de producción (no se despliega)

Este diagrama no lo monta ningún script. Es la distancia entre la tienda del curso y una de verdad, sacada de `docs/lista-comprobacion.md`. El diseño de la red, con sus subredes, zonas DNS, reglas y lo que cuesta, está en `docs/red-virtual.md`.

```mermaid
flowchart LR
    usuario(["Navegador"])
    subgraph borde["Borde"]
        waf["Front Door o Application Gateway<br/>con WAF"]
    end
    subgraph vnet["Red virtual de la tienda"]
        subgraph snApp["Subred de la web"]
            web["Web en App Service<br/>con integración de red<br/>y escalado programado"]
        end
        subgraph snPe["Subred de endpoints privados"]
            peSql["Endpoint privado SQL"]
            peSt["Endpoint privado almacenamiento"]
            peKv["Endpoint privado Key Vault"]
        end
    end
    db[("Base pedidos<br/>la web entra con su identidad<br/>réplica en otra zona")]
    st[["Almacenamiento<br/>sin acceso público"]]
    kv["Key Vault<br/>sin acceso público"]
    cola[["Cola de pedidos"]]
    cache["Caché del catálogo"]
    alertas["Alertas sobre los síntomas"]
    usuario -- HTTPS --> waf --> web
    web --> peSql --> db
    web --> peSt --> st
    web --> peKv --> kv
    web -.-> cola
    web -.-> cache
    alertas -.- web
    classDef futuro fill:#fff8e1,stroke:#b26a00,stroke-dasharray:5 4,color:#4a2c00
    class waf,web,peSql,peSt,peKv,db,st,kv,cola,cache,alertas futuro
```

| Lo que falta para producción | Dónde se decidió |
|---|---|
| Réplica de la base y copias en otra zona | Módulo 3 |
| Escalado programado y la cola de pedidos | Módulo 2 |
| La base sin contraseña, y la red privada | Módulo 4 |
| El borde con cortafuegos de aplicaciones web | Módulo 4 |
| Alertas sobre los síntomas | Módulo 5 |
| Caché para el catálogo y las sesiones | Módulo 3 |

## Cómo se monta y cómo se borra

```mermaid
flowchart TB
    secretos[/"deploy.secrets.ps1"/]
    subgraph ver["ver-cambios.ps1 · no despliega nada"]
        direction LR
        v1["Crea el grupo vacío,<br/>si no existe"] --> v2["az deployment group what-if"]
    end
    subgraph desplegar["desplegar.ps1 · una sola orden"]
        direction LR
        d1["1. Login"] --> d2["2. Grupo"] --> d3["3. Bicep entero"] --> d4["4. Publicar la API<br/>con el front"] --> d5["5. GET /salud"]
    end
    subgraph borrar["borrar.ps1"]
        direction LR
        b1["1. Presupuesto"] --> b2["2. Grupo entero"] --> b3["3. Purga del Key Vault"]
    end
    secretos --> ver
    secretos --> desplegar
    secretos --> borrar
```

Con estas dos órdenes, montar y borrar, la tienda deja de ser algo que se monta a mano y pasa a ser algo que se repite. Y eso es lo que permite probarla en clase, romperla a propósito y volver a dejarla como estaba.
