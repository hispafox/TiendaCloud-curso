# La lista de comprobación de la tienda

Las preguntas salen de los pilares del Well-Architected Framework de Azure, aplicadas a la tienda. Es la lista del módulo 6 del curso, con la columna de la versión básica ajustada a lo que monta este repositorio.

Se usa **un pilar cada vez**, sobre un diseño concreto, preguntando qué está decidido y qué no. Pasar los cinco a la vez sobre un diseño grande produce una lista de pendientes tan larga que no se empieza por ninguno.

| Pilar | Pregunta | Versión básica (este repositorio) | Versión de producción |
|---|---|---|---|
| Fiabilidad | ¿Qué pasa si falla un edificio? | La tienda deja de funcionar, y es aceptable | Réplica de la base y copias en otra zona |
| Fiabilidad | ¿Qué pasa si llega un pico? | No está preparada | Escalado programado y la cola |
| Seguridad | ¿Hay secretos en la configuración? | Uno: la contraseña de la base | Ninguno; la API entra con su identidad |
| Seguridad | ¿Qué tiene dirección pública? | La web; la base, para los servicios de Azure; y el almacenamiento, sin acceso anónimo | Solo el borde |
| Seguridad | ¿Cada identidad tiene solo lo que necesita? | Sí, dos permisos sobre dos recursos | Sí, y revisados cada cierto tiempo |
| Coste | ¿Sabes de quién es cada recurso? | Sí, por las etiquetas, y con un presupuesto que avisa | Sí, y con presupuesto y alertas |
| Coste | ¿Se borra lo que no se usa? | Sí, con una orden | Los entornos de prueba, igual |
| Operación | ¿Se puede repetir el montaje? | Sí, con el Bicep y el script | Sí, con el plan revisado antes |
| Operación | ¿Te enteras de que algo va mal? | `/salud` y el panel de estado de la tienda | Alertas sobre los síntomas |
| Rendimiento | ¿Aguanta la campaña? | No es su objetivo | Caché, escalado y la cola |

## Cómo se lee

La columna de la versión básica dice qué se ha decidido no hacer, y por qué es aceptable para una tienda que se monta y se borra en una clase. Esa es la forma de usar la lista con una arquitectura de tu empresa: cada «no» con su razón es una decisión; cada «no» sin razón es una pregunta pendiente.

## Dónde se decidió cada cosa

| Lo que falta para producción | Módulo |
|---|---|
| Réplica de la base y copias en otra zona | 3 |
| Escalado programado y la cola de pedidos | 2 |
| La base sin contraseña, y la red privada | 4 |
| El borde con cortafuegos de aplicaciones web | 4 |
| Alertas sobre los síntomas | 5 |
| Caché para el catálogo y las sesiones | 3 |
