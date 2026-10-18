import { useCallback, useEffect, useState } from 'react'
import { leerEstado, leerMensaje, listarProductos, pedir, ultimosPedidos } from './api.ts'
import Estado from './Estado.tsx'
import type { PedidoVista, Producto, ResultadoComprobacion } from './tipos.ts'
import './App.css'

const mensajeDe = (e: unknown) => (e instanceof Error ? e.message : 'Algo ha fallado.')

export default function App() {
  const [productos, setProductos] = useState<Producto[]>([])
  const [pedidos, setPedidos] = useState<PedidoVista[]>([])
  const [estado, setEstado] = useState<ResultadoComprobacion[]>([])
  const [mensaje, setMensaje] = useState<string | null>(null)
  const [aviso, setAviso] = useState<string | null>(null)

  const cargar = useCallback(async () => {
    try {
      const [listaProductos, listaPedidos] = await Promise.all([listarProductos(), ultimosPedidos()])
      setProductos(listaProductos)
      setPedidos(listaPedidos)
    } catch (e) {
      setAviso(mensajeDe(e))
    }
  }, [])

  useEffect(() => {
    void cargar()
    leerEstado()
      .then(setEstado)
      .catch(() => setEstado([]))
    leerMensaje()
      .then(setMensaje)
      .catch(() => setMensaje(null))
  }, [cargar])

  async function alPedir(producto: Producto) {
    setAviso(null)
    try {
      const pedido = await pedir(producto.id)
      setAviso(`Pedido ${pedido.id}: ${pedido.cantidad} × ${pedido.producto}.`)
      await cargar()
    } catch (e) {
      setAviso(mensajeDe(e))
    }
  }

  return (
    <main>
      <header>
        <h1>La tienda</h1>
        <p>El catálogo de la tienda del curso, servido por su API .NET.</p>
        {mensaje && <p className="mensaje">Mensaje del Key Vault: «{mensaje}»</p>}
      </header>

      {aviso && (
        <p className="aviso" role="status">
          {aviso}
        </p>
      )}

      <section className="catalogo" aria-label="Catálogo">
        {productos.map((p) => (
          <article key={p.id} className="producto">
            {p.foto && <img src={`/api/productos/${p.id}/foto`} alt={p.nombre} />}
            <h2>{p.nombre}</h2>
            <p>{p.descripcion}</p>
            <p className="existencias">{p.existencias} en existencias</p>
            <button onClick={() => void alPedir(p)} disabled={p.existencias === 0}>
              Pedir
            </button>
          </article>
        ))}
      </section>

      <section className="pedidos" aria-label="Últimos pedidos">
        <h2>Últimos pedidos</h2>
        {pedidos.length === 0 ? (
          <p>Todavía no hay pedidos.</p>
        ) : (
          <ul>
            {pedidos.map((p) => (
              <li key={p.id}>
                {p.cantidad} × {p.producto} · {new Date(p.fecha).toLocaleString('es-ES')}
              </li>
            ))}
          </ul>
        )}
      </section>

      <Estado filas={estado} />
    </main>
  )
}
