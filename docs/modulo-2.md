# Módulo 2 · La tienda se ejecuta en App Service

**Rama:** `modulo-2/app-service` · **Viene de:** `modulo-1/grupo-de-recursos` · **Sigue en:** `modulo-3/datos`

## Qué sube a Azure

Dentro del grupo del módulo 1:

| Recurso | Nombre | Para qué |
|---|---|---|
| Plan de App Service | `plan-tienda-pruebas` | La capacidad: Linux, nivel gratuito Free (F1) |
| Web | `app-tienda-<sufijo>` | La tienda: la API .NET 10 y, en su `wwwroot`, el front React |

El plan es el gratuito: no se paga, a cambio de 60 minutos de CPU al día, sin Always On y sin deployment slots. Para una clase basta; la tienda de verdad necesitaría Standard o más, por los slots con los que el módulo 2 publica sin cortes.

Los dos llevan las cuatro etiquetas. El `<sufijo>` lo calcula el Bicep a partir del grupo, así que la dirección `https://app-tienda-<sufijo>.azurewebsites.net` es única y no cambia si se relanza.

Es la decisión del módulo 2 para la web y la API: una plataforma de aplicaciones, sin máquina que parchear. Las dos van en la misma web, porque el front es un puñado de ficheros estáticos que la propia API sirve.

## Qué cambia en el repositorio

| Fichero | Qué añade |
|---|---|
| `src/Tienda.Api` | La API: `GET /api/productos`, `GET /api/pedidos` y `POST /api/pedidos`. El catálogo, seis productos con sus existencias, vive en memoria (`CatalogoEnMemoria`) |
| `frontend` | El front React: el catálogo con un botón «Pedir» por producto y la lista de los últimos pedidos |
| `tests/Tienda.Api.Tests` | 11 tests: el catálogo en memoria y la API entera con `WebApplicationFactory` |
| `frontend/src/App.test.tsx` | 3 tests del front con Vitest |
| `azure/main.bicep` | Nuevo: el plan y la web |
| `azure/desplegar.ps1` | Tres pasos más: desplegar el Bicep, publicar (compila el front, publica la API y copia el front a su `wwwroot`) y llamar a `/api/productos` hasta que responde |

Pedir resta las existencias del producto y guarda el pedido. Si no quedan bastantes, la API responde 409 y no resta nada.

## Cómo se lanza

```powershell
git checkout modulo-2/app-service
pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
```

El script crea el grupo si no existe, así que funciona igual desde cero que sobre el grupo del módulo 1. Al acabar, `borrar.ps1`.

## Qué mirar en el Portal

En la web (`app-tienda-...`):

- **Overview**: la dirección por defecto, el plan y la región.
- **Configuration** > **General settings**: la pila, .NET 10 sobre Linux.
- En el plan, **Scale up**: el nivel Free (F1), con lo que no trae. Los deployment slots, para publicar sin cortes, solo aparecen desde Standard.

Fíjate también en lo que no hay: ni sistema operativo que parchear ni máquina que vigilar.

## Cómo sabes que está bien

El script termina con «LISTO» y escribe la URL. Al abrirla aparecen los seis productos, y al pulsar «Pedir» las existencias bajan y el pedido sale en la lista. La primera visita después de un rato sin uso tarda unos segundos: sin Always On, el plan gratuito duerme la web.

## Lo que todavía no hace

El catálogo y los pedidos viven en la memoria de la web: si se reinicia, vuelven a empezar, y con dos instancias cada una tendría los suyos. No hay fotos. En el módulo 3 el catálogo pasa a una base de datos y las fotos a un almacenamiento de objetos.
