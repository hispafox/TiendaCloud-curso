using Tienda.Web;

namespace Tienda.Web.Tests;

public class ComprobacionesTests
{
    private sealed class ComprobacionQueFalla : IComprobacion
    {
        public string Nombre => "Falla siempre";
        public Task<ResultadoComprobacion> EjecutarAsync(CancellationToken ct) => throw new InvalidOperationException("sin permiso");
    }

    [Fact]
    public async Task EjecutarSeguro_convierte_la_excepcion_en_una_fila_fallida()
    {
        var resultado = await EjecutorComprobaciones.EjecutarSeguroAsync(new ComprobacionQueFalla(), CancellationToken.None);

        Assert.False(resultado.Correcta);
        Assert.Equal("Falla siempre", resultado.Nombre);
        Assert.Equal("sin permiso", resultado.Detalle);
    }

    [Fact]
    public async Task KeyVault_sin_configurar_falla_sin_lanzar()
    {
        var resultado = await new ComprobacionKeyVault(new OpcionesTienda()).EjecutarAsync(CancellationToken.None);

        Assert.False(resultado.Correcta);
        Assert.Contains("Tienda__KeyVaultUri", resultado.Detalle);
    }

    [Fact]
    public async Task Almacenamiento_sin_configurar_falla_sin_lanzar()
    {
        var resultado = await new ComprobacionAlmacenamiento(new OpcionesTienda()).EjecutarAsync(CancellationToken.None);

        Assert.False(resultado.Correcta);
        Assert.Contains("Tienda__BlobUri", resultado.Detalle);
    }

    [Fact]
    public async Task BaseDatos_sin_cadena_falla_sin_lanzar()
    {
        var resultado = await new ComprobacionBaseDatos(null).EjecutarAsync(CancellationToken.None);

        Assert.False(resultado.Correcta);
        Assert.Contains("ConnectionStrings__Pedidos", resultado.Detalle);
    }
}
