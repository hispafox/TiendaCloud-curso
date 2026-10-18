import type { ResultadoComprobacion } from './tipos.ts'

// El panel de estado: una fila por pieza de la tienda, con lo que ha visto la web.
export default function Estado({ filas }: { filas: ResultadoComprobacion[] }) {
  if (filas.length === 0) return null

  return (
    <section className="estado" aria-label="Estado de la tienda">
      <h2>Estado de la tienda</h2>
      <table>
        <tbody>
          {filas.map((f) => (
            <tr key={f.nombre}>
              <td>{f.nombre}</td>
              <td className={f.correcta ? 'ok' : 'mal'}>{f.correcta ? 'Correcto' : 'Falla'}</td>
              <td>{f.detalle}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
