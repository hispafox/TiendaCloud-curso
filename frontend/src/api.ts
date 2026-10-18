import type { PedidoVista, Producto } from './tipos.ts'

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

export async function pedir(productoId: number, cantidad = 1): Promise<PedidoVista> {
  const respuesta = await fetch('/api/pedidos', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ productoId, cantidad }),
  })
  return leer<PedidoVista>(respuesta)
}
