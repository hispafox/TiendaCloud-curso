using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;

namespace Tienda.Web;

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
    public string? KeyVaultUri { get; set; }
    public string? NombreSecreto { get; set; } = "mensaje";
    public string? BlobUri { get; set; }
    public string? Contenedor { get; set; } = "fotos";
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

// Key Vault: la web lee el mensaje con su identidad, sin contrasena (modulo 4).
public sealed class ComprobacionKeyVault(OpcionesTienda opciones) : IComprobacion
{
    public string Nombre => "Key Vault";

    public async Task<ResultadoComprobacion> EjecutarAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(opciones.KeyVaultUri))
            return new ResultadoComprobacion(Nombre, false, "Sin configurar: falta Tienda__KeyVaultUri.");

        var cliente = new SecretClient(new Uri(opciones.KeyVaultUri), new DefaultAzureCredential());
        var secreto = await cliente.GetSecretAsync(opciones.NombreSecreto, cancellationToken: ct);
        return new ResultadoComprobacion(Nombre, true, secreto.Value.Value);
    }
}

// Almacenamiento: la web lista el contenedor de fotos con su identidad (modulo 3).
public sealed class ComprobacionAlmacenamiento(OpcionesTienda opciones) : IComprobacion
{
    public string Nombre => "Almacenamiento";

    public async Task<ResultadoComprobacion> EjecutarAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(opciones.BlobUri))
            return new ResultadoComprobacion(Nombre, false, "Sin configurar: falta Tienda__BlobUri.");

        var servicio = new BlobServiceClient(new Uri(opciones.BlobUri), new DefaultAzureCredential());
        var contenedor = servicio.GetBlobContainerClient(opciones.Contenedor);
        var total = 0;
        await foreach (var _ in contenedor.GetBlobsAsync(cancellationToken: ct))
            total++;
        return new ResultadoComprobacion(Nombre, true, $"Contenedor «{opciones.Contenedor}»: {total} fichero(s).");
    }
}

// Base de datos: la web guarda un pedido de prueba y cuenta los que hay (modulo 3).
public sealed class ComprobacionBaseDatos(string? cadenaConexion) : IComprobacion
{
    public string Nombre => "Base de datos";

    private const string Sql = """
        IF OBJECT_ID('dbo.Pedidos') IS NULL
            CREATE TABLE dbo.Pedidos (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Producto NVARCHAR(100) NOT NULL,
                Fecha DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
            );
        INSERT INTO dbo.Pedidos (Producto) VALUES (N'Pedido de prueba');
        SELECT COUNT(*) FROM dbo.Pedidos;
        """;

    public async Task<ResultadoComprobacion> EjecutarAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
            return new ResultadoComprobacion(Nombre, false, "Sin configurar: falta ConnectionStrings__Pedidos.");

        await using var conexion = new SqlConnection(cadenaConexion);
        await conexion.OpenAsync(ct);
        await using var comando = new SqlCommand(Sql, conexion);
        var total = Convert.ToInt32(await comando.ExecuteScalarAsync(ct));
        return new ResultadoComprobacion(Nombre, true, $"Pedido guardado. Pedidos en la tabla: {total}.");
    }
}
