namespace Tienda.Api.Tests;

public class PaginaEstadoTests
{
    [Fact]
    public void TodoCorrecto_con_todas_las_comprobaciones_bien_devuelve_true()
    {
        var resultados = new[]
        {
            new ResultadoComprobacion("Base de datos", true, "ok"),
            new ResultadoComprobacion("Almacenamiento", true, "ok"),
        };

        Assert.True(PaginaEstado.TodoCorrecto(resultados));
    }

    [Fact]
    public void TodoCorrecto_con_una_comprobacion_mal_devuelve_false()
    {
        var resultados = new[]
        {
            new ResultadoComprobacion("Base de datos", true, "ok"),
            new ResultadoComprobacion("Almacenamiento", false, "sin permiso"),
        };

        Assert.False(PaginaEstado.TodoCorrecto(resultados));
    }

    [Fact]
    public void TodoCorrecto_sin_comprobaciones_devuelve_false()
    {
        Assert.False(PaginaEstado.TodoCorrecto([]));
    }
}
