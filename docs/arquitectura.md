# Arquitectura en Azure · Módulo 3

**Rama:** `modulo-3/datos` · Lo que hay en Azure después de lanzar `desplegar.ps1` en esta rama. Cada rama trae su propia versión de este fichero, y dibuja solo lo que se monta en ella.

Los datos salen de la memoria de la web. Los pedidos van a una base relacional, porque tienen que cuadrar, y las fotos a un almacenamiento de objetos con su ciclo de vida. La web entra a los dos con credenciales escritas en su configuración, que es justo lo que arregla el módulo 4.

## Lo que hay en Azure

En verde, lo que añade este módulo.

```mermaid
flowchart TB
    usuario(["Navegador"])
    subgraph rg["Grupo de recursos rg-tienda-pruebas · las cuatro etiquetas en todo"]
        subgraph plan["Plan de App Service plan-tienda-pruebas · Linux · Free F1"]
            web["Web app-tienda-#lt;sufijo#gt;<br/>API .NET 10 y front React"]
        end
        subgraph sql["Servidor SQL sql-tienda-#lt;sufijo#gt;"]
            fw{{"Cortafuegos<br/>AllowAzureServices"}}
            db[("Base pedidos<br/>General Purpose serverless, 2 vCores<br/>oferta gratuita · copias locales")]
        end
        subgraph st["Almacenamiento sttienda#lt;sufijo#gt; · StorageV2 · LRS · Hot"]
            fotos[["Contenedor fotos · privado"]]
            ciclo["Regla enfriar-fotos<br/>Cool a los 30 días, Cold a los 90"]
        end
    end
    usuario -- HTTPS --> web
    web -- "usuario y contraseña<br/>ConnectionStrings__Pedidos" --> fw --> db
    web -- "clave de la cuenta<br/>ConnectionStrings__Fotos" --> fotos
    ciclo -.- fotos
    classDef nuevo fill:#e3f2e5,stroke:#2e7d32,stroke-width:2px,color:#1b3d1f
    class fw,db,fotos,ciclo nuevo
    style sql fill:#f1f8f2,stroke:#2e7d32,stroke-width:2px
    style st fill:#f1f8f2,stroke:#2e7d32,stroke-width:2px
```

| Recurso | Nombre | Nivel | Lo que cuesta |
|---|---|---|---|
| Servidor SQL | `sql-tienda-<sufijo>` | Administrador `tiendaadmin`, TLS 1.2 como mínimo | Nada por sí solo |
| Regla del cortafuegos | `AllowAzureServices` | `0.0.0.0`: deja entrar a los servicios de Azure | Nada |
| Base de datos | `pedidos` | General Purpose serverless, Gen5, 2 vCores. Oferta gratuita que se pausa al agotar el cupo, con copias de seguridad locales | Nada: cada mes trae 100.000 segundos de vCore y 32 GB |
| Cuenta de almacenamiento | `sttienda<sufijo>` | StorageV2, Standard LRS, nivel Hot, sin acceso anónimo | Céntimos: seis fotos y pocas operaciones |
| Contenedor | `fotos` | Privado | Va en la cuenta |
| Regla de ciclo de vida | `enfriar-fotos` | Cool a los 30 días sin cambios, Cold a los 90 | Nada |

Todo va en la región del grupo menos el servidor SQL y su base, que van en la de `$UbicacionSql` si la pones en `deploy.secrets.ps1`. La oferta gratuita pone todas las bases gratuitas de una suscripción en la misma región: si ya tienes alguna en otra, la tienda la sigue.

## Cómo responde la tienda

```mermaid
sequenceDiagram
    actor U as Navegador
    participant W as Web
    participant D as Base pedidos
    participant B as Contenedor fotos
    U->>W: GET /api/productos
    W->>D: consulta con EF Core
    Note over W,D: La primera vez crea las tablas y carga los seis productos.<br/>Si la base estaba en pausa, tarda cerca de un minuto<br/>en despertar, y EF Core reintenta mientras tanto
    D-->>W: productos y existencias
    W-->>U: el catálogo
    U->>W: GET /api/productos/1/foto
    W->>B: lee la foto con la clave de la cuenta
    Note over W,B: La primera vez sube las seis fotos de ejemplo que falten
    B-->>W: la imagen
    W-->>U: la imagen
    U->>W: POST /api/pedidos
    W->>D: resta existencias y guarda el pedido, en la misma transacción
    W-->>U: pedido hecho
    U->>W: GET /salud
    W->>D: ¿responde?
    W->>B: ¿responde?
    W-->>U: 200 si todo va bien, 503 si algo falla
```

## Las credenciales

| Ajuste de la web | Qué lleva |
|---|---|
| `ConnectionStrings__Pedidos` | El servidor, la base, el usuario `tiendaadmin` y su contraseña |
| `ConnectionStrings__Fotos` | El nombre de la cuenta y su clave, que abre la cuenta entera |
| `Tienda__Contenedor` | `fotos` |

Las dos cadenas las escribe el Bicep al desplegar: nadie copia una dirección ni una clave a mano. Aun así, la contraseña y la clave quedan a la vista de cualquiera que pueda leer la configuración de la web.

## La red

| Pieza | Acceso público | Quién entra |
|---|---|---|
| Web | Sí, por HTTPS | Cualquiera con la dirección |
| Servidor SQL | Sí, cerrado por el cortafuegos | Los servicios de Azure, con usuario y contraseña |
| Almacenamiento | Sí, sin acceso anónimo | Quien tenga la clave de la cuenta |

Ojo con lo que deja pasar `AllowAzureServices`: cualquier servicio de Azure, no solo esta web. Para una clase vale. En producción la base iría sin acceso público, detrás de un endpoint privado en una red virtual.

## Cómo se monta

```mermaid
flowchart LR
    secretos[/"deploy.secrets.ps1"/] --> s1["1. az login<br/>az account set"]
    s1 --> s2["2. az group create"]
    s2 --> s3["3. az deployment group create<br/>main.bicep: plan, web, SQL<br/>y almacenamiento"]
    s3 --> s4["4. npm ci y npm run build<br/>dotnet publish con el front en wwwroot<br/>az webapp deploy del zip"]
    s4 --> s5["5. GET /salud<br/>hasta 10 intentos"]
```

Dentro del paso 3, la web espera a que existan la base y el contenedor: su configuración lleva las dos cadenas, y no se pueden escribir antes. `borrar.ps1` borra el grupo entero, con todo dentro.

## Lo que llega después

El módulo 4 da a la web una identidad propia. Con ella, la clave del almacenamiento desaparece de la configuración, y llega un Key Vault.
