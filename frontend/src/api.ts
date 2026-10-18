import type { PedidoVista, Producto, ResultadoComprobacion } from './tipos.ts'

// Si la API responde con error, su mensaje llega a la pagina tal cual.
async function leer<T>(respuesta: Response): Promise<T> {
  if (!respuesta.ok) {
    const cuerpo = (await respuesta.json().catch(() => null)) as { error?: string } | null
    throw new Error(cuerpo?.error ?? `La API ha respondido ${respuesta.status}.`)
  }
  return (await respuesta.json()) as T
}

export async function listarProductos(): Promise<Producto[]> {
  return leer<Producto[]>(await fetch('/api/productos'))
}

export async function ultimosPedidos(): Promise<PedidoVista[]> {
  return leer<PedidoVista[]>(await fetch('/api/pedidos'))
}

// El mensaje que la web lee del Key Vault con su identidad. Si no puede, no hay mensaje.
export async function leerMensaje(): Promise<string | null> {
  const respuesta = await fetch('/api/mensaje')
  if (!respuesta.ok) return null
  const cuerpo = (await respuesta.json()) as { mensaje?: string }
  return cuerpo.mensaje ?? null
}

// /salud responde 200 si todo va bien y 503 si algo falla, y las filas vienen en los dos casos.
export async function leerEstado(): Promise<ResultadoComprobacion[]> {
  const respuesta = await fetch('/salud')
  const cuerpo: unknown = await respuesta.json()
  return Array.isArray(cuerpo) ? (cuerpo as ResultadoComprobacion[]) : []
}

export async function pedir(productoId: number, cantidad = 1): Promise<PedidoVista> {
  const respuesta = await fetch('/api/pedidos', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ productoId, cantidad }),
  })
  return leer<PedidoVista>(respuesta)
}
