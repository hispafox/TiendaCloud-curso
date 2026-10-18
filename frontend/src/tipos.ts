// Lo que devuelve la API .NET, campo a campo.

export interface Producto {
  id: number
  nombre: string
  descripcion: string
  existencias: number
}

export interface PedidoVista {
  id: number
  producto: string
  cantidad: number
  fecha: string
}
