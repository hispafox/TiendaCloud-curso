import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App.tsx'

const productos = [{ id: 1, nombre: 'Camiseta', descripcion: 'De algodón.', existencias: 2, foto: 'camiseta.jpg' }]

const salud = [
  { nombre: 'Base de datos', correcta: true, detalle: 'Catálogo leído de la base: 6 productos.' },
  { nombre: 'Almacenamiento', correcta: false, detalle: 'Sin configurar: falta ConnectionStrings__Fotos.' },
]

// Una respuesta de fetch con lo justo que usa api.ts.
function respuesta(cuerpo: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(cuerpo),
  } as Response)
}

describe('App', () => {
  let pedidos: unknown[]

  beforeEach(() => {
    pedidos = []
    vi.stubGlobal(
      'fetch',
      vi.fn((url: string, init?: RequestInit) => {
        if (url === '/api/productos') return respuesta(productos)
        if (url === '/salud') return respuesta(salud, 503)
        if (url === '/api/mensaje') return respuesta({ mensaje: 'Hola desde el Key Vault de la tienda' })
        if (url === '/api/pedidos' && init?.method === 'POST') {
          const pedido = { id: 1, producto: 'Camiseta', cantidad: 1, fecha: '2026-10-19T09:30:00Z' }
          pedidos = [pedido]
          return respuesta(pedido, 201)
        }
        if (url === '/api/pedidos') return respuesta(pedidos)
        return respuesta({ error: 'No existe.' }, 404)
      }),
    )
  })

  afterEach(() => {
    cleanup()
    vi.unstubAllGlobals()
  })

  it('enseña el catálogo que devuelve la API, con la foto de cada producto', async () => {
    render(<App />)

    expect(await screen.findByText('Camiseta')).toBeInTheDocument()
    expect(screen.getByText('2 en existencias')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Camiseta' })).toHaveAttribute('src', '/api/productos/1/foto')
  })

  it('al pedir, manda el pedido a la API y lo enseña', async () => {
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Pedir' }))

    expect(await screen.findByText('Pedido 1: 1 × Camiseta.')).toBeInTheDocument()
    expect(fetch).toHaveBeenCalledWith('/api/pedidos', expect.objectContaining({ method: 'POST' }))
  })

  it('enseña el estado de cada pieza aunque /salud responda 503', async () => {
    render(<App />)

    expect(await screen.findByText('Estado de la tienda')).toBeInTheDocument()
    expect(screen.getByText('Correcto')).toBeInTheDocument()
    expect(screen.getByText('Falla')).toBeInTheDocument()
    expect(screen.getByText('Sin configurar: falta ConnectionStrings__Fotos.')).toBeInTheDocument()
  })

  it('enseña el mensaje que la web lee del Key Vault', async () => {
    render(<App />)

    expect(await screen.findByText('Mensaje del Key Vault: «Hola desde el Key Vault de la tienda»')).toBeInTheDocument()
  })

  it('si la API falla, lo dice en la página y no enseña mensaje', async () => {
    vi.stubGlobal('fetch', vi.fn(() => respuesta({ error: 'La API no responde.' }, 503)))

    render(<App />)

    expect(await screen.findByText('La API no responde.')).toBeInTheDocument()
    expect(screen.queryByText(/Mensaje del Key Vault/)).not.toBeInTheDocument()
  })
})
