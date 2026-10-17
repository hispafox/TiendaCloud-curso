using System.Net;
using System.Text;

namespace Tienda.Web;

// Monta la pagina que se ensena en clase: una fila por pieza de la tienda.
public static class PaginaEstado
{
    public static bool TodoCorrecto(IReadOnlyCollection<ResultadoComprobacion> resultados) =>
        resultados.Count > 0 && resultados.All(r => r.Correcta);

    public static string Generar(IReadOnlyCollection<ResultadoComprobacion> resultados)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\">");
        html.Append("<title>La tienda en Azure</title>");
        html.Append("<style>body{font-family:sans-serif;max-width:720px;margin:40px auto;padding:0 16px}");
        html.Append("td{padding:8px;border-bottom:1px solid #ddd}.ok{color:#137333}.mal{color:#b3261e}</style>");
        html.Append("</head><body><h1>La tienda en Azure</h1>");

        var estado = TodoCorrecto(resultados) ? "<p class=\"ok\">Todo en marcha.</p>" : "<p class=\"mal\">Hay piezas que fallan.</p>";
        html.Append(estado);
        html.Append("<table>");
        foreach (var r in resultados)
        {
            var clase = r.Correcta ? "ok" : "mal";
            var marca = r.Correcta ? "Correcto" : "Falla";
            html.Append("<tr><td>").Append(WebUtility.HtmlEncode(r.Nombre)).Append("</td>");
            html.Append("<td class=\"").Append(clase).Append("\">").Append(marca).Append("</td>");
            html.Append("<td>").Append(WebUtility.HtmlEncode(r.Detalle)).Append("</td></tr>");
        }
        html.Append("</table></body></html>");
        return html.ToString();
    }
}
