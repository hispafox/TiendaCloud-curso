using Microsoft.Extensions.DependencyInjection;

namespace Tienda.Api.Tests;

public class ComprobacionesTests
{
    private sealed class ComprobacionQueFalla : IComprobacion
    {
        public string Nombre => "Falla siempre";
        public Task<ResultadoComprobacion> EjecutarAsync(CancellationToken ct) => throw new InvalidOperationException("sin permiso");
    }

    private static IServiceScopeFactory ScopesConCatalogoEnMemoria() =>
        new ServiceCollection()
            .AddSingleton<ICatalogo, CatalogoEnMemoria>()
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

    [Fact]
    public async Task EjecutarSeguro_convierte_la_excepcion_en_una_fila_fallida()
    {
        var resultado = await EjecutorComprobaciones.EjecutarSeguroAsync(new ComprobacionQueFalla(), CancellationToken.None);

        Assert.False(resultado.Correcta);
        Assert.Equal("Falla siempre", resultado.Nombre);
        Assert.Equal("sin permiso", resultado.Detalle);
    }

    [Fact]
    public async Task BaseDatos_sin_cadena_falla_sin_lanzar()
    {
        var resultado = await new ComprobacionBaseDatos(null, ScopesConCatalogoEnMemoria()).EjecutarAsync(CancellationToken.None);

        Assert.False(resultado.Correcta);
        Assert.Contains("ConnectionStrings__Pedidos", resultado.Detalle);
    }

    [Fact]
    public async Task BaseDatos_con_cadena_lee_el_catalogo()
    {
        var resultado = await new ComprobacionBaseDatos("configurada", ScopesConCatalogoEnMemoria()).EjecutarAsync(CancellationToken.None);

        Assert.True(resultado.Correcta);
        Assert.Contains("6 productos", resultado.Detalle);
    }

    [Fact]
    public async Task Almacenamiento_sin_cadena_falla_sin_lanzar()
    {
        var resultado = await new ComprobacionAlmacenamiento(null, "fotos").EjecutarAsync(CancellationToken.None);

        Assert.False(resultado.Correcta);
        Assert.Contains("ConnectionStrings__Fotos", resultado.Detalle);
    }
}
