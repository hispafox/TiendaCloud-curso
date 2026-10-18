using Azure.Storage.Blobs;

namespace Tienda.Api;

// Lo que devuelve cada comprobacion: si ha ido bien y que ha visto.
public sealed record ResultadoComprobacion(string Nombre, bool Correcta, string Detalle);

// Una pieza de la tienda que se puede comprobar desde la propia aplicacion.
public interface IComprobacion
{
    string Nombre { get; }
    Task<ResultadoComprobacion> EjecutarAsync(CancellationToken ct);
}

// Configuracion que el Bicep deja en los App Settings de la web.
public sealed class OpcionesTienda
{
    public string Contenedor { get; set; } = "fotos";
}

// Ejecuta una comprobacion sin dejar que una excepcion tumbe la pagina entera.
public static class EjecutorComprobaciones
{
    public static async Task<ResultadoComprobacion> EjecutarSeguroAsync(IComprobacion comprobacion, CancellationToken ct)
    {
        try
        {
            return await comprobacion.EjecutarAsync(ct);
        }
        catch (Exception ex)
        {
            return new ResultadoComprobacion(comprobacion.Nombre, false, ex.Message);
        }
    }
}

// Decide si la tienda esta entera. Lo usa /salud; el panel de estado del front lo ensena fila a fila.
public static class PaginaEstado
{
    public static bool TodoCorrecto(IReadOnlyCollection<ResultadoComprobacion> resultados) =>
        resultados.Count > 0 && resultados.All(r => r.Correcta);
}

// Base de datos: la web lee el catalogo de la base (modulo 3).
public sealed class ComprobacionBaseDatos(string? cadenaConexion, IServiceScopeFactory scopes) : IComprobacion
{
    public string Nombre => "Base de datos";

    public async Task<ResultadoComprobacion> EjecutarAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
            return new ResultadoComprobacion(Nombre, false, "Sin configurar: falta ConnectionStrings__Pedidos.");

        await using var scope = scopes.CreateAsyncScope();
        var productos = await scope.ServiceProvider.GetRequiredService<ICatalogo>().ListarProductosAsync(ct);
        return new ResultadoComprobacion(Nombre, true, $"Catálogo leído de la base: {productos.Count} productos.");
    }
}

// Almacenamiento: la web lista el contenedor de fotos con la clave de la cuenta (modulo 3).
public sealed class ComprobacionAlmacenamiento(string? cadenaConexion, string contenedor) : IComprobacion
{
    public string Nombre => "Almacenamiento";

    public async Task<ResultadoComprobacion> EjecutarAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
            return new ResultadoComprobacion(Nombre, false, "Sin configurar: falta ConnectionStrings__Fotos.");

        var cliente = new BlobContainerClient(cadenaConexion, contenedor);
        var total = 0;
        await foreach (var _ in cliente.GetBlobsAsync(cancellationToken: ct))
            total++;
        return new ResultadoComprobacion(Nombre, true, $"Contenedor «{contenedor}»: {total} fichero(s).");
    }
}
