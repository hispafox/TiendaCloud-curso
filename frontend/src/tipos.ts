// Lo que devuelve la API .NET, campo a campo.

export interface Producto {
  id: number
  nombre: string
  descripcion: string
  existencias: number
  foto: string | null
}

export interface PedidoVista {
  id: number
  producto: string
  cantidad: number
  fecha: string
}

// Una fila de /salud: una pieza de la tienda y si responde.
export interface ResultadoComprobacion {
  nombre: string
  correcta: boolean
  detalle: string
}
