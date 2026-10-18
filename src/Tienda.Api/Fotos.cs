using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Tienda.Api;

// Una foto leida, con su tipo para el navegador.
public sealed record FotoLeida(Stream Contenido, string Tipo);

// Donde estan las fotos de los productos.
public interface IAlmacenFotos
{
    Task<FotoLeida?> AbrirAsync(string nombre, CancellationToken ct);
}

// Las fotos de ejemplo, metidas dentro de la propia web (Fotos\*.svg).
public static class FotosEjemplo
{
    public const string Tipo = "image/svg+xml";

    public static IEnumerable<string> Nombres => DatosEjemplo.Productos().Select(p => p.Foto!);

    public static Stream? Abrir(string nombre) =>
        typeof(FotosEjemplo).Assembly.GetManifestResourceStream($"Fotos.{nombre}");
}

// En local, sin almacenamiento configurado, las fotos salen de la propia web.
public sealed class AlmacenFotosLocal : IAlmacenFotos
{
    public Task<FotoLeida?> AbrirAsync(string nombre, CancellationToken ct)
    {
        var contenido = FotosEjemplo.Abrir(nombre);
        return Task.FromResult(contenido is null ? null : new FotoLeida(contenido, FotosEjemplo.Tipo));
    }
}

// Modulo 3: las fotos en Blob Storage, en el contenedor privado «fotos». La web
// las sirve ella misma, porque el contenedor no se abre a Internet. Desde el
// modulo 4 entra con su identidad, sin clave.
public sealed class AlmacenFotosBlob(BlobContainerClient contenedor) : IAlmacenFotos
{
    private readonly SemaphoreSlim cerrojo = new(1, 1);
    private bool sembrado;

    public async Task<FotoLeida?> AbrirAsync(string nombre, CancellationToken ct)
    {
        await SembrarAsync(ct);
        try
        {
            var descarga = await contenedor.GetBlobClient(nombre).DownloadStreamingAsync(cancellationToken: ct);
            return new FotoLeida(descarga.Value.Content, descarga.Value.Details.ContentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    // La primera vez, sube las fotos de ejemplo que falten en el contenedor.
    private async Task SembrarAsync(CancellationToken ct)
    {
        if (sembrado)
            return;
        await cerrojo.WaitAsync(ct);
        try
        {
            if (sembrado)
                return;
            foreach (var nombre in FotosEjemplo.Nombres)
            {
                var blob = contenedor.GetBlobClient(nombre);
                if (await blob.ExistsAsync(ct))
                    continue;
                await using var contenido = FotosEjemplo.Abrir(nombre)!;
                var cabeceras = new BlobHttpHeaders { ContentType = FotosEjemplo.Tipo };
                await blob.UploadAsync(contenido, new BlobUploadOptions { HttpHeaders = cabeceras }, ct);
            }
            sembrado = true;
        }
        finally
        {
            cerrojo.Release();
        }
    }
}
